using InvenFlow.Core.Entities;
using InvenFlow.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InvenFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
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
    // Adds a new product item into inventory
    [HttpPost]
    [HttpPost("~/api/inventoryitems")]
    public async Task<ActionResult<InventoryItem>> CreateInventoryItem(InventoryItem item)
    {
        item.Id = Guid.NewGuid();
        item.UpdatedAt = DateTime.UtcNow;

        _context.InventoryItems.Add(item);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetInventoryItem), new { id = item.Id }, item);
    }

    // PUT: api/inventory/{id}
    // Updates quantity or price for an existing inventory item
    [HttpPut("{id:guid}")]
    [HttpPut("~/api/inventoryitems/{id:guid}")]
    public async Task<IActionResult> UpdateInventoryItem(Guid id, [FromBody] InventoryItem updatedItem)
    {
        var existingItem = await _context.InventoryItems.FindAsync(id);
        if (existingItem == null)
        {
            return NotFound(new { message = "Item not found." });
        }

        existingItem.Name = string.IsNullOrWhiteSpace(updatedItem.Name) ? existingItem.Name : updatedItem.Name;
        existingItem.Sku = string.IsNullOrWhiteSpace(updatedItem.Sku) ? existingItem.Sku : updatedItem.Sku;
        existingItem.Category = string.IsNullOrWhiteSpace(updatedItem.Category) ? existingItem.Category : updatedItem.Category;
        existingItem.QuantityOnHand = updatedItem.QuantityOnHand;
        existingItem.UnitPrice = updatedItem.UnitPrice;
        existingItem.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/inventory/{id}
    [HttpDelete("{id:guid}")]
    [HttpDelete("~/api/inventoryitems/{id:guid}")]
    public async Task<IActionResult> DeleteInventoryItem(Guid id)
    {
        var existingItem = await _context.InventoryItems.FindAsync(id);
        if (existingItem == null)
        {
            return NotFound(new { message = "Item not found." });
        }

        _context.InventoryItems.Remove(existingItem);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}