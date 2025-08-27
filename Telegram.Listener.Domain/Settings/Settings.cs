namespace Telegram.Listener.Domain.Settings;

public class DbSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string CommandTimeOut { get; set; } = "30";
}

public class AppSettings
{
    public string LogPath { get; set; } = string.Empty;
    public int LogFlushInterval { get; set; } = 0;
}

public class ListenerSettings
{
    public string DropFolderPath { get; set; } = string.Empty;
    public string ArchiveFolderPath { get; set; } = string.Empty;
    public int ParallelWorkers { get; set; } // from appsettings
    public int IdleDelaySeconds { get; set; } // from appsettings

    public int ResolveWorkers(bool ioBound = true)
    {
        if (ParallelWorkers > 0) return ParallelWorkers;

        int cpu = Environment.ProcessorCount;

        // IO-bound default: allow more concurrency than cores, but clamp to avoid overload
        return ioBound
            ? Math.Clamp(cpu * 2, 2, 32)
            : Math.Max(1, cpu); // CPU-bound default
    }

    public int ResolveIdleDelay() => Math.Max(0, IdleDelaySeconds);
}
