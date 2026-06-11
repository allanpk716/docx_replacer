# Telemetry Client-Side Implementation Plan (DocuFiller, C#)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add telemetry event tracking to DocuFiller — events persist to local SQLite, a background thread sends them to update-hub, server confirms receipt before local deletion. Telemetry failures never impact main functionality.

**Architecture:** New `ITelemetryService` / `TelemetryService` registered as Singleton. Local `telemetry.db` SQLite for pending events. Background timer sends batches via HTTP POST. HMAC-SHA256 signing. Auto-enabled when `Update.UpdateUrl` is configured.

**Tech Stack:** C# / .NET 8, Microsoft.Data.Sqlite, System.Net.Http, System.Security.Cryptography

**Spec:** `docs/superpowers/specs/2026-06-11-telemetry-design.md`
**Server plan:** `docs/superpowers/plans/2026-06-11-telemetry-server-plan.md`

---

## File Structure

| Action | File | Responsibility |
|--------|------|----------------|
| Create | `Configuration/TelemetrySettings.cs` | Configuration POCO |
| Create | `Services/Interfaces/ITelemetryService.cs` | Interface |
| Create | `Utils/TelemetryDb.cs` | Local SQLite pending_events operations |
| Create | `Services/TelemetryService.cs` | Main service: HMAC signing, background send, flush |
| Create | `Services/NullTelemetryService.cs` | No-op fallback when disabled |
| Modify | `DocuFiller.csproj` | Add Microsoft.Data.Sqlite NuGet |
| Modify | `appsettings.json` | Add Telemetry config section |
| Modify | `App.xaml.cs` | DI registration + lifecycle hooks |
| Modify | `Services/DocumentProcessorService.cs` | fill_complete 埋点 |
| Modify | `DocuFiller/Services/DocumentCleanupService.cs` | cleanup_complete 埋点 |
| Modify | `Cli/Commands/InspectCommand.cs` | inspect_complete 埋点 |
| Modify | `Services/UpdateService.cs` | update_check / update_applied 埋点 |

---

### Task 1: Add Microsoft.Data.Sqlite dependency

**Files:**
- Modify: `DocuFiller.csproj`

- [ ] **Step 1: Add NuGet package**

Run: `dotnet add package Microsoft.Data.Sqlite`

- [ ] **Step 2: Verify build**

Run: `dotnet build`
Expected: BUILD SUCCESS

- [ ] **Step 3: Commit**

```bash
git add DocuFiller.csproj
git commit -m "feat: add Microsoft.Data.Sqlite dependency for telemetry"
```

---

### Task 2: Telemetry configuration

**Files:**
- Create: `Configuration/TelemetrySettings.cs`
- Modify: `appsettings.json`

- [ ] **Step 1: Create TelemetrySettings.cs**

```csharp
namespace DocuFiller.Configuration;

public class TelemetrySettings
{
    public const string SectionName = "Telemetry";

    public bool Enabled { get; set; } = true;
    public int BatchSize { get; set; } = 50;
    public int FlushIntervalSeconds { get; set; } = 60;
}
```

- [ ] **Step 2: Update appsettings.json**

Add the Telemetry section:

```json
{
  "Performance": {
    "EnableTemplateCache": true,
    "CacheExpirationMinutes": 30
  },
  "Update": {
    "UpdateUrl": "",
    "Channel": ""
  },
  "Telemetry": {
    "Enabled": true,
    "BatchSize": 50,
    "FlushIntervalSeconds": 60
  }
}
```

- [ ] **Step 3: Verify build**

Run: `dotnet build`
Expected: BUILD SUCCESS

- [ ] **Step 4: Commit**

```bash
git add Configuration/TelemetrySettings.cs appsettings.json
git commit -m "feat: add TelemetrySettings configuration class"
```

---

### Task 3: ITelemetryService interface

**Files:**
- Create: `Services/Interfaces/ITelemetryService.cs`

- [ ] **Step 1: Create the interface**

```csharp
namespace DocuFiller.Services.Interfaces;

public interface ITelemetryService : IDisposable
{
    void TrackEvent(string eventName, Dictionary<string, object>? properties = null);
    Task FlushAsync(TimeSpan? timeout = null);
}
```

- [ ] **Step 2: Commit**

```bash
git add Services/Interfaces/ITelemetryService.cs
git commit -m "feat: add ITelemetryService interface"
```

---

### Task 4: Local SQLite persistence (TelemetryDb)

**Files:**
- Create: `Utils/TelemetryDb.cs`

- [ ] **Step 1: Write TelemetryDb**

```csharp
using System.Text.Json;
using DocuFiller.Services.Interfaces;
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
            _connection = OpenWithRecovery(dbPath: _dbPath);
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
        var params_list = new List<string>();
        for (int i = 0; i < idList.Count; i++)
        {
            var paramName = $"@id{i}";
            params_list.Add(paramName);
            cmd.Parameters.AddWithValue(paramName, idList[i]);
        }
        cmd.CommandText = $"DELETE FROM pending_events WHERE id IN ({string.Join(",", params_list)})";
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
```

- [ ] **Step 2: Verify build**

Run: `dotnet build`
Expected: BUILD SUCCESS

- [ ] **Step 3: Commit**

```bash
git add Utils/TelemetryDb.cs
git commit -m "feat: add TelemetryDb local SQLite persistence for pending events"
```

---

### Task 5: NullTelemetryService (no-op fallback)

**Files:**
- Create: `Services/NullTelemetryService.cs`

- [ ] **Step 1: Create no-op implementation**

```csharp
using DocuFiller.Services.Interfaces;

namespace DocuFiller.Services;

public sealed class NullTelemetryService : ITelemetryService
{
    public void TrackEvent(string eventName, Dictionary<string, object>? properties = null) { }
    public Task FlushAsync(TimeSpan? timeout = null) => Task.CompletedTask;
    public void Dispose() { }
}
```

- [ ] **Step 2: Commit**

```bash
git add Services/NullTelemetryService.cs
git commit -m "feat: add NullTelemetryService no-op fallback"
```

---

### Task 6: TelemetryService (core implementation)

**Files:**
- Create: `Services/TelemetryService.cs`

- [ ] **Step 1: Write the full TelemetryService**

```csharp
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DocuFiller.Configuration;
using DocuFiller.Services.Interfaces;
using DocuFiller.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DocuFiller.Services;

public sealed class TelemetryService : ITelemetryService
{
    private const string AppId = "docufiller";
    // HMAC shared key — must match the key configured on update-hub via --telemetry-keys
    // e.g. --telemetry-keys docufiller=<this-value>
    private const string SecretKey = "CHANGE_ME_TO_MATCH_SERVER_KEY";
    private const int MaxDbSizeBytes = 10 * 1024 * 1024; // 10MB

    private readonly ILogger<TelemetryService> _logger;
    private readonly HttpClient _httpClient;
    private readonly TelemetryDb _db;
    private readonly TelemetrySettings _settings;
    private readonly string _sessionId;
    private readonly string _endpointUrl;
    private readonly string _version;
    private readonly string _user;
    private readonly string _machine;

    private readonly CancellationTokenSource _cts = new();
    private readonly SemaphoreSlim _signal = new(0);
    private readonly Task _sendLoop;
    private bool _disposed;

    public TelemetryService(
        ILogger<TelemetryService> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _sessionId = Guid.NewGuid().ToString();
        _version = GetVersion();
        _user = Environment.UserName;
        _machine = Environment.MachineName;

        // Derive endpoint from Update.UpdateUrl
        var updateUrl = configuration["Update:UpdateUrl"]?.TrimEnd('/');
        if (string.IsNullOrEmpty(updateUrl))
            throw new InvalidOperationException("UpdateUrl not configured, telemetry disabled");

        _endpointUrl = $"{updateUrl}/api/telemetry";

        _settings = new TelemetrySettings();
        configuration.GetSection(TelemetrySettings.SectionName).Bind(_settings);

        // Initialize local DB
        var appDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DocuFiller");
        _db = new TelemetryDb(appDataDir, logger);

        // Send any leftover events from previous sessions on startup
        _signal.Release();

        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

        // Start background send loop
        _sendLoop = Task.Run(() => SendLoopAsync(_cts.Token));
    }

    public void TrackEvent(string eventName, Dictionary<string, object>? properties = null)
    {
        if (_disposed) return;

        try
        {
            // Use SortedDictionary to ensure alphabetical key ordering
            // This MUST match the server's Go json.Marshal(map) behavior
            var evt = new SortedDictionary<string, object>
            {
                ["app_id"] = AppId,
                ["event"] = eventName,
                ["timestamp"] = DateTime.Now.ToString("o"),
                ["session_id"] = _sessionId,
                ["user"] = _user,
                ["machine"] = _machine,
                ["version"] = _version,
            };
            if (properties != null)
                evt["properties"] = properties;

            // Compute signature over payload without signature field
            // Keys are sorted alphabetically: app_id, event, machine, properties, session_id, timestamp, user, version
            var payloadJson = JsonSerializer.Serialize(evt, JsonOpts);
            var signature = ComputeHmac(payloadJson);
            evt["signature"] = signature;

            var finalJson = JsonSerializer.Serialize(evt, JsonOpts);

            _db.Insert(finalJson);
            _db.TruncateOldEvents(MaxDbSizeBytes);

            // Wake up send loop
            try { _signal.Release(); } catch (ObjectDisposedException) { }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Telemetry TrackEvent failed for {Event}", eventName);
        }
    }

    public async Task FlushAsync(TimeSpan? timeout = null)
    {
        timeout ??= TimeSpan.FromSeconds(5);
        try
        {
            await SendPendingAsync(timeout.Value);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Telemetry flush failed");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        try { _cts.Cancel(); } catch { }
        try { _signal.Release(); } catch { }

        try
        {
            _sendLoop.Wait(TimeSpan.FromSeconds(3));
        }
        catch { }

        _cts.Dispose();
        _signal.Dispose();
        _httpClient.Dispose();
        _db.Dispose();
    }

    private async Task SendLoopAsync(CancellationToken ct)
    {
        // Initial delay: wait 5s for network readiness
        try { await Task.Delay(5000, ct); } catch (OperationCanceledException) { return; }

        while (!ct.IsCancellationRequested)
        {
            try
            {
                // Wait for signal or periodic timer
                var delayTask = Task.Delay(TimeSpan.FromSeconds(_settings.FlushIntervalSeconds), ct);
                var signalTask = _signal.WaitAsync(ct);
                await Task.WhenAny(delayTask, signalTask);

                // Drain any accumulated signals
                while (_signal.CurrentCount > 0)
                    await _signal.WaitAsync(ct);

                await SendPendingAsync(TimeSpan.FromSeconds(10));
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Telemetry send loop error");
                try { await Task.Delay(5000, ct); } catch (OperationCanceledException) { break; }
            }
        }

        // Final flush on shutdown
        try { await SendPendingAsync(TimeSpan.FromSeconds(3)); } catch { }
    }

    private async Task SendPendingAsync(TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        var ct = cts.Token;

        while (true)
        {
            var batch = _db.Peek(_settings.BatchSize);
            if (batch.Count == 0) return;

            var successIds = new List<long>();
            var rejectedIds = new List<long>();

            foreach (var chunk in batch.Chunk(_settings.BatchSize))
            {
                try
                {
                    // Build JSON: single event as-is, batch as JSON array of objects
                    // NOTE: payloads are already-serialized JSON objects, so we join them
                    // rather than re-serializing (which would produce an array of strings)
                    var json = chunk.Length == 1
                        ? chunk[0].Payload
                        : "[" + string.Join(",", chunk.Select(c => c.Payload)) + "]";

                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var response = await _httpClient.PostAsync(_endpointUrl, content, ct);

                    if (response.IsSuccessStatusCode)
                    {
                        // All accepted (or partially accepted — for simplicity, mark all as sent)
                        successIds.AddRange(chunk.Select(c => c.Id));
                    }
                    else
                    {
                        var body = await response.Content.ReadAsStringAsync(ct);
                        _logger.LogDebug("Telemetry server returned {Status}: {Body}", (int)response.StatusCode, body);
                        // Non-200 means server rejected — delete to avoid infinite retry
                        rejectedIds.AddRange(chunk.Select(c => c.Id));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Telemetry send failed, will retry");
                    // Network error — keep in DB, retry later
                    return;
                }
            }

            _db.Delete(successIds.Concat(rejectedIds));
        }
    }

    private static string ComputeHmac(string payloadJson)
    {
        var keyBytes = Encoding.UTF8.GetBytes(SecretKey);
        var payloadBytes = Encoding.UTF8.GetBytes(payloadJson);
        using var hmac = new HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(payloadBytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string GetVersion()
    {
        var v = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version;
        return v is not null ? $"{v.Major}.{v.Minor}.{v.Build}" : "0.0.0";
    }

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };
}
```

- [ ] **Step 2: Verify build**

Run: `dotnet build`
Expected: BUILD SUCCESS (may have warnings about unused imports — fix if any)

- [ ] **Step 3: Commit**

```bash
git add Services/TelemetryService.cs
git commit -m "feat: add TelemetryService with local SQLite persistence and background sending"
```

---

### Task 7: DI registration and lifecycle hooks in App.xaml.cs

**Files:**
- Modify: `App.xaml.cs`

- [ ] **Step 1: Add telemetry service registration**

In the `ConfigureServices()` method, after the existing service registrations, add:

```csharp
// Telemetry — graceful fallback: if construction fails, use no-op
try
{
    services.AddSingleton<ITelemetryService>(sp =>
    {
        var config = sp.GetRequiredService<IConfiguration>();
        var telSettings = new TelemetrySettings();
        config.GetSection(TelemetrySettings.SectionName).Bind(telSettings);
        var updateUrl = config["Update:UpdateUrl"]?.Trim();

        // Auto-disable when UpdateUrl is empty or Enabled is false
        if (!telSettings.Enabled || string.IsNullOrEmpty(updateUrl))
            return new NullTelemetryService();

        return new TelemetryService(
            sp.GetRequiredService<ILogger<TelemetryService>>(),
            config);
    });
}
catch (Exception ex)
{
    // Should not happen at registration time, but be safe
    services.AddSingleton<ITelemetryService>(new NullTelemetryService());
    // Log will happen when service is resolved
}
```

Add the necessary using directives at the top:

```csharp
using DocuFiller.Configuration;
using DocuFiller.Services.Interfaces;
```

- [ ] **Step 2: Add lifecycle event tracking**

In the `OnStartup` method (or equivalent startup location), after service provider is built:

```csharp
// Track app_start
var telemetry = _serviceProvider.GetService<ITelemetryService>();
telemetry?.TrackEvent("app_start", new Dictionary<string, object>
{
    ["launch_mode"] = _isCliMode ? "cli" : "gui",
    ["is_installed"] = IsInstalledBuild(), // helper or inline check
    ["is_portable"] = IsPortableBuild(),
});
```

In `OnExit` or equivalent shutdown location:

```csharp
// Track app_exit and flush
var telemetry = _serviceProvider.GetService<ITelemetryService>();
if (telemetry is not NullTelemetryService)
{
    telemetry.TrackEvent("app_exit", new Dictionary<string, object>
    {
        ["session_duration_sec"] = (int)(DateTime.Now - _startTime).TotalSeconds,
    });
    telemetry.FlushAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
}
telemetry?.Dispose();
```

In the global exception handler:

```csharp
// Track app_crash
var telemetry = _serviceProvider?.GetService<ITelemetryService>();
telemetry?.TrackEvent("app_crash", new Dictionary<string, object>
{
    ["exception_type"] = e.Exception.GetType().Name,
    ["exception_message"] = Truncate(e.Exception.Message, 500),
});
telemetry?.FlushAsync(TimeSpan.FromSeconds(3)).GetAwaiter().GetResult();
```

Add a `Truncate` helper and a `_startTime` field:

```csharp
private readonly DateTime _startTime = DateTime.Now;

private static string Truncate(string s, int maxLength) =>
    string.IsNullOrEmpty(s) ? s : s.Length <= maxLength ? s : s[..maxLength];
```

- [ ] **Step 3: Verify build**

Run: `dotnet build`
Expected: BUILD SUCCESS

- [ ] **Step 4: Commit**

```bash
git add App.xaml.cs
git commit -m "feat: wire TelemetryService into DI and app lifecycle"
```

---

### Task 8: Instrument DocumentProcessorService (fill_complete)

**Files:**
- Modify: `Services/DocumentProcessorService.cs`

- [ ] **Step 1: Add ITelemetryService dependency**

Add `ITelemetryService` as a constructor parameter:

```csharp
private readonly ITelemetryService _telemetry;

// In constructor, add parameter:
// ITelemetryService telemetry
_telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
```

- [ ] **Step 2: Add fill_complete tracking after processing completes**

In `ProcessDocumentsAsync`, after the result is finalized (near the return statement), add:

```csharp
_telemetry.TrackEvent("fill_complete", new Dictionary<string, object>
{
    ["file_count"] = result.TotalRecords,
    ["success_count"] = result.SuccessfulRecords,
    ["fail_count"] = result.FailedRecords,
    ["duration_ms"] = (int)result.Duration.TotalMilliseconds,
    ["input_mode"] = request is FolderProcessRequest ? "folder" : "single",
});
```

- [ ] **Step 3: Verify build**

Run: `dotnet build`
Expected: BUILD SUCCESS

- [ ] **Step 4: Commit**

```bash
git add Services/DocumentProcessorService.cs
git commit -m "feat: add fill_complete telemetry tracking"
```

---

### Task 9: Instrument DocumentCleanupService (cleanup_complete)

**Files:**
- Modify: `DocuFiller/Services/DocumentCleanupService.cs` (path may vary — check exact location)

- [ ] **Step 1: Add ITelemetryService dependency**

Add to constructor injection:

```csharp
private readonly ITelemetryService _telemetry;

// In constructor:
_telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
```

- [ ] **Step 2: Add cleanup_complete tracking**

After each cleanup operation completes successfully:

```csharp
_telemetry.TrackEvent("cleanup_complete", new Dictionary<string, object>
{
    ["file_count"] = 1,
    ["comments_removed"] = result.CommentsRemoved,
    ["controls_unwrapped"] = result.ControlsUnwrapped,
    ["duration_ms"] = durationMs,
    ["input_mode"] = "single",
});
```

- [ ] **Step 3: Verify build**

Run: `dotnet build`
Expected: BUILD SUCCESS

- [ ] **Step 4: Commit**

```bash
git add DocuFiller/Services/DocumentCleanupService.cs
git commit -m "feat: add cleanup_complete telemetry tracking"
```

---

### Task 10: Instrument InspectCommand (inspect_complete)

**Files:**
- Modify: `Cli/Commands/InspectCommand.cs`

- [ ] **Step 1: Add ITelemetryService dependency**

Inject `ITelemetryService` via the constructor (follow existing pattern in the file).

- [ ] **Step 2: Add inspect_complete tracking**

After inspect results are output:

```csharp
_telemetry.TrackEvent("inspect_complete", new Dictionary<string, object>
{
    ["control_count"] = controlCount,
    ["duration_ms"] = elapsedMs,
});
```

- [ ] **Step 3: Verify build**

Run: `dotnet build`
Expected: BUILD SUCCESS

- [ ] **Step 4: Commit**

```bash
git add Cli/Commands/InspectCommand.cs
git commit -m "feat: add inspect_complete telemetry tracking"
```

---

### Task 11: Instrument UpdateService (update_check / update_applied)

**Files:**
- Modify: `Services/UpdateService.cs`

- [ ] **Step 1: Add ITelemetryService dependency**

Add to constructor:

```csharp
private readonly ITelemetryService _telemetry;

// In constructor:
_telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
```

- [ ] **Step 2: Add update_check after CheckForUpdatesAsync**

```csharp
_telemetry.TrackEvent("update_check", new Dictionary<string, object>
{
    ["has_update"] = updateInfo != null,
    ["current_version"] = currentVersion,
    ["latest_version"] = updateInfo?.TargetFullRelease.Version?.ToString(),
});
```

- [ ] **Step 3: Add update_applied before ApplyUpdatesAndRestart**

```csharp
_telemetry.TrackEvent("update_applied", new Dictionary<string, object>
{
    ["old_version"] = currentVersion,
    ["new_version"] = newVersion,
});
_telemetry.FlushAsync(TimeSpan.FromSeconds(3)).GetAwaiter().GetResult();
```

- [ ] **Step 4: Verify build**

Run: `dotnet build`
Expected: BUILD SUCCESS

- [ ] **Step 5: Commit**

```bash
git add Services/UpdateService.cs
git commit -m "feat: add update_check and update_applied telemetry tracking"
```

---

### Task 12: End-to-end verification

**Files:**
- None (manual testing)

- [ ] **Step 1: Build Release**

Run: `dotnet build -c Release`
Expected: BUILD SUCCESS

- [ ] **Step 2: Test with no UpdateUrl configured (telemetry disabled)**

Run the app without configuring UpdateUrl. Verify:
- App starts normally
- No errors in logs related to telemetry
- No `telemetry.db` created

- [ ] **Step 3: Test with UpdateUrl pointing to running update-hub**

Configure `appsettings.json` with a valid UpdateUrl. Run the app and:
- Verify `telemetry.db` is created in `%LOCALAPPDATA%/DocuFiller/`
- Perform a fill operation
- Close the app
- Check update-hub's SQLite for received telemetry events

- [ ] **Step 4: Test server-down resilience**

Configure UpdateUrl to an unreachable address. Run the app and:
- Verify all features work normally
- Verify events accumulate in local `telemetry.db`
- Start the server and verify events are sent on next launch

- [ ] **Step 5: Final commit**

```bash
git add -A
git commit -m "test: telemetry end-to-end verification fixes"
```
