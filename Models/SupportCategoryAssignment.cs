namespace ticketflow.Models;

public class SupportCategoryAssignment
{
    public string SupportUserId { get; set; } = string.Empty;

    public ApplicationUser? SupportUser { get; set; }

    public TicketCategory Category { get; set; }
}
