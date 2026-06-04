namespace ticketflow.ViewModels;

public class NotificationMenuItemViewModel
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public bool IsUnread { get; set; }
}
