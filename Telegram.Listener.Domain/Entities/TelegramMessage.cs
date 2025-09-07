namespace Telegram.Listener.Domain.Entities;

public class TelegramMessage
{
    /// <summary>
    /// Unique customer identifier (DB-generated/assigned). Do not derive from credentials.
    /// </summary>
    public int CustomerId { get; set; }

    /// <summary> Telegram chat identifier (provided by the user). </summary>
    public string ChatId { get; set; } = null!;

    /// <summary> Telegram bot API key (provided by the user). </summary>
    public required int BotId { get; set; }

    /// <summary> Content of the Telegram message (provided by the user). </summary>
    public required string MessageText { get; set; }

    /// <summary> Recipient's phone number (provided by the user). </summary>
    public required string PhoneNumber { get; set; }

    /// <summary> Indicates the type of message. </summary>
    public required string MessageType { get; set; }

    /// <summary> Campaign Id (Name of the Campaign) </summary>
    public string CampaignId { get; set; } = string.Empty;

    /// <summary> Campaign Description </summary>
    public string CampDescription { get; set; } = string.Empty;

    /// <summary> Date and time when the message is scheduled to be sent. </summary>
    public DateTime? ScheduledSendDateTime { get; set; }

    /// <summary> Processing priority (smaller number = small priority). </summary>
    public required int Priority { get; set; }

    /// <summary> Indicates whether the system approved the message for sending. </summary>
    public required bool IsSystemApproved { get; set; }
}
