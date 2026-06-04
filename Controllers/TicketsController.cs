using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ticketflow.Data;
using ticketflow.Models;
using ticketflow.ViewModels;

namespace ticketflow.Controllers;

[Authorize]
public class TicketsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public TicketsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(TicketStatus? status, bool onlyMine = false)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Challenge();
        }

        var isStaffView = CanManageTickets();
        var visibleTickets = _context.Tickets.AsNoTracking();

        if (!isStaffView)
        {
            visibleTickets = visibleTickets.Where(ticket => ticket.CustomerId == userId);
            onlyMine = false;
        }
        else if (onlyMine)
        {
            visibleTickets = visibleTickets.Where(ticket => ticket.AssignedSupportId == userId);
        }

        var filteredTickets = status.HasValue
            ? visibleTickets.Where(ticket => ticket.Status == status.Value)
            : visibleTickets;

        var model = new TicketListViewModel
        {
            SelectedStatus = status,
            OnlyAssignedToMe = isStaffView && onlyMine,
            IsStaffView = isStaffView,
            OpenCount = await visibleTickets.CountAsync(ticket => ticket.Status == TicketStatus.Open),
            ResolvedCount = await visibleTickets.CountAsync(ticket => ticket.Status == TicketStatus.Resolved),
            ClosedCount = await visibleTickets.CountAsync(ticket => ticket.Status == TicketStatus.Closed),
            Tickets = await filteredTickets
                .OrderByDescending(ticket => ticket.CreatedAt)
                .Select(ticket => new TicketListItemViewModel
                {
                    Id = ticket.Id,
                    Title = ticket.Title,
                    Status = ticket.Status,
                    CreatedAt = ticket.CreatedAt,
                    CustomerEmail = DisplayUserName(ticket.Customer),
                    AssignedSupportEmail = DisplayUserName(ticket.AssignedSupport),
                    ReplyCount = ticket.Replies.Count
                })
                .ToListAsync()
        };

        return View(model);
    }

    public IActionResult Create()
    {
        return View(new TicketCreateViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TicketCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Challenge();
        }

        var ticket = new Ticket
        {
            Title = model.Title.Trim(),
            Description = model.Description.Trim(),
            CustomerId = userId,
            Status = TicketStatus.Open,
            CreatedAt = DateTime.UtcNow
        };

        _context.Tickets.Add(ticket);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Destek talebiniz oluşturuldu.";
        return RedirectToAction(nameof(Details), new { id = ticket.Id });
    }

    public async Task<IActionResult> Details(int id)
    {
        var ticket = await GetTicketForDetailsAsync(id);
        if (ticket is null)
        {
            return NotFound();
        }

        if (!CanView(ticket))
        {
            return Forbid();
        }

        return View(await BuildDetailsViewModelAsync(ticket));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Take(int id)
    {
        if (!CanManageTickets())
        {
            return Forbid();
        }

        var ticket = await _context.Tickets.FindAsync(id);
        if (ticket is null)
        {
            return NotFound();
        }

        ticket.AssignedSupportId = _userManager.GetUserId(User);
        ticket.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Talep üzerinize alındı.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = SeedData.AdminRole)]
    public async Task<IActionResult> AssignSupport(int id, string? supportUserId)
    {
        var ticket = await _context.Tickets.FindAsync(id);
        if (ticket is null)
        {
            return NotFound();
        }

        var selectedSupportId = supportUserId?.Trim();
        if (string.IsNullOrWhiteSpace(selectedSupportId))
        {
            ticket.AssignedSupportId = null;
            ticket.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Destek sorumlusu kaldÄ±rÄ±ldÄ±.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var supportUser = await _userManager.FindByIdAsync(selectedSupportId);
        if (supportUser is null || !await _userManager.IsInRoleAsync(supportUser, SeedData.SupportRole))
        {
            TempData["ErrorMessage"] = "SeÃ§ilen kullanÄ±cÄ± support rolÃ¼nde deÄŸil.";
            return RedirectToAction(nameof(Details), new { id });
        }

        ticket.AssignedSupportId = supportUser.Id;
        ticket.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"{DisplayUserName(supportUser)} talebe destek sorumlusu olarak atandÄ±.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reply(int id, TicketReplyViewModel model)
    {
        var ticket = await _context.Tickets.FindAsync(id);
        if (ticket is null)
        {
            return NotFound();
        }

        if (!CanView(ticket))
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            var detailedTicket = await GetTicketForDetailsAsync(id);
            if (detailedTicket is null)
            {
                return NotFound();
            }

            var detailsModel = await BuildDetailsViewModelAsync(detailedTicket);
            detailsModel.NewReply = model;
            return View("Details", detailsModel);
        }

        var authorId = _userManager.GetUserId(User);
        if (authorId is null)
        {
            return Challenge();
        }

        if (CanManageTickets() && ticket.AssignedSupportId is null)
        {
            ticket.AssignedSupportId = authorId;
        }

        ticket.UpdatedAt = DateTime.UtcNow;
        _context.TicketReplies.Add(new TicketReply
        {
            TicketId = id,
            AuthorId = authorId,
            Message = model.Message.Trim(),
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Cevabınız eklendi.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var ticket = await _context.Tickets.FindAsync(id);
        if (ticket is null)
        {
            return NotFound();
        }

        if (!CanDelete(ticket))
        {
            return Forbid();
        }

        _context.Tickets.Remove(ticket);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Talep silindi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, TicketStatus status)
    {
        if (!CanManageTickets())
        {
            return Forbid();
        }

        if (!Enum.IsDefined(status))
        {
            return BadRequest();
        }

        var ticket = await _context.Tickets.FindAsync(id);
        if (ticket is null)
        {
            return NotFound();
        }

        ticket.Status = status;
        ticket.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Talep durumu güncellendi.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private bool CanManageTickets()
    {
        return User.IsInRole(SeedData.SupportRole) || User.IsInRole(SeedData.AdminRole);
    }

    private bool CanView(Ticket ticket)
    {
        return CanManageTickets() || ticket.CustomerId == _userManager.GetUserId(User);
    }

    private bool CanDelete(Ticket ticket)
    {
        return CanManageTickets() || ticket.CustomerId == _userManager.GetUserId(User);
    }

    private Task<Ticket?> GetTicketForDetailsAsync(int id)
    {
        return _context.Tickets
            .AsNoTracking()
            .Include(ticket => ticket.Customer)
            .Include(ticket => ticket.AssignedSupport)
            .Include(ticket => ticket.Replies)
                .ThenInclude(reply => reply.Author)
            .FirstOrDefaultAsync(ticket => ticket.Id == id);
    }

    private async Task<TicketDetailsViewModel> BuildDetailsViewModelAsync(Ticket ticket)
    {
        var model = ToDetailsViewModel(ticket);

        if (User.IsInRole(SeedData.AdminRole))
        {
            model.CanAssignSupport = true;
            model.SupportUsers = await GetSupportOptionsAsync();
        }

        return model;
    }

    private TicketDetailsViewModel ToDetailsViewModel(Ticket ticket)
    {
        return new TicketDetailsViewModel
        {
            Id = ticket.Id,
            Title = ticket.Title,
            Description = ticket.Description,
            Status = ticket.Status,
            CreatedAt = ticket.CreatedAt,
            UpdatedAt = ticket.UpdatedAt,
            CustomerEmail = DisplayUserName(ticket.Customer),
            AssignedSupportEmail = DisplayUserName(ticket.AssignedSupport),
            AssignedSupportId = ticket.AssignedSupportId,
            CanManage = CanManageTickets(),
            CanDelete = CanDelete(ticket),
            Replies = ticket.Replies
                .OrderBy(reply => reply.CreatedAt)
                .Select(reply => new TicketReplyItemViewModel
                {
                    AuthorEmail = DisplayUserName(reply.Author),
                    Message = reply.Message,
                    CreatedAt = reply.CreatedAt
                })
                .ToList()
        };
    }

    private async Task<IReadOnlyList<TicketSupportOptionViewModel>> GetSupportOptionsAsync()
    {
        var supportUsers = await _userManager.GetUsersInRoleAsync(SeedData.SupportRole);

        return supportUsers
            .OrderBy(user => DisplayUserName(user))
            .Select(user => new TicketSupportOptionViewModel
            {
                Id = user.Id,
                DisplayName = DisplayUserName(user)
            })
            .ToList();
    }

    private static string DisplayUserName(ApplicationUser? user)
    {
        if (user is null)
        {
            return "Bilinmiyor";
        }

        return user.UserName ?? user.Email ?? "Bilinmiyor";
    }
}
