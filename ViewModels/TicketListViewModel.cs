using ticketflow.Models;

namespace ticketflow.ViewModels;

public class TicketListViewModel
{
    public IReadOnlyList<TicketListItemViewModel> Tickets { get; set; } = [];

    public TicketStatus? SelectedStatus { get; set; }

    public int OpenCount { get; set; }

    public int ResolvedCount { get; set; }

    public int ClosedCount { get; set; }

    public bool IsStaffView { get; set; }
}

public class TicketListItemViewModel
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public TicketStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public string CustomerEmail { get; set; } = string.Empty;

    public string? AssignedSupportEmail { get; set; }

    public int ReplyCount { get; set; }
}
