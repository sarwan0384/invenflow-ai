using InvenFlow.Core.Entities;
using InvenFlow.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InvenFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InventoryController : ControllerBase
{
    private readonly AppDbContext _context;

    public InventoryController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/inventory
    // Fetches all inventory items on the warehouse shelves
    [HttpGet]
    [HttpGet("~/api/inventoryitems")]
    [Authorize(Policy = "RequireEmployeeOrAbove")]
    public async Task<ActionResult<IEnumerable<InventoryItem>>> GetInventory()
    {
        return await _context.InventoryItems.ToListAsync();
    }

    // GET: api/inventory/{id}
    // Fetches a single inventory item by ID
    [HttpGet("{id:guid}")]
    [HttpGet("~/api/inventoryitems/{id:guid}")]
    public async Task<ActionResult<InventoryItem>> GetInventoryItem(Guid id)
    {
        var item = await _context.InventoryItems.FindAsync(id);

        if (item == null)
        {
            return NotFound(new { message = "Item not found in inventory." });
        }

        return item;
    }

    // POST: api/inventory
    // Adds a new product item into inventory (Manual Add / Add Stock)
    [HttpPost]
    [HttpPost("~/api/inventoryitems")]
    [Authorize(Policy = "RequireManagerOrAdmin")]
    public async Task<ActionResult<InventoryItem>> CreateInventoryItem(InventoryItem item)
    {
        var tenantId = User.FindFirst("tenantId")?.Value;
        if (!Guid.TryParse(tenantId, out var parsedTenantId))
        {
            return Unauthorized(new { message = "Tenant context is missing." });
        }

        item.Id = Guid.NewGuid();
        item.TenantId = parsedTenantId;
        item.UpdatedAt = DateTime.UtcNow;

        _context.InventoryItems.Add(item);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetInventoryItem), new { id = item.Id }, item);
    }

    // PUT: api/inventory/{id}
    // Updates quantity or price for an existing inventory item
    // Preserves existing TenantId, VendorId, and InboundDocumentId
    [HttpPut("{id:guid}")]
    [HttpPut("~/api/inventoryitems/{id:guid}")]
    [Authorize(Policy = "RequireManagerOrAdmin")]
    public async Task<IActionResult> UpdateInventoryItem(Guid id, [FromBody] InventoryItem updatedItem)
    {
        var existingItem = await _context.InventoryItems.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id);
        if (existingItem == null)
        {
            return NotFound(new { message = "Item not found." });
        }

        // Preserve immutable foreign keys and tenant context
        updatedItem.Id = id;
        updatedItem.TenantId = existingItem.TenantId;
        updatedItem.VendorId = existingItem.VendorId;
        updatedItem.InboundDocumentId = existingItem.InboundDocumentId;
        updatedItem.UpdatedAt = DateTime.UtcNow;

        _context.InventoryItems.Update(updatedItem);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/inventory/{id}
    [HttpDelete("{id:guid}")]
    [HttpDelete("~/api/inventoryitems/{id:guid}")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<IActionResult> DeleteInventoryItem(Guid id)
    {
        var existingItem = await _context.InventoryItems.FindAsync(id);
        if (existingItem == null)
        {
            return NotFound(new { message = "Item not found." });
        }

        try
        {
            _context.InventoryItems.Remove(existingItem);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (DbUpdateException)
        {
            return BadRequest(new { message = "Cannot delete inventory item due to existing references." });
        }
    }
}
