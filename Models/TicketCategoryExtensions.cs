using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace ticketflow.Models;

public static class TicketCategoryExtensions
{
    public static string GetDisplayName(this TicketCategory category)
    {
        var member = typeof(TicketCategory).GetMember(category.ToString()).FirstOrDefault();
        var display = member?.GetCustomAttribute<DisplayAttribute>();

        return display?.Name ?? category.ToString();
    }
}
