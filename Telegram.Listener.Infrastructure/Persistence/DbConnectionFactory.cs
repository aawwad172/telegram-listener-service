using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Telegram.Listener.Domain.Interfaces.Infrastructure;
using Telegram.Listener.Domain.Settings;
using System.Data;

namespace Telegram.Listener.Infrastructure.Persistence;

public class DbConnectionFactory(IOptionsMonitor<DbSettings> options) : IDbConnectionFactory
{
    private readonly IOptionsMonitor<DbSettings> _options = options;

    public async Task<IDbConnection> CreateOpenConnection()
    {
        SqlConnection conn = new(_options.CurrentValue.ConnectionString);
        await conn.OpenAsync().ConfigureAwait(false);
        return conn;
    }
}
