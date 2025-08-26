namespace Telegram.Listener.Domain.Entities;

public class CampaignMessage
{
    public string? ChatId { get; init; }
    public required string PhoneNumber { get; init; }
}
