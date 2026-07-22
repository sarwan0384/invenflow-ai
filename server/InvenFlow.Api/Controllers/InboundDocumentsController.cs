using InvenFlow.Core.Entities;
using InvenFlow.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InvenFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InboundDocumentsController : ControllerBase
{
    private readonly AppDbContext _context;

    public InboundDocumentsController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/inbounddocuments
    // Lists all uploaded documents along with their associated vendor info
    [HttpGet]
    public async Task<ActionResult<IEnumerable<InboundDocument>>> GetDocuments()
    {
        return await _context.InboundDocuments
            .Include(d => d.Vendor)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync();
    }

    // GET: api/inbounddocuments/{id}
    // Gets details for a specific uploaded document
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<InboundDocument>> GetDocument(Guid id)
    {
        var doc = await _context.InboundDocuments
            .Include(d => d.Vendor)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (doc == null)
        {
            return NotFound(new { message = "Document not found." });
        }

        return doc;
    }

    // POST: api/inbounddocuments
    // Registers a new uploaded invoice document record
    [HttpPost]
    public async Task<ActionResult<InboundDocument>> CreateDocument(InboundDocument document)
    {
        document.Id = Guid.NewGuid();
        document.UploadedAt = DateTime.UtcNow;

        _context.InboundDocuments.Add(document);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetDocument), new { id = document.Id }, document);
    }
}