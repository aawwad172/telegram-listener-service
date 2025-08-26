using Telegram.Listener.Domain.Entities;

namespace Telegram.Listener.Domain.Interfaces.Infrastructure.Repositories;

public interface IMessageRepository
{
    Task<BulkMessage> GetBulkMessageByCampaignIdAsync(string campaignId, CancellationToken cancellationToken);
    Task AddBatchAsync(List<TelegramMessage> messages, CancellationToken cancellationToken);
    Task ArchiveDbFileAsync(string campaignId, CancellationToken cancellationToken);
}
