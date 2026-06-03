using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace ticketflow.Models;

public static class TicketStatusExtensions
{
    public static string GetDisplayName(this TicketStatus status)
    {
        var member = typeof(TicketStatus).GetMember(status.ToString()).FirstOrDefault();
        var display = member?.GetCustomAttribute<DisplayAttribute>();

        return display?.Name ?? status.ToString();
    }
}
