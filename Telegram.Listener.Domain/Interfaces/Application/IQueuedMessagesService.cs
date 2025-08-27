namespace Telegram.Listener.Domain.Interfaces.Application;

public interface IQueuedMessagesService
{
    Task<bool> ProcessQueuedMessagesAsync(CancellationToken cancellationToken = default);
}
