using System.Data;
using A2ASerilog;
using Microsoft.Data.SqlClient;
using Telegram.Listener.Domain.Entities;
using Telegram.Listener.Domain.Interfaces.Infrastructure;
using Telegram.Listener.Domain.Interfaces.Infrastructure.Repositories;

namespace Telegram.Listener.Infrastructure.Persistence.Repositories;

public class MessageRepository(IDbConnectionFactory connectionFactory) : IMessageRepository
{
    private readonly IDbConnectionFactory _connectionFactory = connectionFactory;
    public async Task AddBatchAsync(List<TelegramMessage> messages, CancellationToken cancellationToken = default)
    {
        if (messages == null || messages.Count == 0)
            return;

        // Build a DataTable matching dbo.TelegramMessage_Tvp
        DataTable tvp = new DataTable();
        tvp.Columns.Add("CustomerId", typeof(int));
        tvp.Columns.Add("ChatId", typeof(string));
        tvp.Columns.Add("BotKey", typeof(string));
        tvp.Columns.Add("PhoneNumber", typeof(string));
        tvp.Columns.Add("MessageText", typeof(string));
        tvp.Columns.Add("MessageType", typeof(string));
        tvp.Columns.Add("ScheduledSendDateTime", typeof(DateTime));
        tvp.Columns.Add("Priority", typeof(short));
        tvp.Columns.Add("CampaignId", typeof(string));
        tvp.Columns.Add("CampDescription", typeof(string));
        tvp.Columns.Add("IsSystemApproved", typeof(bool));

        foreach (TelegramMessage m in messages)
        {
            tvp.Rows.Add(
                m.CustomerId,
                (object?)m.ChatId ?? DBNull.Value,
                m.BotKey,
                m.PhoneNumber,
                m.MessageText,
                m.MessageType,
                (object?)m.ScheduledSendDateTime ?? DBNull.Value,
                (short)m.Priority,
                string.IsNullOrWhiteSpace(m.CampaignId) ? DBNull.Value : m.CampaignId,
                string.IsNullOrWhiteSpace(m.CampDescription) ? DBNull.Value : m.CampDescription,
                m.IsSystemApproved
            );
        }

        using IDbConnection conn = await _connectionFactory.CreateOpenConnection();

        using SqlCommand cmd = (SqlCommand)conn.CreateCommand();
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "dbo.usp_ReadyTable_BulkEnqueue";   // your SP that inserts into ReadyTable

        SqlParameter p = cmd.Parameters.Add("@Batch", SqlDbType.Structured);
        p.TypeName = "dbo.TelegramMessage_Tvp";          // your TVP type name
        p.Value = tvp;

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// This method is being used to get the record that represent the metadata of the bulk message file that we have in the directory (drop folder).
    /// </summary>
    /// <param name="campaignId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async Task<BulkMessage> GetBulkMessageByCampaignIdAsync(string campaignId, CancellationToken cancellationToken = default)
    {
        using IDbConnection conn = await _connectionFactory.CreateOpenConnection();
        using SqlCommand cmd = (SqlCommand)conn.CreateCommand();
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "usp_GetBulkMessageByCampaignId";
        cmd.Parameters.Add(new SqlParameter("@CampaignId", SqlDbType.NVarChar) { Value = campaignId });

        using SqlDataReader reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return new BulkMessage
            {
                CustId = reader.GetInt32(reader.GetOrdinal("CustID")),
                BotKey = reader.GetString(reader.GetOrdinal("BotKey")),
                MsgText = reader.IsDBNull(reader.GetOrdinal("MsgText")) ? null! : reader.GetString(reader.GetOrdinal("MsgText")),
                MsgType = reader.GetString(reader.GetOrdinal("MsgType")),
                Priority = reader.GetInt16(reader.GetOrdinal("Priority")),
                FilePath = reader.GetString(reader.GetOrdinal("FilePath")),
                FileType = reader.GetString(reader.GetOrdinal("FileType")),
                CampaignId = reader.GetString(reader.GetOrdinal("CampaignID")),
                CampDesc = reader.IsDBNull(reader.GetOrdinal("CampDesc")) ? null : reader.GetString(reader.GetOrdinal("CampDesc")),
                ScheduledSendDateTime = reader.GetDateTime(reader.GetOrdinal("ScheduledSendDateTime")),
                IsSystemApproved = reader.GetBoolean(reader.GetOrdinal("isSystemApproved")),
                IsAdminApproved = reader.GetBoolean(reader.GetOrdinal("isAdminApproved")),
                IsProcessed = reader.GetBoolean(reader.GetOrdinal("IsProcessed")),
            };
        }

        LoggerService.Info("No bulk message found with CampaignId: {CampaignId}", campaignId);
        return null!;
    }

    public async Task ArchiveDbFileAsync(string campaignId, CancellationToken cancellationToken = default)
    {
        IDbConnection conn = await _connectionFactory.CreateOpenConnection();
        using SqlCommand cmd = (SqlCommand)conn.CreateCommand();
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "usp_ArchiveTelegramFileByCampaignId";

        cmd.Parameters.Add(new SqlParameter("@CampaignId", SqlDbType.NVarChar) { Value = campaignId });

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }
}
