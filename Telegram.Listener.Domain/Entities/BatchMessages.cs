namespace Telegram.Listener.Domain.Entities;

public class BatchMessages
{
    public string? ChatId { get; init; }
    public required string MessageText { get; init; }
    public required string PhoneNumber { get; init; }
}
