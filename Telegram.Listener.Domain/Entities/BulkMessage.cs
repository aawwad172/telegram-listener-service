namespace Telegram.Listener.Domain.Entities;

public class BulkMessage
{
    /// <summary>Customer identifier (FK to Customers table).</summary>
    public int CustomerId { get; set; }

    /// <summary>Bot API key used for this message batch.</summary>
    public int BotId { get; set; }

    /// <summary>The actual text of the message to be sent.</summary>
    public string MsgText { get; set; } = string.Empty;

    /// <summary>Message type (could be 'T' = Telegram, 'S' = SMS, etc.).</summary>
    public string MsgType { get; set; } = string.Empty;

    /// <summary>Processing priority (smaller = higher priority).</summary>
    public short Priority { get; set; }

    /// <summary>File path from which this message was originally loaded (if applicable).</summary>
    public string? FilePath { get; set; }

    /// <summary>Type/format of the input file (Batch, Campaign).</summary>
    public string? FileType { get; set; }

    /// <summary>Campaign identifier this message belongs to.</summary>
    public string CampaignId { get; set; } = string.Empty;

    /// <summary>Optional campaign description.</summary>
    public string? CampDescription { get; set; }

    /// <summary>When the message is scheduled to be sent.</summary>
    public DateTime? ScheduledSendDateTime { get; set; }

    /// <summary>Flag set if approved by system rules.</summary>
    public bool IsSystemApproved { get; set; }

    /// <summary>Flag set if approved manually by admin.</summary>
    public bool IsAdminApproved { get; set; }

    /// <summary>Indicates whether this message has already been processed.</summary>
    public bool IsProcessed { get; set; }
}
