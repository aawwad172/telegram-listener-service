namespace Telegram.Listener.Domain.Interfaces.Application;

public interface IQueuedMessagesService
{
    Task ProcessQueuedMessagesAsync(CancellationToken cancellationToken);
}
