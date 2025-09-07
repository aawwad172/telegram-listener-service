using Mapster;
using Telegram.Listener.Domain.Entities;

namespace Telegram.Listener.Application.Utilities;

public static class MapsterConfigurations
{
    /// <summary>
    /// Registers Mapster mappings that convert source tuples into TelegramMessage instances.
    /// </summary>
    /// <remarks>
    /// Adds two TypeAdapterConfig mappings:
    /// 1. (BulkMessage metadata, CampaignMessage message) -> TelegramMessage — uses metadata.MsgText for MessageText.
    /// 2. (BulkMessage metadata, BatchMessages message) -> TelegramMessage — uses message.MessageText for MessageText.
    ///
    /// In both mappings:
    /// - CustomerId, BotId, MessageType, IsSystemApproved, Priority, CampaignId, CampDescription and ScheduledSendDateTime
    ///   are taken from the metadata tuple element.
    /// - ChatId is mapped to null when the source ChatId is null or whitespace; otherwise the source ChatId is used.
    /// - PhoneNumber is taken from the message/batch item element.
    ///
    /// This method has the side effect of registering these mappings with Mapster's TypeAdapterConfig.
    /// </remarks>
    public static void RegisterMappings()
    {
        // Mapping for CampaignMessage where message text comes from the metadata
        TypeAdapterConfig<(BulkMessage metadata, CampaignMessage message), TelegramMessage>
                .NewConfig()
                .Map(dest => dest.CustomerId, src => src.metadata.CustomerId)
                .Map(dest => dest.ChatId, src => string.IsNullOrWhiteSpace(src.message.ChatId) ? null : src.message.ChatId)
                .Map(dest => dest.BotId, src => src.metadata.BotId)
                .Map(dest => dest.MessageText, src => src.metadata.MsgText) // Use metadata message text
                .Map(dest => dest.PhoneNumber, src => src.message.PhoneNumber)
                .Map(dest => dest.MessageType, src => src.metadata.MsgType)
                .Map(dest => dest.IsSystemApproved, src => src.metadata.IsSystemApproved)
                .Map(dest => dest.Priority, src => src.metadata.Priority)
                .Map(dest => dest.CampaignId, src => src.metadata.CampaignId)
                .Map(dest => dest.CampDescription, src => src.metadata.CampDesc)
                .Map(dest => dest.ScheduledSendDateTime, src => src.metadata.ScheduledSendDateTime);

        // Mapping for BatchMessages where message text comes from the batch item
        TypeAdapterConfig<(BulkMessage metadata, BatchMessages message), TelegramMessage>
                .NewConfig()
                .Map(dest => dest.CustomerId, src => src.metadata.CustomerId)
                .Map(dest => dest.ChatId, src => string.IsNullOrWhiteSpace(src.message.ChatId) ? null : src.message.ChatId)
                .Map(dest => dest.BotId, src => src.metadata.BotId)
                .Map(dest => dest.MessageText, src => src.message.MessageText) // from batch item
                .Map(dest => dest.PhoneNumber, src => src.message.PhoneNumber)
                .Map(dest => dest.MessageType, src => src.metadata.MsgType)
                .Map(dest => dest.IsSystemApproved, src => src.metadata.IsSystemApproved)
                .Map(dest => dest.Priority, src => src.metadata.Priority)
                .Map(dest => dest.CampaignId, src => src.metadata.CampaignId)
                .Map(dest => dest.CampDescription, src => src.metadata.CampDesc)
                .Map(dest => dest.ScheduledSendDateTime, src => src.metadata.ScheduledSendDateTime);
    }
}
