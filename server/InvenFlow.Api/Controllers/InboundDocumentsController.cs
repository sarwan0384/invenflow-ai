using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InvenFlow.Infrastructure.Data;
using InvenFlow.Core.Entities;
using InvenFlow.Api.Services;

namespace InvenFlow.Api.Controllers;

// Request DTO for file uploads
public class UploadDocumentDto
{
    public required IFormFile File { get; set; }
    public Guid? VendorId { get; set; }
}

// Response DTOs to prevent circular serialization cycles (Document -> Vendor -> Document)
public class VendorSummaryDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? ContactPerson { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class InboundDocumentResponseDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public DocumentStatus Status { get; set; }
    public double ConfidenceScore { get; set; }
    public DateTime UploadedAt { get; set; }
    public Guid? VendorId { get; set; }
    public VendorSummaryDto? Vendor { get; set; }
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
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
    [Authorize(Policy = "RequireEmployeeOrAbove")]
    public async Task<ActionResult<IEnumerable<InboundDocumentResponseDto>>> GetDocuments()
    {
        var documents = await _context.InboundDocuments
            .Include(d => d.Vendor)
            .Select(d => MapToResponseDto(d))
            .ToListAsync();

        return Ok(documents);
    }

    // GET: api/inbounddocuments/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<InboundDocumentResponseDto>> GetDocument(Guid id)
    {
        var doc = await _context.InboundDocuments
            .Include(d => d.Vendor)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (doc == null) return NotFound();

        return Ok(MapToResponseDto(doc));
    }

    // POST: api/inbounddocuments/upload
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [Authorize(Policy = "RequireManagerOrAdmin")]
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

        var tenantId = User.FindFirst("tenantId")?.Value;
        if (!Guid.TryParse(tenantId, out var parsedTenantId))
        {
            return Unauthorized(new { message = "Tenant context is missing." });
        }

        var document = new InboundDocument
        {
            Id = Guid.NewGuid(),
            TenantId = parsedTenantId,
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
        var responseDto = MapToResponseDto(processedDocument);

        return success
            ? Ok(new { document = responseDto, result, message = "AI processing complete! Vendor linked and inventory updated." })
            : Ok(new { document = responseDto, result, message = errorMessage ?? "AI processing could not be completed." });
    }

    // POST: api/inbounddocuments/{id}/process-ai
    [HttpPost("{id:guid}/process-ai")]
    [Authorize(Policy = "RequireManagerOrAdmin")]
    public async Task<IActionResult> ProcessWithAI(Guid id, [FromServices] GeminiInvoiceService aiService)
    {
        var doc = await _context.InboundDocuments.FindAsync(id);
        if (doc == null) return NotFound(new { message = "Document not found." });

        var (processedDocument, result, success, message) = await ProcessDocumentAsync(doc, aiService);
        var responseDto = MapToResponseDto(processedDocument);

        if (success)
        {
            return Ok(new { message = "AI processing complete! Vendor linked and inventory updated.", data = result, document = responseDto });
        }

        return BadRequest(new { message = message ?? "Could not parse document data.", document = responseDto });
    }

    // DELETE: api/inbounddocuments/{id}
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<IActionResult> DeleteDocument(Guid id)
    {
        var doc = await _context.InboundDocuments.FindAsync(id);
        if (doc == null) return NotFound();

        try
        {
            if (System.IO.File.Exists(doc.FilePath))
            {
                System.IO.File.Delete(doc.FilePath);
            }

            _context.InboundDocuments.Remove(doc);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (DbUpdateException)
        {
            // DeleteBehavior.SetNull will nullify InboundDocumentId on related InventoryItems
            return BadRequest(new { message = "Cannot delete document because it is linked to existing inventory items. Remove or reassign them first." });
        }
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
                            TenantId = document.TenantId,
                            Name = result.VendorName,
                            ContactPerson = "Extracted from Invoice",
                            Email = $"billing@{result.VendorName.ToLower().Replace(" ", "")}.com",
                            Phone = "N/A",
                            Address = "Extracted by ProChips AI"
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
                        
                        // Ensure existing items updated by document upload also map vendor and document
                        if (existingItem.VendorId == null) existingItem.VendorId = document.VendorId;
                        if (existingItem.InboundDocumentId == null) existingItem.InboundDocumentId = document.Id;
                    }
                    else
                    {
                        _context.InventoryItems.Add(new InventoryItem
                        {
                            Id = Guid.NewGuid(),
                            TenantId = document.TenantId,
                            Sku = string.IsNullOrWhiteSpace(item.Sku) ? $"SKU-{Guid.NewGuid().ToString()[..6]}" : item.Sku,
                            Name = item.ItemName,
                            Category = "Inbound General",
                            QuantityOnHand = item.Quantity,
                            UnitPrice = item.UnitPrice,
                            UpdatedAt = DateTime.UtcNow,
                            VendorId = document.VendorId,      // Link to vendor
                            InboundDocumentId = document.Id  // Link to source document
                        });
                    }
                }

                await _context.SaveChangesAsync();

                // Re-load Vendor navigation property for the response
                if (document.VendorId != null && document.Vendor == null)
                {
                    document.Vendor = await _context.Vendors.FindAsync(document.VendorId);
                }

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

    private static InboundDocumentResponseDto MapToResponseDto(InboundDocument doc)
    {
        return new InboundDocumentResponseDto
        {
            Id = doc.Id,
            TenantId = doc.TenantId,
            FileName = doc.FileName,
            FilePath = doc.FilePath,
            Status = doc.Status,
            ConfidenceScore = doc.ConfidenceScore,
            UploadedAt = doc.UploadedAt,
            VendorId = doc.VendorId,
            Vendor = doc.Vendor == null ? null : new VendorSummaryDto
            {
                Id = doc.Vendor.Id,
                TenantId = doc.Vendor.TenantId,
                Name = doc.Vendor.Name,
                Code = doc.Vendor.Code,
                ContactPerson = doc.Vendor.ContactPerson,
                Email = doc.Vendor.Email,
                Phone = doc.Vendor.Phone,
                Address = doc.Vendor.Address,
                CreatedAt = doc.Vendor.CreatedAt
            }
        };
    }
}