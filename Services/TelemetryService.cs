using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
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
    private const string SecretKey = "f9146af6fc0f7daa44d9f4151f84ae45d49c12c9b764185fc65c1e53ed15a49e";
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

        var updateUrl = configuration["Update:UpdateUrl"]?.TrimEnd('/');
        if (string.IsNullOrEmpty(updateUrl))
            throw new InvalidOperationException("UpdateUrl not configured, telemetry disabled");

        // UpdateUrl is like http://host:port/docufiller — extract base URL for API calls
        var uri = new Uri(updateUrl);
        _endpointUrl = $"{uri.Scheme}://{uri.Authority}/api/telemetry";

        _settings = new TelemetrySettings();
        configuration.GetSection(TelemetrySettings.SectionName).Bind(_settings);

        var appDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DocuFiller");
        _db = new TelemetryDb(appDataDir, logger);

        _signal.Release();

        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

        _sendLoop = Task.Run(() => SendLoopAsync(_cts.Token));
    }

    public void TrackEvent(string eventName, Dictionary<string, object>? properties = null)
    {
        if (_disposed) return;

        try
        {
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

            var payloadJson = JsonSerializer.Serialize(evt, JsonOpts);
            var signature = ComputeHmac(payloadJson);
            evt["signature"] = signature;

            var finalJson = JsonSerializer.Serialize(evt, JsonOpts);

            _db.Insert(finalJson);
            _db.TruncateOldEvents(MaxDbSizeBytes);

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
        try { await Task.Delay(5000, ct); } catch (OperationCanceledException) { return; }

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var delayTask = Task.Delay(TimeSpan.FromSeconds(_settings.FlushIntervalSeconds), ct);
                var signalTask = _signal.WaitAsync(ct);
                await Task.WhenAny(delayTask, signalTask);

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
                    var json = chunk.Length == 1
                        ? chunk[0].Payload
                        : "[" + string.Join(",", chunk.Select(c => c.Payload)) + "]";

                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var response = await _httpClient.PostAsync(_endpointUrl, content, ct);

                    if (response.IsSuccessStatusCode)
                    {
                        var body = await response.Content.ReadAsStringAsync(ct);
                        var doc = JsonDocument.Parse(body);
                        var accepted = doc.RootElement.GetProperty("accepted").GetInt32();
                        var rejected = doc.RootElement.GetProperty("rejected").GetInt32();

                        if (rejected == 0)
                        {
                            successIds.AddRange(chunk.Select(c => c.Id));
                        }
                        else
                        {
                            _logger.LogWarning("Telemetry server rejected {Count}/{Total} events: {Body}", rejected, chunk.Length, body);
                            // If all rejected, treat as rejected. If partial, delete accepted ones only.
                            if (accepted > 0 && chunk.Length > 1)
                            {
                                // Batch had partial success - can't tell which specific events were accepted
                                // so keep them all for retry (they'll be re-sent next cycle)
                            }
                            else
                            {
                                // All rejected or single event - delete to avoid retry loop
                                rejectedIds.AddRange(chunk.Select(c => c.Id));
                            }
                        }
                    }
                    else
                    {
                        var body = await response.Content.ReadAsStringAsync(ct);
                        _logger.LogDebug("Telemetry server returned {Status}: {Body}", (int)response.StatusCode, body);
                        return; // Retry on server errors
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Telemetry send failed, will retry");
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
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };
}
