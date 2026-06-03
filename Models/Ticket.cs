using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace ticketflow.Models;

public class Ticket
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Başlık zorunludur.")]
    [StringLength(120, ErrorMessage = "Başlık en fazla 120 karakter olabilir.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Açıklama zorunludur.")]
    [StringLength(2000, MinimumLength = 10, ErrorMessage = "Açıklama 10-2000 karakter arasında olmalıdır.")]
    public string Description { get; set; } = string.Empty;

    public TicketStatus Status { get; set; } = TicketStatus.Open;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    [Required]
    public string CustomerId { get; set; } = string.Empty;

    public IdentityUser? Customer { get; set; }

    public string? AssignedSupportId { get; set; }

    public IdentityUser? AssignedSupport { get; set; }

    public ICollection<TicketReply> Replies { get; set; } = new List<TicketReply>();
}
