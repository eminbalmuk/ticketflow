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
        IQueryable<Ticket> visibleTickets = _context.Tickets.AsNoTracking();

        if (!User.IsInRole(SeedData.AdminRole))
        {
            if (User.IsInRole(SeedData.SupportRole))
            {
                var allowedCategories = await GetAllowedCategoriesAsync(userId);
                visibleTickets = visibleTickets.Where(ticket => allowedCategories.Contains(ticket.Category));
            }
            else
            {
                visibleTickets = visibleTickets.Where(ticket => ticket.CustomerId == userId);
                onlyMine = false;
            }
        }

        if (isStaffView && onlyMine)
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
                    Category = ticket.Category,
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

        if (!model.Category.HasValue || !Enum.IsDefined(model.Category.Value))
        {
            ModelState.AddModelError(nameof(model.Category), "Geçerli bir kategori seçiniz.");
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
            Category = model.Category.Value,
            Description = model.Description.Trim(),
            CustomerId = userId,
            Status = TicketStatus.Open,
            CreatedAt = DateTime.UtcNow
        };

        _context.Tickets.Add(ticket);
        await _context.SaveChangesAsync();

        await AddTicketNotificationsAsync(
            ticket,
            await GetStaffRecipientIdsForTicketAsync(ticket),
            "Yeni talep",
            $"#{ticket.Id} {ticket.Title} için yeni talep açıldı.",
            userId);
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

        if (!await CanViewAsync(ticket))
        {
            return Forbid();
        }

        return View(await BuildDetailsViewModelAsync(ticket));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Take(int id)
    {
        var ticket = await _context.Tickets.FindAsync(id);
        if (ticket is null)
        {
            return NotFound();
        }

        if (!await CanManageTicketAsync(ticket))
        {
            return Forbid();
        }

        var actorId = _userManager.GetUserId(User);
        var actorName = DisplayPersonName(await _userManager.GetUserAsync(User));

        ticket.AssignedSupportId = actorId;
        ticket.UpdatedAt = DateTime.UtcNow;
        await AddTicketNotificationsAsync(
            ticket,
            (await GetAdminUserIdsAsync()).Append(ticket.CustomerId),
            "Talep üstlenildi",
            $"#{ticket.Id} {ticket.Title} talebi {actorName} tarafından üstlenildi.",
            actorId);
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
            await AddTicketNotificationsAsync(
                ticket,
                [ticket.CustomerId],
                "Destek sorumlusu kaldırıldı",
                $"#{ticket.Id} {ticket.Title} talebinin destek sorumlusu kaldırıldı.",
                _userManager.GetUserId(User));
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

        if (!await SupportCanHandleCategoryAsync(supportUser.Id, ticket.Category))
        {
            TempData["ErrorMessage"] = "Seçilen support kullanıcısı bu kategoriye bakamıyor.";
            return RedirectToAction(nameof(Details), new { id });
        }

        ticket.AssignedSupportId = supportUser.Id;
        ticket.UpdatedAt = DateTime.UtcNow;
        await AddTicketNotificationsAsync(
            ticket,
            [ticket.CustomerId, supportUser.Id],
            "Destek sorumlusu atandı",
            $"#{ticket.Id} {ticket.Title} talebine {DisplayPersonName(supportUser)} atandı.",
            _userManager.GetUserId(User));
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

        var canManageTicket = await CanManageTicketAsync(ticket);
        if (!canManageTicket && ticket.CustomerId != _userManager.GetUserId(User))
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

        if (canManageTicket && ticket.AssignedSupportId is null)
        {
            ticket.AssignedSupportId = authorId;
        }

        IEnumerable<string> recipients = canManageTicket
            ? [ticket.CustomerId]
            : await GetStaffRecipientIdsForTicketAsync(ticket);
        var authorName = DisplayPersonName(await _userManager.GetUserAsync(User));

        ticket.UpdatedAt = DateTime.UtcNow;
        _context.TicketReplies.Add(new TicketReply
        {
            TicketId = id,
            AuthorId = authorId,
            Message = model.Message.Trim(),
            CreatedAt = DateTime.UtcNow
        });
        await AddTicketNotificationsAsync(
            ticket,
            recipients,
            "Yeni cevap",
            $"#{ticket.Id} {ticket.Title} talebine {authorName} cevap yazdı.",
            authorId);
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

        if (!await CanDeleteAsync(ticket))
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
        if (!Enum.IsDefined(status))
        {
            return BadRequest();
        }

        var ticket = await _context.Tickets.FindAsync(id);
        if (ticket is null)
        {
            return NotFound();
        }

        if (!await CanManageTicketAsync(ticket))
        {
            return Forbid();
        }

        var currentUserId = _userManager.GetUserId(User);
        if (!string.Equals(ticket.AssignedSupportId, currentUserId, StringComparison.Ordinal))
        {
            TempData["ErrorMessage"] = "Durumu güncellemek için önce talebi üstlenmelisiniz.";
            return RedirectToAction(nameof(Details), new { id });
        }

        ticket.Status = status;
        ticket.UpdatedAt = DateTime.UtcNow;
        await AddTicketNotificationsAsync(
            ticket,
            (await GetAdminUserIdsAsync())
                .Append(ticket.CustomerId)
                .Concat(ticket.AssignedSupportId is null ? [] : [ticket.AssignedSupportId]),
            "Talep durumu değişti",
            $"#{ticket.Id} {ticket.Title} durumu {status.GetDisplayName()} olarak güncellendi.",
            _userManager.GetUserId(User));
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Talep durumu güncellendi.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private bool CanManageTickets()
    {
        return User.IsInRole(SeedData.SupportRole) || User.IsInRole(SeedData.AdminRole);
    }

    private async Task<bool> CanViewAsync(Ticket ticket)
    {
        return ticket.CustomerId == _userManager.GetUserId(User) || await CanManageTicketAsync(ticket);
    }

    private async Task<bool> CanDeleteAsync(Ticket ticket)
    {
        return ticket.CustomerId == _userManager.GetUserId(User) || await CanManageTicketAsync(ticket);
    }

    private async Task<bool> CanManageTicketAsync(Ticket ticket)
    {
        if (User.IsInRole(SeedData.AdminRole))
        {
            return true;
        }

        var userId = _userManager.GetUserId(User);
        if (userId is null || !User.IsInRole(SeedData.SupportRole))
        {
            return false;
        }

        return await SupportCanHandleCategoryAsync(userId, ticket.Category);
    }

    private Task<bool> SupportCanHandleCategoryAsync(string supportUserId, TicketCategory category)
    {
        return _context.SupportCategoryAssignments
            .AsNoTracking()
            .AnyAsync(assignment =>
                assignment.SupportUserId == supportUserId &&
                assignment.Category == category);
    }

    private Task<List<TicketCategory>> GetAllowedCategoriesAsync(string supportUserId)
    {
        return _context.SupportCategoryAssignments
            .AsNoTracking()
            .Where(assignment => assignment.SupportUserId == supportUserId)
            .Select(assignment => assignment.Category)
            .ToListAsync();
    }

    private async Task<IReadOnlyList<string>> GetStaffRecipientIdsForTicketAsync(Ticket ticket)
    {
        var recipients = new List<string>();
        recipients.AddRange(await GetAdminUserIdsAsync());

        if (!string.IsNullOrWhiteSpace(ticket.AssignedSupportId))
        {
            recipients.Add(ticket.AssignedSupportId);
        }
        else
        {
            recipients.AddRange(await GetSupportUserIdsForCategoryAsync(ticket.Category));
        }

        return recipients;
    }

    private async Task<IReadOnlyList<string>> GetAdminUserIdsAsync()
    {
        var admins = await _userManager.GetUsersInRoleAsync(SeedData.AdminRole);
        return admins.Select(user => user.Id).ToList();
    }

    private Task<List<string>> GetSupportUserIdsForCategoryAsync(TicketCategory category)
    {
        return _context.SupportCategoryAssignments
            .AsNoTracking()
            .Where(assignment => assignment.Category == category)
            .Select(assignment => assignment.SupportUserId)
            .ToListAsync();
    }

    private Task AddTicketNotificationsAsync(
        Ticket ticket,
        IEnumerable<string> recipientIds,
        string title,
        string message,
        string? actorId)
    {
        var notifications = recipientIds
            .Where(recipientId => !string.IsNullOrWhiteSpace(recipientId))
            .Where(recipientId => !string.Equals(recipientId, actorId, StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .Select(recipientId => new TicketNotification
            {
                UserId = recipientId,
                TicketId = ticket.Id,
                Title = title,
                Message = message,
                CreatedAt = DateTime.UtcNow
            })
            .ToList();

        if (notifications.Count > 0)
        {
            _context.TicketNotifications.AddRange(notifications);
        }

        return Task.CompletedTask;
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
        var canManage = await CanManageTicketAsync(ticket);
        var canDelete = ticket.CustomerId == _userManager.GetUserId(User) || canManage;
        var canUpdateStatus = canManage &&
            string.Equals(ticket.AssignedSupportId, _userManager.GetUserId(User), StringComparison.Ordinal);
        var model = ToDetailsViewModel(ticket, canManage, canDelete);
        model.CanUpdateStatus = canUpdateStatus;
        await ApplyReplyRolesAsync(model);

        if (User.IsInRole(SeedData.AdminRole))
        {
            model.CanAssignSupport = true;
            model.SupportUsers = await GetSupportOptionsAsync(ticket.Category);
        }

        return model;
    }

    private TicketDetailsViewModel ToDetailsViewModel(Ticket ticket, bool canManage, bool canDelete)
    {
        return new TicketDetailsViewModel
        {
            Id = ticket.Id,
            Title = ticket.Title,
            Description = ticket.Description,
            Status = ticket.Status,
            Category = ticket.Category,
            CreatedAt = ticket.CreatedAt,
            UpdatedAt = ticket.UpdatedAt,
            CustomerEmail = DisplayUserName(ticket.Customer),
            AssignedSupportEmail = DisplayUserName(ticket.AssignedSupport),
            AssignedSupportId = ticket.AssignedSupportId,
            CanManage = canManage,
            CanDelete = canDelete,
            Replies = ticket.Replies
                .OrderBy(reply => reply.CreatedAt)
                .Select(reply => new TicketReplyItemViewModel
                {
                    AuthorId = reply.AuthorId,
                    AuthorEmail = DisplayPersonName(reply.Author),
                    Message = reply.Message,
                    CreatedAt = reply.CreatedAt
                })
                .ToList()
        };
    }

    private async Task ApplyReplyRolesAsync(TicketDetailsViewModel model)
    {
        var authorIds = model.Replies
            .Select(reply => reply.AuthorId)
            .Where(authorId => !string.IsNullOrWhiteSpace(authorId))
            .Distinct()
            .ToList();

        if (authorIds.Count == 0)
        {
            return;
        }

        var authorRoles = await (
            from userRole in _context.UserRoles.AsNoTracking()
            join role in _context.Roles.AsNoTracking() on userRole.RoleId equals role.Id
            where authorIds.Contains(userRole.UserId)
            select new
            {
                userRole.UserId,
                role.Name
            })
            .ToListAsync();

        var rolesByAuthor = authorRoles
            .GroupBy(item => item.UserId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(item => item.Name)
                    .Where(roleName => !string.IsNullOrWhiteSpace(roleName))
                    .Cast<string>()
                    .ToHashSet(StringComparer.OrdinalIgnoreCase));

        foreach (var reply in model.Replies)
        {
            rolesByAuthor.TryGetValue(reply.AuthorId, out var roles);

            if (roles?.Contains(SeedData.AdminRole) == true)
            {
                reply.AuthorRoleName = "Admin";
                reply.AuthorRoleCssClass = "reply-role-admin";
            }
            else if (roles?.Contains(SeedData.SupportRole) == true)
            {
                reply.AuthorRoleName = "Support";
                reply.AuthorRoleCssClass = "reply-role-support";
            }
            else
            {
                reply.AuthorRoleName = "Müşteri";
                reply.AuthorRoleCssClass = "reply-role-customer";
            }
        }
    }

    private async Task<IReadOnlyList<TicketSupportOptionViewModel>> GetSupportOptionsAsync(TicketCategory category)
    {
        var supportUsers = await _userManager.GetUsersInRoleAsync(SeedData.SupportRole);
        var supportUserIds = supportUsers.Select(user => user.Id).ToList();
        var categorySupportIds = await _context.SupportCategoryAssignments
            .AsNoTracking()
            .Where(assignment =>
                supportUserIds.Contains(assignment.SupportUserId) &&
                assignment.Category == category)
            .Select(assignment => assignment.SupportUserId)
            .ToListAsync();
        var categorySupportIdSet = categorySupportIds.ToHashSet(StringComparer.Ordinal);

        return supportUsers
            .Where(user => categorySupportIdSet.Contains(user.Id))
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

    private static string DisplayPersonName(ApplicationUser? user)
    {
        if (user is null)
        {
            return "Bilinmiyor";
        }

        if (!string.IsNullOrWhiteSpace(user.FullName))
        {
            return user.FullName;
        }

        return DisplayUserName(user);
    }
}
