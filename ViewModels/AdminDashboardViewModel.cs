using System.ComponentModel.DataAnnotations;
using ticketflow.Models;

namespace ticketflow.ViewModels;

public class AdminDashboardViewModel
{
    public string? CustomerQuery { get; set; }

    public IReadOnlyList<AdminCustomerResultViewModel> CustomerResults { get; set; } = [];

    public AdminSupportInputModel SupportInput { get; set; } = new();

    public IReadOnlyList<AdminSupportUserViewModel> SupportUsers { get; set; } = [];

    public int OpenTicketCount { get; set; }

    public int UnassignedTicketCount { get; set; }
}

public class AdminCustomerResultViewModel
{
    public string Id { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public IReadOnlyList<AdminTicketSummaryViewModel> Tickets { get; set; } = [];

    public int OpenCount => Tickets.Count(ticket => ticket.Status == TicketStatus.Open);

    public int ResolvedCount => Tickets.Count(ticket => ticket.Status == TicketStatus.Resolved);

    public int ClosedCount => Tickets.Count(ticket => ticket.Status == TicketStatus.Closed);
}

public class AdminTicketSummaryViewModel
{
    public string CustomerId { get; set; } = string.Empty;

    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public TicketStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? AssignedSupportEmail { get; set; }
}

public class AdminSupportInputModel
{
    [Required(ErrorMessage = "E-posta zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
    [Display(Name = "E-posta")]
    public string Email { get; set; } = string.Empty;

    [StringLength(40, ErrorMessage = "Kullanıcı adı en az {2}, en fazla {1} karakter olmalıdır.", MinimumLength = 3)]
    [RegularExpression(@"^[a-zA-Z0-9._-]+$", ErrorMessage = "Kullanıcı adı sadece harf, rakam, nokta, tire ve alt çizgi içerebilir.")]
    [Display(Name = "Kullanıcı adı")]
    public string? UserName { get; set; }

    [StringLength(120, ErrorMessage = "Ad soyad en fazla {1} karakter olmalıdır.")]
    [Display(Name = "Ad soyad")]
    public string? FullName { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Geçici şifre")]
    public string? TemporaryPassword { get; set; }
}

public class AdminSupportUserViewModel
{
    public string UserName { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
}
