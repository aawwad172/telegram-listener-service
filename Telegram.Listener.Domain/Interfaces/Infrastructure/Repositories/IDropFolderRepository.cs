using Telegram.Listener.Domain.Entities;

namespace Telegram.Listener.Domain.Interfaces.Infrastructure.Repositories;

public interface IDropFolderRepository
{
    Task<LockedBulkJsonFile?> GetLockedJsonFilesAsync(CancellationToken cancellationToken = default);
    Task ArchiveFileAsync(string filePath, CancellationToken cancellationToken);
}
