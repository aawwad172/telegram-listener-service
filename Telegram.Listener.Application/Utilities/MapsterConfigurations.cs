using Mapster;
using Telegram.Listener.Domain.Entities;

namespace Telegram.Listener.Application.Utilities;

public static class MapsterConfigurations
{
    public static void RegisterMappings()
    {
        // Mapping for CampaignMessage where message text comes from the metadata
        TypeAdapterConfig<(BulkMessage metadata, CampaignMessage message), TelegramMessage>
                .NewConfig()
                .Map(dest => dest.CustomerId, src => src.metadata.CustId)
                .Map(dest => dest.ChatId, src => src.message.ChatId ?? null!)
                .Map(dest => dest.BotKey, src => src.metadata.BotKey)
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
                .Map(dest => dest.CustomerId, src => src.metadata.CustId)
                .Map(dest => dest.ChatId, src => src.message.ChatId ?? null!)
                .Map(dest => dest.BotKey, src => src.metadata.BotKey)
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
