using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ticketflow.Data;

namespace ticketflow.Controllers;

[Authorize]
public class NotificationsController : Controller
{
    private readonly ApplicationDbContext _context;

    public NotificationsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Open(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Challenge();
        }

        var notification = await _context.TicketNotifications
            .FirstOrDefaultAsync(item => item.Id == id && item.UserId == userId);

        if (notification is null)
        {
            return NotFound();
        }

        if (notification.ReadAt is null)
        {
            notification.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return RedirectToAction("Details", "Tickets", new { id = notification.TicketId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, string? returnUrl)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Challenge();
        }

        var notification = await _context.TicketNotifications
            .FirstOrDefaultAsync(item => item.Id == id && item.UserId == userId);

        if (notification is not null)
        {
            _context.TicketNotifications.Remove(notification);
            await _context.SaveChangesAsync();
        }

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction("Index", "Tickets");
    }
}
