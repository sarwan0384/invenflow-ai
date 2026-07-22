using InvenFlow.Core.Entities;
using InvenFlow.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InvenFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VendorsController : ControllerBase
{
    private readonly AppDbContext _context;

    public VendorsController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/vendors
    // Fetches all vendors from PostgreSQL
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Vendor>>> GetVendors()
    {
        return await _context.Vendors.ToListAsync();
    }

    // POST: api/vendors
    // Adds a new vendor into PostgreSQL
    [HttpPost]
    public async Task<ActionResult<Vendor>> CreateVendor(Vendor vendor)
    {
        vendor.Id = Guid.NewGuid();
        vendor.CreatedAt = DateTime.UtcNow;

        _context.Vendors.Add(vendor);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetVendors), new { id = vendor.Id }, vendor);
    }
}