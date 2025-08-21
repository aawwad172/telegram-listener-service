using System.Data;

namespace Service.Template.Domain.Interfaces.Infrastructure;

public interface IDbConnectionFactory
{
    /// <summary>
    /// Creates and opens a new IDbConnection against the configured database.
    /// Caller is responsible for disposing it.
    /// </summary>
    Task<IDbConnection> CreateOpenConnection();
}

