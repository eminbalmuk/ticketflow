using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace ticketflow.Models;

public class ApplicationUser : IdentityUser
{
    [StringLength(120)]
    public string FullName { get; set; } = string.Empty;
}
