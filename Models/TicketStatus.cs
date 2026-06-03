using System.ComponentModel.DataAnnotations;

namespace ticketflow.Models;

public enum TicketStatus
{
    [Display(Name = "Açık")]
    Open = 1,

    [Display(Name = "Çözüldü")]
    Resolved = 2,

    [Display(Name = "Kapandı")]
    Closed = 3
}
