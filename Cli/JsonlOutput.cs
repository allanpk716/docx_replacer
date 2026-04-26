using System.Text.Json;
using System.Text.Json.Serialization;

namespace DocuFiller.Cli;

/// <summary>
/// JSONL 格式化输出工具。每行输出一个 JSON 对象，统一 envelope 格式：
/// {"type":"...","status":"success|error","timestamp":"..."}
/// </summary>
internal static class JsonlOutput
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>
    /// 输出帮助信息 JSONL。
    /// </summary>
    public static void WriteHelp(object helpData)
    {
        WriteEnvelope("help", "success", helpData);
    }

    /// <summary>
    /// 输出结果 JSONL。
    /// </summary>
    public static void WriteResult(string type, object data)
    {
        WriteEnvelope(type, "success", data);
    }

    /// <summary>
    /// 输出错误 JSONL。
    /// </summary>
    public static void WriteError(string message, string? code = null)
    {
        var errorData = new Dictionary<string, object>
        {
            ["message"] = message,
        };
        if (code is not null)
        {
            errorData["code"] = code;
        }
        WriteEnvelope("error", "error", errorData);
    }

    /// <summary>
    /// 输出汇总 JSONL。
    /// </summary>
    public static void WriteSummary(object summary)
    {
        WriteEnvelope("summary", "success", summary);
    }

    /// <summary>
    /// 输出 update 类型 JSONL 行（版本检查结果、更新进度等）。
    /// </summary>
    public static void WriteUpdate(object data)
    {
        WriteEnvelope("update", "success", data);
    }

    private static void WriteEnvelope(string type, string status, object data)
    {
        var envelope = new JsonlEnvelope
        {
            Type = type,
            Status = status,
            Timestamp = DateTimeOffset.UtcNow.ToString("o"),
            Data = data,
        };
        string json = JsonSerializer.Serialize(envelope, JsonOptions);
        Console.WriteLine(json);
    }

    /// <summary>
    /// 直接输出原始 JSON 行（用于需要完全控制格式的场景）。
    /// </summary>
    public static void WriteRaw(string jsonLine)
    {
        Console.WriteLine(jsonLine);
    }

    private sealed class JsonlEnvelope
    {
        public string Type { get; set; } = "";
        public string Status { get; set; } = "";
        public string Timestamp { get; set; } = "";
        public object? Data { get; set; }
    }
}
