using A2ASerilog;
using Microsoft.Extensions.Options;
using Telegram.Listener.Domain.Entities;
using Telegram.Listener.Domain.Interfaces.Infrastructure.Repositories;
using Telegram.Listener.Domain.Settings;

namespace Telegram.Listener.Infrastructure.Persistence.Repositories;

public class DropFolderRepository(
    IOptionsMonitor<ListenerSettings> monitor)
    : IDropFolderRepository
{
    private readonly string _dropFolderPath = monitor.CurrentValue.DropFolderPath;
    public readonly string _archiveFolderPath = monitor.CurrentValue.ArchiveFolderPath;
    public async Task<LockedBulkJsonFile?> GetLockedJsonFilesAsync(CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(_dropFolderPath))
            Directory.CreateDirectory(_dropFolderPath);


        foreach (string path in Directory.EnumerateFiles(_dropFolderPath, "*.json", SearchOption.TopDirectoryOnly))
        {
            cancellationToken.ThrowIfCancellationRequested();

            FileStream? fs = null;
            try
            {
                // Take an OS-level exclusive lock
                fs = new FileStream(
                    path: path,
                    mode: FileMode.Open,
                    access: FileAccess.Read,
                    share: FileShare.None,
                    bufferSize: 4096,
                    options: FileOptions.SequentialScan);

                // Read content while holding the lock
                using StreamReader reader = new(fs, leaveOpen: true);
                string content = await reader.ReadToEndAsync(cancellationToken);

                // Build the result (transfer ownership of fs to the LockedBulkJsonFile)
                return new LockedBulkJsonFile
                {
                    FileName = Path.GetFileName(path),
                    FullPath = path,
                    Content = content,
                    LockHandle = fs
                };
            }
            catch (IOException)
            {
                LoggerService.Warning("Skipping locked file: {File}", path);
            }
            catch (UnauthorizedAccessException)
            {
                LoggerService.Warning("Skipping inaccessible file: {File}", path);
            }
            catch (Exception ex)
            {
                fs?.Dispose();
                LoggerService.Error("Unexpected error while processing file: {File} {Exception}", path, ex.Message);
                throw;
            }

        }
        return null;
    }
    public async Task ArchiveFileAsync(string fileFullPath, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(_archiveFolderPath))
            Directory.CreateDirectory(_archiveFolderPath);

        if (!File.Exists(fileFullPath))
            LoggerService.Warning("File not found for archiving: {File}", fileFullPath);

        try
        {
            string fileName = Path.GetFileName(fileFullPath);
            string destinationPath = Path.Combine(_archiveFolderPath, fileName);

            File.Move(
            sourceFileName: fileFullPath,
            destFileName: destinationPath,
            overwrite: true);
        }
        finally
        {
            File.Delete(fileFullPath);
        }
        await Task.CompletedTask;
    }
}
