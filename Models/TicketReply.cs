using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace ticketflow.Models;

public class TicketReply
{
    public int Id { get; set; }

    public int TicketId { get; set; }

    public Ticket? Ticket { get; set; }

    [Required]
    public string AuthorId { get; set; } = string.Empty;

    public IdentityUser? Author { get; set; }

    [Required(ErrorMessage = "Cevap metni zorunludur.")]
    [StringLength(1500, MinimumLength = 2, ErrorMessage = "Cevap 2-1500 karakter arasında olmalıdır.")]
    public string Message { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
