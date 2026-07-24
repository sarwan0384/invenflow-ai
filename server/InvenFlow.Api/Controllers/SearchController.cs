using InvenFlow.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InvenFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SearchController : ControllerBase
{
    private readonly AppDbContext _context;

    public SearchController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return Ok(new { inventory = Array.Empty<object>(), vendors = Array.Empty<object>(), documents = Array.Empty<object>() });
        }

        var query = q.Trim();
        var inventory = await _context.InventoryItems.Where(i => i.Name.Contains(query) || i.Sku.Contains(query)).Take(5).Select(i => new { type = "Inventory", id = i.Id, title = i.Name, subtitle = i.Sku }).ToListAsync();
        var vendors = await _context.Vendors.Where(v => v.Name.Contains(query) || v.Code!.Contains(query)).Take(5).Select(v => new { type = "Vendor", id = v.Id, title = v.Name, subtitle = v.Code }).ToListAsync();
        var documents = await _context.InboundDocuments.Where(d => d.FileName.Contains(query)).Take(5).Select(d => new { type = "Document", id = d.Id, title = d.FileName, subtitle = d.Status.ToString() }).ToListAsync();
        return Ok(new { inventory, vendors, documents });
    }
}
