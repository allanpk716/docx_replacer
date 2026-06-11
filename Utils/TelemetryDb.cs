using System.IO;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace DocuFiller.Utils;

public sealed class TelemetryDb : IDisposable
{
    private readonly ILogger _logger;
    private readonly SqliteConnection _connection;
    private readonly string _dbPath;
    private bool _disposed;

    public TelemetryDb(string dbDirectory, ILogger logger)
    {
        _logger = logger;
        _dbPath = Path.Combine(dbDirectory, "telemetry.db");

        try
        {
            Directory.CreateDirectory(dbDirectory);
            _connection = OpenWithRecovery(_dbPath);
            EnsureSchema(_connection);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Telemetry DB initialization failed, telemetry disabled");
            throw;
        }
    }

    public void Insert(string payloadJson)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO pending_events (payload, created_at)
            VALUES (@payload, datetime('now'))";
        cmd.Parameters.AddWithValue("@payload", payloadJson);
        cmd.ExecuteNonQuery();
    }

    public List<(long Id, string Payload)> Peek(int batchSize)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var results = new List<(long, string)>();
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT id, payload FROM pending_events ORDER BY id LIMIT @limit";
        cmd.Parameters.AddWithValue("@limit", batchSize);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            results.Add((reader.GetInt64(0), reader.GetString(1)));
        }
        return results;
    }

    public void Delete(IEnumerable<long> ids)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var idList = ids.ToList();
        if (idList.Count == 0) return;

        using var cmd = _connection.CreateCommand();
        var paramNames = new List<string>();
        for (int i = 0; i < idList.Count; i++)
        {
            var paramName = $"@id{i}";
            paramNames.Add(paramName);
            cmd.Parameters.AddWithValue(paramName, idList[i]);
        }
        cmd.CommandText = $"DELETE FROM pending_events WHERE id IN ({string.Join(",", paramNames)})";
        cmd.ExecuteNonQuery();
    }

    public void TruncateOldEvents(int maxDbSizeBytes)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!File.Exists(_dbPath)) return;
        var fi = new FileInfo(_dbPath);
        if (fi.Length < maxDbSizeBytes) return;

        _logger.LogWarning("Telemetry DB exceeded {Size}MB, truncating old events", maxDbSizeBytes / 1024 / 1024);

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "DELETE FROM pending_events WHERE id IN (SELECT id FROM pending_events ORDER BY id LIMIT 500)";
        cmd.ExecuteNonQuery();
    }

    private SqliteConnection OpenWithRecovery(string dbPath)
    {
        var conn = new SqliteConnection($"Data Source={dbPath}");
        conn.Open();

        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "PRAGMA integrity_check";
            var result = cmd.ExecuteScalar()?.ToString();
            if (result != "ok")
            {
                _logger.LogWarning("Telemetry DB corrupted ({Result}), recreating", result);
                conn.Close();
                File.Delete(dbPath);
                conn = new SqliteConnection($"Data Source={dbPath}");
                conn.Open();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Telemetry DB integrity check failed, recreating");
            try { conn.Close(); } catch { }
            try { File.Delete(dbPath); } catch { }
            conn = new SqliteConnection($"Data Source={dbPath}");
            conn.Open();
        }

        return conn;
    }

    private static void EnsureSchema(SqliteConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS pending_events (
                id         INTEGER PRIMARY KEY AUTOINCREMENT,
                payload    TEXT NOT NULL,
                created_at TEXT NOT NULL
            )";
        cmd.ExecuteNonQuery();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _connection?.Close();
        _connection?.Dispose();
    }
}
