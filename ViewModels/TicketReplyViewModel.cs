using System.ComponentModel.DataAnnotations;

namespace ticketflow.ViewModels;

public class TicketReplyViewModel
{
    [Required(ErrorMessage = "Cevap metni zorunludur.")]
    [StringLength(1500, MinimumLength = 2, ErrorMessage = "Cevap 2-1500 karakter arasında olmalıdır.")]
    [Display(Name = "Cevap")]
    public string Message { get; set; } = string.Empty;
}
