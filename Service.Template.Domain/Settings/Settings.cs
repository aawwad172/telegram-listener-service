namespace Service.Template.Domain.Settings;

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
