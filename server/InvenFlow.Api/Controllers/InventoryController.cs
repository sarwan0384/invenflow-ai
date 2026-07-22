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
    public async Task<ActionResult<IEnumerable<InventoryItem>>> GetInventory()
    {
        return await _context.InventoryItems.ToListAsync();
    }

    // GET: api/inventory/{id}
    // Fetches a single inventory item by ID
    [HttpGet("{id:guid}")]
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
    public async Task<IActionResult> UpdateInventoryItem(Guid id, InventoryItem updatedItem)
    {
        if (id != updatedItem.Id)
        {
            return BadRequest(new { message = "ID mismatch." });
        }

        var existingItem = await _context.InventoryItems.FindAsync(id);
        if (existingItem == null)
        {
            return NotFound(new { message = "Item not found." });
        }

        existingItem.Name = updatedItem.Name;
        existingItem.Sku = updatedItem.Sku;
        existingItem.Category = updatedItem.Category;
        existingItem.QuantityOnHand = updatedItem.QuantityOnHand;
        existingItem.UnitPrice = updatedItem.UnitPrice;
        existingItem.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }
}