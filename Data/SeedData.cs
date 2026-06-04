using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ticketflow.Models;

namespace ticketflow.Data;

public static class SeedData
{
    public const string CustomerRole = "Customer";
    public const string SupportRole = "Support";
    public const string AdminRole = "Admin";

    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        foreach (var role in new[] { CustomerRole, SupportRole, AdminRole })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        await EnsureUserAsync(userManager, "customer", "Müşteri Kullanıcı", "customer@ticketflow.local", "Customer123!", CustomerRole);
        var defaultSupport = await EnsureUserAsync(userManager, "support", "Destek Kullanıcı", "support@ticketflow.local", "Support123!", SupportRole);
        await EnsureUserAsync(userManager, "admin", "Admin Kullanıcı", "admin@ticketflow.local", "Admin123!", AdminRole);
        await EnsureDefaultSupportCategoriesAsync(dbContext, defaultSupport.Id);
    }

    private static async Task<ApplicationUser> EnsureUserAsync(UserManager<ApplicationUser> userManager, string userName, string fullName, string email, string password, string role)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = userName,
                FullName = fullName,
                Email = email,
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(string.Join("; ", createResult.Errors.Select(error => error.Description)));
            }
        }
        else if (!string.Equals(user.UserName, userName, StringComparison.Ordinal))
        {
            var userNameResult = await userManager.SetUserNameAsync(user, userName);
            if (!userNameResult.Succeeded)
            {
                throw new InvalidOperationException(string.Join("; ", userNameResult.Errors.Select(error => error.Description)));
            }
        }

        if (!string.Equals(user.FullName, fullName, StringComparison.Ordinal))
        {
            user.FullName = fullName;
            var updateResult = await userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                throw new InvalidOperationException(string.Join("; ", updateResult.Errors.Select(error => error.Description)));
            }
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            await userManager.AddToRoleAsync(user, role);
        }

        return user;
    }

    private static async Task EnsureDefaultSupportCategoriesAsync(ApplicationDbContext dbContext, string supportUserId)
    {
        var existingCategories = await dbContext.SupportCategoryAssignments
            .Where(assignment => assignment.SupportUserId == supportUserId)
            .Select(assignment => assignment.Category)
            .ToListAsync();

        var existingCategorySet = existingCategories.ToHashSet();
        var categoriesToAdd = Enum.GetValues<TicketCategory>()
            .Where(category => !existingCategorySet.Contains(category))
            .Select(category => new SupportCategoryAssignment
            {
                SupportUserId = supportUserId,
                Category = category
            })
            .ToList();

        if (categoriesToAdd.Count == 0)
        {
            return;
        }

        dbContext.SupportCategoryAssignments.AddRange(categoriesToAdd);
        await dbContext.SaveChangesAsync();
    }
}
