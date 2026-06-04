using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ticketflow.Data;
using ticketflow.Models;

namespace ticketflow.Areas.Identity.Pages.Account.Manage;

[Authorize]
public partial class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public IndexModel(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _context = context;
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [TempData]
    public string? StatusMessage { get; set; }

    [BindProperty]
    public ProfileInputModel ProfileInput { get; set; } = new();

    [BindProperty]
    public EmailInputModel EmailInput { get; set; } = new();

    [BindProperty]
    public PasswordInputModel PasswordInput { get; set; } = new();

    public string CurrentEmail { get; set; } = string.Empty;

    public bool HasPassword { get; set; }

    public bool CanEditFullName { get; set; }

    public bool ShowRoleSection { get; set; }

    public IReadOnlyList<string> RoleNames { get; set; } = [];

    public IReadOnlyList<string> SupportCategoryNames { get; set; } = [];

    public class ProfileInputModel
    {
        public string? UserName { get; set; }

        public string? FullName { get; set; }

        public string? PhoneNumber { get; set; }
    }

    public class EmailInputModel
    {
        public string? Email { get; set; }
    }

    public class PasswordInputModel
    {
        [DataType(DataType.Password)]
        public string? CurrentPassword { get; set; }

        [DataType(DataType.Password)]
        public string? NewPassword { get; set; }

        [DataType(DataType.Password)]
        public string? ConfirmPassword { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await GetCurrentUserAsync();
        if (user is null)
        {
            return NotFound("Kullanıcı bulunamadı.");
        }

        await LoadAsync(user);
        return Page();
    }

    public async Task<IActionResult> OnPostUpdateProfileAsync()
    {
        var user = await GetCurrentUserAsync();
        if (user is null)
        {
            return NotFound("Kullanıcı bulunamadı.");
        }

        await LoadMetadataAsync(user);
        ValidateProfileInput();
        if (!ModelState.IsValid)
        {
            await LoadAsync(user, preserveProfile: true);
            return Page();
        }

        var phoneNumber = string.IsNullOrWhiteSpace(ProfileInput.PhoneNumber)
            ? null
            : ProfileInput.PhoneNumber.Trim();

        var currentPhone = await _userManager.GetPhoneNumberAsync(user);
        if (!string.Equals(currentPhone, phoneNumber, StringComparison.Ordinal))
        {
            var phoneResult = await _userManager.SetPhoneNumberAsync(user, phoneNumber);
            if (!phoneResult.Succeeded)
            {
                AddIdentityErrors(phoneResult);
                await LoadAsync(user, preserveProfile: true);
                return Page();
            }
        }

        if (CanEditFullName)
        {
            var fullName = ProfileInput.FullName?.Trim() ?? string.Empty;
            if (!string.Equals(user.FullName, fullName, StringComparison.Ordinal))
            {
                user.FullName = fullName;
                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    AddIdentityErrors(updateResult);
                    await LoadAsync(user, preserveProfile: true);
                    return Page();
                }
            }
        }

        await _signInManager.RefreshSignInAsync(user);
        StatusMessage = "Profil bilgileriniz güncellendi.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUpdateEmailAsync()
    {
        var user = await GetCurrentUserAsync();
        if (user is null)
        {
            return NotFound("Kullanıcı bulunamadı.");
        }

        ValidateEmailInput();
        if (!ModelState.IsValid)
        {
            await LoadAsync(user, preserveEmail: true);
            return Page();
        }

        var email = EmailInput.Email?.Trim() ?? string.Empty;
        var currentEmail = await _userManager.GetEmailAsync(user);

        if (!string.Equals(currentEmail, email, StringComparison.OrdinalIgnoreCase))
        {
            var emailResult = await _userManager.SetEmailAsync(user, email);
            if (!emailResult.Succeeded)
            {
                AddIdentityErrors(emailResult);
                await LoadAsync(user, preserveEmail: true);
                return Page();
            }

            user.EmailConfirmed = true;
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                AddIdentityErrors(updateResult);
                await LoadAsync(user, preserveEmail: true);
                return Page();
            }
        }

        await _signInManager.RefreshSignInAsync(user);
        StatusMessage = "E-posta adresiniz güncellendi.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostChangePasswordAsync()
    {
        var user = await GetCurrentUserAsync();
        if (user is null)
        {
            return NotFound("Kullanıcı bulunamadı.");
        }

        await LoadMetadataAsync(user);
        if (!HasPassword)
        {
            ModelState.AddModelError("PasswordInput.CurrentPassword", "Bu hesap için şifre değişikliği desteklenmiyor.");
        }

        ValidatePasswordInput();
        if (!ModelState.IsValid)
        {
            await LoadAsync(user, preservePassword: true);
            return Page();
        }

        var changeResult = await _userManager.ChangePasswordAsync(
            user,
            PasswordInput.CurrentPassword ?? string.Empty,
            PasswordInput.NewPassword ?? string.Empty);

        if (!changeResult.Succeeded)
        {
            AddIdentityErrors(changeResult);
            await LoadAsync(user, preservePassword: true);
            return Page();
        }

        await _signInManager.RefreshSignInAsync(user);
        StatusMessage = "Şifreniz güncellendi.";
        return RedirectToPage();
    }

    private async Task<ApplicationUser?> GetCurrentUserAsync()
    {
        return await _userManager.GetUserAsync(User);
    }

    private async Task LoadAsync(
        ApplicationUser user,
        bool preserveProfile = false,
        bool preserveEmail = false,
        bool preservePassword = false)
    {
        await LoadMetadataAsync(user);

        if (!preserveProfile)
        {
            ProfileInput = new ProfileInputModel
            {
                UserName = await _userManager.GetUserNameAsync(user) ?? string.Empty,
                FullName = user.FullName,
                PhoneNumber = await _userManager.GetPhoneNumberAsync(user)
            };
        }

        if (!preserveEmail)
        {
            EmailInput = new EmailInputModel
            {
                Email = CurrentEmail
            };
        }

        if (!preservePassword)
        {
            PasswordInput = new PasswordInputModel();
        }
    }

    private async Task LoadMetadataAsync(ApplicationUser user)
    {
        CurrentEmail = await _userManager.GetEmailAsync(user) ?? string.Empty;
        HasPassword = await _userManager.HasPasswordAsync(user);
        var roles = await _userManager.GetRolesAsync(user);
        RoleNames = roles
            .Select(DisplayRoleName)
            .OrderBy(roleName => roleName)
            .ToList();

        CanEditFullName = roles.Contains(SeedData.CustomerRole) || roles.Contains(SeedData.SupportRole);
        ShowRoleSection = roles.Any(role => role != SeedData.CustomerRole);

        if (roles.Contains(SeedData.SupportRole))
        {
            var categories = await _context.SupportCategoryAssignments
                .AsNoTracking()
                .Where(assignment => assignment.SupportUserId == user.Id)
                .Select(assignment => assignment.Category)
                .ToListAsync();

            SupportCategoryNames = categories
                .Select(category => category.GetDisplayName())
                .OrderBy(categoryName => categoryName)
                .ToList();
        }
        else
        {
            SupportCategoryNames = [];
        }
    }

    private void ValidateProfileInput()
    {
        ProfileInput.FullName = ProfileInput.FullName?.Trim();
        if (CanEditFullName && string.IsNullOrWhiteSpace(ProfileInput.FullName))
        {
            ModelState.AddModelError("ProfileInput.FullName", "Ad soyad zorunludur.");
        }
        else if (ProfileInput.FullName?.Length > 120)
        {
            ModelState.AddModelError("ProfileInput.FullName", "Ad soyad en fazla 120 karakter olabilir.");
        }

        ProfileInput.PhoneNumber = string.IsNullOrWhiteSpace(ProfileInput.PhoneNumber)
            ? null
            : ProfileInput.PhoneNumber.Trim();
    }

    private void ValidateEmailInput()
    {
        EmailInput.Email = EmailInput.Email?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(EmailInput.Email))
        {
            ModelState.AddModelError("EmailInput.Email", "E-posta zorunludur.");
        }
        else if (!new EmailAddressAttribute().IsValid(EmailInput.Email))
        {
            ModelState.AddModelError("EmailInput.Email", "Geçerli bir e-posta adresi giriniz.");
        }
    }

    private void ValidatePasswordInput()
    {
        if (string.IsNullOrWhiteSpace(PasswordInput.CurrentPassword))
        {
            ModelState.AddModelError("PasswordInput.CurrentPassword", "Mevcut şifre zorunludur.");
        }

        if (string.IsNullOrWhiteSpace(PasswordInput.NewPassword))
        {
            ModelState.AddModelError("PasswordInput.NewPassword", "Yeni şifre zorunludur.");
        }
        else if (PasswordInput.NewPassword.Length < 6)
        {
            ModelState.AddModelError("PasswordInput.NewPassword", "Yeni şifre en az 6 karakter olmalıdır.");
        }

        if (!string.Equals(PasswordInput.NewPassword, PasswordInput.ConfirmPassword, StringComparison.Ordinal))
        {
            ModelState.AddModelError("PasswordInput.ConfirmPassword", "Şifreler eşleşmiyor.");
        }
    }

    private void AddIdentityErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
    }

    private static string DisplayRoleName(string roleName)
    {
        return roleName switch
        {
            SeedData.CustomerRole => "Müşteri",
            SeedData.SupportRole => "Destek",
            SeedData.AdminRole => "Admin",
            _ => roleName
        };
    }
}
