using Microsoft.AspNetCore.Identity;

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
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

        foreach (var role in new[] { CustomerRole, SupportRole, AdminRole })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        await EnsureUserAsync(userManager, "customer@ticketflow.local", "Customer123!", CustomerRole);
        await EnsureUserAsync(userManager, "support@ticketflow.local", "Support123!", SupportRole);
        await EnsureUserAsync(userManager, "admin@ticketflow.local", "Admin123!", AdminRole);
    }

    private static async Task EnsureUserAsync(UserManager<IdentityUser> userManager, string email, string password, string role)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new IdentityUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(string.Join("; ", createResult.Errors.Select(error => error.Description)));
            }
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            await userManager.AddToRoleAsync(user, role);
        }
    }
}
