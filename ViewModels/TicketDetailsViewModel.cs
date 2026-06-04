using ticketflow.Models;

namespace ticketflow.ViewModels;

public class TicketDetailsViewModel
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public TicketStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string CustomerEmail { get; set; } = string.Empty;

    public string? AssignedSupportEmail { get; set; }

    public string? AssignedSupportId { get; set; }

    public bool CanManage { get; set; }

    public bool CanDelete { get; set; }

    public bool CanAssignSupport { get; set; }

    public IReadOnlyList<TicketSupportOptionViewModel> SupportUsers { get; set; } = [];

    public TicketReplyViewModel NewReply { get; set; } = new();

    public IReadOnlyList<TicketReplyItemViewModel> Replies { get; set; } = [];
}

public class TicketSupportOptionViewModel
{
    public string Id { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;
}

public class TicketReplyItemViewModel
{
    public string AuthorEmail { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
