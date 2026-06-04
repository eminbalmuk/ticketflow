using ticketflow.Models;

namespace ticketflow.ViewModels;

public class CustomerSearchViewModel
{
    public string? CustomerQuery { get; set; }

    public bool IsSupportView { get; set; }

    public IReadOnlyList<CustomerSearchResultViewModel> CustomerResults { get; set; } = [];
}

public class CustomerSearchResultViewModel
{
    public string Id { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public IReadOnlyList<CustomerSearchTicketViewModel> Tickets { get; set; } = [];

    public int OpenCount => Tickets.Count(ticket => ticket.Status == TicketStatus.Open);

    public int ResolvedCount => Tickets.Count(ticket => ticket.Status == TicketStatus.Resolved);

    public int ClosedCount => Tickets.Count(ticket => ticket.Status == TicketStatus.Closed);
}

public class CustomerSearchTicketViewModel
{
    public string CustomerId { get; set; } = string.Empty;

    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public TicketStatus Status { get; set; }

    public TicketCategory Category { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? AssignedSupportEmail { get; set; }
}
