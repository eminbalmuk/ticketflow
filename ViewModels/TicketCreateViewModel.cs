using System.ComponentModel.DataAnnotations;
using ticketflow.Models;

namespace ticketflow.ViewModels;

public class TicketCreateViewModel
{
    [Required(ErrorMessage = "Başlık zorunludur.")]
    [StringLength(120, ErrorMessage = "Başlık en fazla 120 karakter olabilir.")]
    [Display(Name = "Başlık")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Kategori seçimi zorunludur.")]
    [EnumDataType(typeof(TicketCategory), ErrorMessage = "Geçerli bir kategori seçiniz.")]
    [Display(Name = "Kategori")]
    public TicketCategory? Category { get; set; }

    [Required(ErrorMessage = "Açıklama zorunludur.")]
    [StringLength(2000, MinimumLength = 10, ErrorMessage = "Açıklama 10-2000 karakter arasında olmalıdır.")]
    [Display(Name = "Açıklama")]
    public string Description { get; set; } = string.Empty;
}
