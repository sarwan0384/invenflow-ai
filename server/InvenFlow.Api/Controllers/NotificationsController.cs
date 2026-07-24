using InvenFlow.Core.Entities;
using InvenFlow.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InvenFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly AppDbContext _context;

    public NotificationsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var notifications = await _context.Notifications.OrderByDescending(n => n.CreatedAt).Take(10).ToListAsync();
        return Ok(notifications);
    }

    [HttpPost("{id:guid}/mark-read")]
    public async Task<IActionResult> MarkRead(Guid id)
    {
        var notification = await _context.Notifications.FindAsync(id);
        if (notification == null) return NotFound();
        notification.IsRead = true;
        await _context.SaveChangesAsync();
        return Ok();
    }

    [HttpPost("{id:guid}/sync")]
    public async Task<IActionResult> Sync(Guid id)
    {
        var notification = await _context.Notifications.FindAsync(id);
        if (notification == null) return NotFound();
        notification.Status = NotificationStatus.Synced;
        notification.IsRead = true;
        await _context.SaveChangesAsync();
        return Ok(new { message = "Sync acknowledged." });
    }
}
