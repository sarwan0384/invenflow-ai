using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InvenFlow.Infrastructure.Data;
using InvenFlow.Core.Entities;
using InvenFlow.Api.Services;

namespace InvenFlow.Api.Controllers;

// Request DTO for file uploads to ensure explicit schema generation in Swagger
public class UploadDocumentDto
{
    public required IFormFile File { get; set; }
    public Guid? VendorId { get; set; }
}

[ApiController]
[Route("api/[controller]")]
public class InboundDocumentsController : ControllerBase
{
    private readonly AppDbContext _context;
    private const string DuplicateMessage = "Document with filename '{0}' already exists in the system.";

    public InboundDocumentsController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/inbounddocuments
    [HttpGet]
    public async Task<ActionResult<IEnumerable<InboundDocument>>> GetDocuments()
    {
        return await _context.InboundDocuments.Include(d => d.Vendor).ToListAsync();
    }

    // GET: api/inbounddocuments/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<InboundDocument>> GetDocument(Guid id)
    {
        var doc = await _context.InboundDocuments.Include(d => d.Vendor).FirstOrDefaultAsync(d => d.Id == id);
        if (doc == null) return NotFound();
        return doc;
    }

    // POST: api/inbounddocuments/upload
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadDocument([FromForm] UploadDocumentDto dto, [FromServices] GeminiInvoiceService aiService)
    {
        if (dto.File == null || dto.File.Length == 0)
        {
            return BadRequest(new { message = "No file uploaded." });
        }

        var fileName = dto.File.FileName?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return BadRequest(new { message = "No file uploaded." });
        }

        var existingDocument = await _context.InboundDocuments
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.FileName.ToLower() == fileName.ToLower());

        if (existingDocument != null)
        {
            return Conflict(new { message = string.Format(DuplicateMessage, fileName) });
        }

        using var memoryStream = new MemoryStream();
        await dto.File.CopyToAsync(memoryStream);
        var fileBytes = memoryStream.ToArray();
        var fileHash = Convert.ToHexString(SHA256.HashData(fileBytes));
        var fileSize = fileBytes.Length;

        var existingFiles = await _context.InboundDocuments.AsNoTracking().ToListAsync();
        foreach (var candidate in existingFiles)
        {
            if (!System.IO.File.Exists(candidate.FilePath))
            {
                continue;
            }

            var existingInfo = new FileInfo(candidate.FilePath);
            if (existingInfo.Length != fileSize)
            {
                continue;
            }

            if (string.Equals(ComputeFileHash(candidate.FilePath), fileHash, StringComparison.OrdinalIgnoreCase))
            {
                return Conflict(new { message = string.Format(DuplicateMessage, fileName) });
            }
        }

        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(fileName)}";
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);
        await System.IO.File.WriteAllBytesAsync(filePath, fileBytes);

        var document = new InboundDocument
        {
            Id = Guid.NewGuid(),
            FileName = fileName,
            FilePath = filePath,
            Status = DocumentStatus.Pending,
            ConfidenceScore = 0.0,
            UploadedAt = DateTime.UtcNow,
            VendorId = dto.VendorId
        };

        _context.InboundDocuments.Add(document);
        await _context.SaveChangesAsync();

        var (processedDocument, result, success, errorMessage) = await ProcessDocumentAsync(document, aiService);
        return success
            ? Ok(new { document = processedDocument, result, message = "AI processing complete! Vendor linked and inventory updated." })
            : Ok(new { document = processedDocument, result, message = errorMessage ?? "AI processing could not be completed." });
    }

    // POST: api/inbounddocuments/{id}/process-ai
    [HttpPost("{id:guid}/process-ai")]
    public async Task<IActionResult> ProcessWithAI(Guid id, [FromServices] GeminiInvoiceService aiService)
    {
        var doc = await _context.InboundDocuments.FindAsync(id);
        if (doc == null) return NotFound(new { message = "Document not found." });

        var (processedDocument, result, success, message) = await ProcessDocumentAsync(doc, aiService);
        if (success)
        {
            return Ok(new { message = "AI processing complete! Vendor linked and inventory updated.", data = result, document = processedDocument });
        }

        return BadRequest(new { message = message ?? "Could not parse document data.", document = processedDocument });
    }

    // DELETE: api/inbounddocuments/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteDocument(Guid id)
    {
        var doc = await _context.InboundDocuments.FindAsync(id);
        if (doc == null) return NotFound();

        if (System.IO.File.Exists(doc.FilePath))
        {
            System.IO.File.Delete(doc.FilePath);
        }

        _context.InboundDocuments.Remove(doc);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private async Task<(InboundDocument Document, ExtractedInvoiceData? Result, bool Succeeded, string? Message)> ProcessDocumentAsync(InboundDocument document, GeminiInvoiceService aiService)
    {
        document.Status = DocumentStatus.Processing;
        await _context.SaveChangesAsync();

        try
        {
            var result = await aiService.ProcessInvoiceAsync(document.FilePath);

            if (result != null)
            {
                document.Status = DocumentStatus.Processed;
                document.ConfidenceScore = result.ConfidenceScore;

                if (document.VendorId is null && !string.IsNullOrWhiteSpace(result.VendorName))
                {
                    var existingVendor = await _context.Vendors
                        .FirstOrDefaultAsync(v => v.Name.ToLower() == result.VendorName.ToLower());

                    if (existingVendor != null)
                    {
                        document.VendorId = existingVendor.Id;
                    }
                    else
                    {
                        var newVendor = new Vendor
                        {
                            Id = Guid.NewGuid(),
                            Name = result.VendorName,
                            ContactPerson = "Extracted from Invoice",
                            Email = $"billing@{result.VendorName.ToLower().Replace(" ", "")}.com",
                            Phone = "N/A",
                            Address = "Extracted by InvenFlow AI"
                        };

                        _context.Vendors.Add(newVendor);
                        document.VendorId = newVendor.Id;
                    }
                }

                foreach (var item in result.LineItems)
                {
                    var existingItem = await _context.InventoryItems.FirstOrDefaultAsync(i => i.Sku == item.Sku);
                    if (existingItem != null)
                    {
                        existingItem.QuantityOnHand += item.Quantity;
                        existingItem.UpdatedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        _context.InventoryItems.Add(new InventoryItem
                        {
                            Id = Guid.NewGuid(),
                            Sku = string.IsNullOrWhiteSpace(item.Sku) ? $"SKU-{Guid.NewGuid().ToString()[..6]}" : item.Sku,
                            Name = item.ItemName,
                            Category = "Inbound General",
                            QuantityOnHand = item.Quantity,
                            UnitPrice = item.UnitPrice,
                            UpdatedAt = DateTime.UtcNow
                        });
                    }
                }

                await _context.SaveChangesAsync();
                return (document, result, true, null);
            }

            document.Status = DocumentStatus.Failed;
            await _context.SaveChangesAsync();
            return (document, null, false, "Could not parse document data.");
        }
        catch (Exception ex)
        {
            document.Status = DocumentStatus.Failed;
            await _context.SaveChangesAsync();
            return (document, null, false, ex.Message);
        }
    }

    private static string ComputeFileHash(string filePath)
    {
        using var stream = System.IO.File.OpenRead(filePath);
        using var sha256 = SHA256.Create();
        return Convert.ToHexString(sha256.ComputeHash(stream));
    }
}