using System.ComponentModel.DataAnnotations;

namespace ticketflow.Models;

public enum TicketCategory
{
    [Display(Name = "Telefon")]
    Phone = 1,

    [Display(Name = "Tablet")]
    Tablet = 2,

    [Display(Name = "Kamera")]
    Camera = 3,

    [Display(Name = "Kulaklık")]
    Headphones = 4,

    [Display(Name = "Televizyon")]
    Television = 5,

    [Display(Name = "Monitör")]
    Monitor = 6,

    [Display(Name = "Beyaz Eşya")]
    HomeAppliance = 7
}
