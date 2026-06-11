using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace DocuFiller.Tests;

/// <summary>
/// Tests that the C# HMAC computation produces output compatible with the Go server.
/// </summary>
public class TelemetryHmacTests
{
    private const string SecretKey = "f9146af6fc0f7daa44d9f4151f84ae45d49c12c9b764185fc65c1e53ed15a49e";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    [Fact]
    public void PayloadJson_MatchesGoJsonMarshal()
    {
        // Simulate exactly what TelemetryService.TrackEvent does
        var evt = new SortedDictionary<string, object>
        {
            ["app_id"] = "docufiller",
            ["event"] = "app_start",
            ["machine"] = "TEST-PC",
            ["session_id"] = "test-session-001",
            ["timestamp"] = "2026-06-11T18:44:12.3456789+08:00",
            ["user"] = "testuser",
            ["version"] = "1.0.0",
        };

        var payloadJson = JsonSerializer.Serialize(evt, JsonOpts);
        var signature = ComputeHmac(payloadJson);

        // Output for debugging - compare with Go's output
        Console.WriteLine($"C# PAYLOAD JSON: [{payloadJson}]");
        Console.WriteLine($"C# PAYLOAD LENGTH: {payloadJson.Length}");
        Console.WriteLine($"C# PAYLOAD BYTES: {BitConverter.ToString(Encoding.UTF8.GetBytes(payloadJson))}");
        Console.WriteLine($"C# SIGNATURE: {signature}");

        // The Go server would produce this JSON from buildPayload:
        // {"app_id":"docufiller","event":"app_start","machine":"TEST-PC","session_id":"test-session-001","timestamp":"2026-06-11T18:44:12.3456789+08:00","user":"testuser","version":"1.0.0"}
        var expectedGoJson = "{\"app_id\":\"docufiller\",\"event\":\"app_start\",\"machine\":\"TEST-PC\",\"session_id\":\"test-session-001\",\"timestamp\":\"2026-06-11T18:44:12.3456789+08:00\",\"user\":\"testuser\",\"version\":\"1.0.0\"}";

        Console.WriteLine($"GO EXPECTED:       [{expectedGoJson}]");
        Console.WriteLine($"MATCH: {payloadJson == expectedGoJson}");

        // Assert they match
        Assert.Equal(expectedGoJson, payloadJson);
    }

    [Fact]
    public void Hmac_MatchesGoHmac()
    {
        var evt = new SortedDictionary<string, object>
        {
            ["app_id"] = "docufiller",
            ["event"] = "app_start",
            ["machine"] = "TEST-PC",
            ["session_id"] = "test-session-001",
            ["timestamp"] = "2026-06-11T18:44:12.3456789+08:00",
            ["user"] = "testuser",
            ["version"] = "1.0.0",
        };

        var payloadJson = JsonSerializer.Serialize(evt, JsonOpts);
        var csSignature = ComputeHmac(payloadJson);

        // This is the HMAC that Go produces for the same payload
        var goExpectedSignature = "ec854ee230adb044922a3d88560321340a016aa944d2375cd19152e6d363a5d5";

        Console.WriteLine($"C# HMAC: {csSignature}");
        Console.WriteLine($"GO HMAC: {goExpectedSignature}");
        Console.WriteLine($"MATCH:   {csSignature == goExpectedSignature}");

        Assert.Equal(goExpectedSignature, csSignature);
    }

    [Fact]
    public void FullRoundTrip_ClientToServer()
    {
        // Simulate the full client flow
        var evt = new SortedDictionary<string, object>
        {
            ["app_id"] = "docufiller",
            ["event"] = "app_start",
            ["machine"] = "TEST-PC",
            ["session_id"] = "test-session-001",
            ["timestamp"] = "2026-06-11T18:44:12.3456789+08:00",
            ["user"] = "testuser",
            ["version"] = "1.0.0",
        };

        var payloadJson = JsonSerializer.Serialize(evt, JsonOpts);
        var signature = ComputeHmac(payloadJson);
        evt["signature"] = signature;

        var finalJson = JsonSerializer.Serialize(evt, JsonOpts);
        Console.WriteLine($"FINAL JSON: {finalJson}");

        // Now simulate server: deserialize, rebuild payload, verify
        using var doc = JsonDocument.Parse(finalJson);
        var root = doc.RootElement;

        // Extract fields (simulating Go's Event struct deserialization)
        var app_id = root.GetProperty("app_id").GetString()!;
        var @event = root.GetProperty("event").GetString()!;
        var timestamp = root.GetProperty("timestamp").GetString()!;
        var session_id = root.GetProperty("session_id").GetString()!;
        var user = root.GetProperty("user").GetString()!;
        var machine = root.GetProperty("machine").GetString()!;
        var version = root.GetProperty("version").GetString()!;
        var receivedSig = root.GetProperty("signature").GetString()!;

        // Rebuild payload (simulating Go's buildPayload)
        var serverPayload = new SortedDictionary<string, object>
        {
            ["app_id"] = app_id,
            ["event"] = @event,
            ["timestamp"] = timestamp,
            ["session_id"] = session_id,
            ["user"] = user,
            ["machine"] = machine,
            ["version"] = version,
        };

        var serverPayloadJson = JsonSerializer.Serialize(serverPayload, JsonOpts);
        var serverSig = ComputeHmac(serverPayloadJson);

        Console.WriteLine($"CLIENT PAYLOAD: {payloadJson}");
        Console.WriteLine($"SERVER PAYLOAD: {serverPayloadJson}");
        Console.WriteLine($"CLIENT SIG:     {receivedSig}");
        Console.WriteLine($"SERVER SIG:     {serverSig}");
        Console.WriteLine($"PAYLOAD MATCH:  {payloadJson == serverPayloadJson}");
        Console.WriteLine($"SIG MATCH:      {receivedSig == serverSig}");

        Assert.Equal(payloadJson, serverPayloadJson);
        Assert.Equal(receivedSig, serverSig);
    }

    private static string ComputeHmac(string payloadJson)
    {
        var keyBytes = Encoding.UTF8.GetBytes(SecretKey);
        var payloadBytes = Encoding.UTF8.GetBytes(payloadJson);
        using var hmac = new HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(payloadBytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
