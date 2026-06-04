using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ticketflow.Data;
using ticketflow.Models;
using ticketflow.ViewModels;

namespace ticketflow.Controllers;

[Authorize(Roles = SeedData.AdminRole)]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminController(
        ApplicationDbContext context,
        RoleManager<IdentityRole> roleManager,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _roleManager = roleManager;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(string? customerQuery)
    {
        return View(await BuildDashboardAsync(customerQuery, new AdminSupportInputModel()));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MakeSupport([Bind(Prefix = "SupportInput")] AdminSupportInputModel supportInput, string? customerQuery)
    {
        var email = supportInput.Email.Trim();
        var userName = supportInput.UserName?.Trim();

        var user = string.IsNullOrWhiteSpace(email)
            ? null
            : await _userManager.FindByEmailAsync(email);

        if (user is null)
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                ModelState.AddModelError(nameof(supportInput.UserName), "Kayıtlı olmayan kullanıcı için kullanıcı adı zorunludur.");
            }

            if (string.IsNullOrWhiteSpace(supportInput.TemporaryPassword))
            {
                ModelState.AddModelError(nameof(supportInput.TemporaryPassword), "Kayıtlı olmayan kullanıcı için geçici şifre zorunludur.");
            }
        }

        if (!ModelState.IsValid)
        {
            return View("Index", await BuildDashboardAsync(customerQuery, supportInput));
        }

        if (!await _roleManager.RoleExistsAsync(SeedData.SupportRole))
        {
            await _roleManager.CreateAsync(new IdentityRole(SeedData.SupportRole));
        }

        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = userName,
                FullName = supportInput.FullName?.Trim() ?? string.Empty,
                Email = email,
                EmailConfirmed = true
            };

            var createResult = await _userManager.CreateAsync(user, supportInput.TemporaryPassword!);
            if (!createResult.Succeeded)
            {
                AddIdentityErrors(createResult);
                return View("Index", await BuildDashboardAsync(customerQuery, supportInput));
            }
        }
        else if (!string.IsNullOrWhiteSpace(userName) && !string.Equals(user.UserName, userName, StringComparison.Ordinal))
        {
            var userNameResult = await _userManager.SetUserNameAsync(user, userName);
            if (!userNameResult.Succeeded)
            {
                AddIdentityErrors(userNameResult);
                return View("Index", await BuildDashboardAsync(customerQuery, supportInput));
            }
        }

        if (!string.IsNullOrWhiteSpace(supportInput.FullName) &&
            !string.Equals(user.FullName, supportInput.FullName.Trim(), StringComparison.Ordinal))
        {
            user.FullName = supportInput.FullName.Trim();
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                AddIdentityErrors(updateResult);
                return View("Index", await BuildDashboardAsync(customerQuery, supportInput));
            }
        }

        if (!await _userManager.IsInRoleAsync(user, SeedData.SupportRole))
        {
            var roleResult = await _userManager.AddToRoleAsync(user, SeedData.SupportRole);
            if (!roleResult.Succeeded)
            {
                AddIdentityErrors(roleResult);
                return View("Index", await BuildDashboardAsync(customerQuery, supportInput));
            }
        }

        TempData["SuccessMessage"] = $"{user.UserName} support rolüne alındı.";
        return RedirectToAction(nameof(Index), new { customerQuery });
    }

    private async Task<AdminDashboardViewModel> BuildDashboardAsync(string? customerQuery, AdminSupportInputModel supportInput)
    {
        var query = customerQuery?.Trim();
        var model = new AdminDashboardViewModel
        {
            CustomerQuery = query,
            SupportInput = supportInput,
            SupportUsers = await GetSupportUsersAsync(),
            OpenTicketCount = await _context.Tickets.CountAsync(ticket => ticket.Status == TicketStatus.Open),
            UnassignedTicketCount = await _context.Tickets.CountAsync(ticket => ticket.AssignedSupportId == null)
        };

        if (!string.IsNullOrWhiteSpace(query))
        {
            model.CustomerResults = await SearchCustomersAsync(query);
        }

        return model;
    }

    private async Task<IReadOnlyList<AdminCustomerResultViewModel>> SearchCustomersAsync(string query)
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
                 (user.UserName != null && user.UserName.Contains(query))))
            .OrderBy(user => user.UserName)
            .Take(10)
            .Select(user => new AdminCustomerResultViewModel
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
        var tickets = await _context.Tickets
            .AsNoTracking()
            .Where(ticket => userIds.Contains(ticket.CustomerId))
            .OrderByDescending(ticket => ticket.CreatedAt)
            .Select(ticket => new AdminTicketSummaryViewModel
            {
                CustomerId = ticket.CustomerId,
                Id = ticket.Id,
                Title = ticket.Title,
                Status = ticket.Status,
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

    private async Task<IReadOnlyList<AdminSupportUserViewModel>> GetSupportUsersAsync()
    {
        var supportUsers = await _userManager.GetUsersInRoleAsync(SeedData.SupportRole);

        return supportUsers
            .OrderBy(user => user.UserName)
            .Select(user => new AdminSupportUserViewModel
            {
                UserName = user.UserName ?? "Bilinmiyor",
                FullName = DisplayUser(user),
                Email = user.Email ?? "E-posta yok"
            })
            .ToList();
    }

    private void AddIdentityErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
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
