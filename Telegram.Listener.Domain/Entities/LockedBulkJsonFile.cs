namespace Telegram.Listener.Domain.Entities;

public sealed record LockedBulkJsonFile : IAsyncDisposable
{
    /// <summary>Full path to the file on disk (including folder + name + extension).</summary>
    public required string FullPath { get; init; }

    /// <summary>File name without extension (e.g., "campaign123").</summary>
    public required string FileName { get; init; }

    /// <summary>Raw content of the file (e.g., JSON text).</summary>
    public required string Content { get; init; }

    /// <summary>OS-level lock handle. Kept open until Dispose.</summary>
    public required FileStream LockHandle { get; init; }

    public ValueTask DisposeAsync()
    {
        LockHandle.Dispose(); // releases the file lock
        return ValueTask.CompletedTask;
    }
}
