using InvenFlow.Core.Entities;
using InvenFlow.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InvenFlow.Api.Controllers;

public class VendorUpdateRequest
{
    public string? Name { get; set; }
    public string? Code { get; set; }
    public string? ContactPerson { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
}

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

    // PUT: api/vendors/{id}
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateVendor(Guid id, [FromBody] VendorUpdateRequest request)
    {
        var existingVendor = await _context.Vendors.FindAsync(id);
        if (existingVendor == null)
        {
            return NotFound(new { message = "Vendor not found." });
        }

        existingVendor.Name = string.IsNullOrWhiteSpace(request.Name) ? existingVendor.Name : request.Name;
        existingVendor.Code = string.IsNullOrWhiteSpace(request.Code) ? existingVendor.Code : request.Code;
        existingVendor.ContactPerson = string.IsNullOrWhiteSpace(request.ContactPerson) ? existingVendor.ContactPerson : request.ContactPerson;
        existingVendor.Email = string.IsNullOrWhiteSpace(request.Email) ? existingVendor.Email : request.Email;
        existingVendor.Phone = string.IsNullOrWhiteSpace(request.PhoneNumber) ? (string.IsNullOrWhiteSpace(request.Phone) ? existingVendor.Phone : request.Phone) : request.PhoneNumber;
        existingVendor.Address = string.IsNullOrWhiteSpace(request.Address) ? existingVendor.Address : request.Address;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    // DELETE: api/vendors/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteVendor(Guid id)
    {
        var vendor = await _context.Vendors.FindAsync(id);
        if (vendor == null)
        {
            return NotFound(new { message = "Vendor not found." });
        }

        var relatedDocuments = await _context.InboundDocuments.Where(d => d.VendorId == id).ToListAsync();
        _context.InboundDocuments.RemoveRange(relatedDocuments);
        _context.Vendors.Remove(vendor);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}