using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ticketflow.Data;
using ticketflow.Models;
using ticketflow.ViewModels;

namespace ticketflow.Controllers;

[Authorize(Roles = SeedData.AdminRole + "," + SeedData.SupportRole)]
public class CustomersController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public CustomersController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(string? customerQuery)
    {
        var query = customerQuery?.Trim();
        var isSupportOnly = User.IsInRole(SeedData.SupportRole) && !User.IsInRole(SeedData.AdminRole);
        IReadOnlyList<TicketCategory>? allowedCategories = null;

        if (isSupportOnly)
        {
            var userId = _userManager.GetUserId(User);
            if (userId is null)
            {
                return Challenge();
            }

            allowedCategories = await GetAllowedCategoriesAsync(userId);
        }

        var model = new CustomerSearchViewModel
        {
            CustomerQuery = query,
            IsSupportView = isSupportOnly,
            CustomerResults = string.IsNullOrWhiteSpace(query)
                ? []
                : await SearchCustomersAsync(query, allowedCategories)
        };

        return View(model);
    }

    private async Task<IReadOnlyList<CustomerSearchResultViewModel>> SearchCustomersAsync(
        string query,
        IReadOnlyList<TicketCategory>? allowedCategories)
    {
        var customerRoleId = await _context.Roles
            .AsNoTracking()
            .Where(role => role.Name == SeedData.CustomerRole)
            .Select(role => role.Id)
            .FirstOrDefaultAsync();

        if (customerRoleId is null)
        {
            return [];
        }

        var customerIds = _context.UserRoles
            .AsNoTracking()
            .Where(userRole => userRole.RoleId == customerRoleId)
            .Select(userRole => userRole.UserId);

        var users = await _userManager.Users
            .AsNoTracking()
            .Where(user =>
                customerIds.Contains(user.Id) &&
                ((user.Email != null && user.Email.Contains(query)) ||
                 (user.UserName != null && user.UserName.Contains(query)) ||
                 user.FullName.Contains(query)))
            .OrderBy(user => user.UserName)
            .Take(10)
            .Select(user => new CustomerSearchResultViewModel
            {
                Id = user.Id,
                UserName = user.UserName ?? "Bilinmiyor",
                FullName = DisplayUser(user),
                Email = user.Email ?? "E-posta yok"
            })
            .ToListAsync();

        if (users.Count == 0)
        {
            return users;
        }

        var userIds = users.Select(user => user.Id).ToList();
        var ticketQuery = _context.Tickets
            .AsNoTracking()
            .Where(ticket => userIds.Contains(ticket.CustomerId));

        if (allowedCategories is { Count: > 0 })
        {
            ticketQuery = ticketQuery.Where(ticket => allowedCategories.Contains(ticket.Category));
        }
        else if (allowedCategories is not null)
        {
            ticketQuery = ticketQuery.Where(ticket => false);
        }

        var tickets = await ticketQuery
            .OrderByDescending(ticket => ticket.CreatedAt)
            .Select(ticket => new CustomerSearchTicketViewModel
            {
                CustomerId = ticket.CustomerId,
                Id = ticket.Id,
                Title = ticket.Title,
                Status = ticket.Status,
                Category = ticket.Category,
                CreatedAt = ticket.CreatedAt,
                AssignedSupportEmail = DisplayUser(ticket.AssignedSupport)
            })
            .ToListAsync();

        foreach (var user in users)
        {
            user.Tickets = tickets
                .Where(ticket => ticket.CustomerId == user.Id)
                .ToList();
        }

        return users;
    }

    private Task<List<TicketCategory>> GetAllowedCategoriesAsync(string supportUserId)
    {
        return _context.SupportCategoryAssignments
            .AsNoTracking()
            .Where(assignment => assignment.SupportUserId == supportUserId)
            .Select(assignment => assignment.Category)
            .ToListAsync();
    }

    private static string DisplayUser(ApplicationUser? user)
    {
        if (user is null)
        {
            return "Bilinmiyor";
        }

        if (!string.IsNullOrWhiteSpace(user.FullName))
        {
            return user.FullName;
        }

        return user.UserName ?? user.Email ?? "Bilinmiyor";
    }
}
