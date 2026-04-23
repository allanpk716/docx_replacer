using System.IO;
using System.Text.Json;
using Xunit;
using DocuFiller.Cli;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace DocuFiller.Tests.Cli;

/// <summary>
/// JsonlOutput 格式化输出测试
/// </summary>
public class JsonlOutputTests
{
    /// <summary>
    /// 捕获 Console.WriteLine 输出的辅助方法
    /// </summary>
    private static string CaptureOutput(Action action)
    {
        var sw = new StringWriter();
        var originalOut = Console.Out;
        try
        {
            Console.SetOut(sw);
            action();
            return sw.ToString();
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    /// <summary>
    /// 解析输出为 JsonDocument 并验证是合法 JSON
    /// </summary>
    private static JsonDocument ParseJsonLine(string line)
    {
        return JsonDocument.Parse(line.Trim());
    }

    [Fact]
    public void WriteResult_ContainsTypeStatusTimestampData()
    {
        // Act
        var output = CaptureOutput(() =>
        {
            JsonlOutput.WriteResult("control", new { tag = "test", title = "Test" });
        });

        // Assert
        var line = output.Trim();
        var doc = ParseJsonLine(line);

        Assert.True(doc.RootElement.TryGetProperty("type", out var typeEl));
        Assert.Equal("control", typeEl.GetString());

        Assert.True(doc.RootElement.TryGetProperty("status", out var statusEl));
        Assert.Equal("success", statusEl.GetString());

        Assert.True(doc.RootElement.TryGetProperty("timestamp", out var timestampEl));
        Assert.False(string.IsNullOrEmpty(timestampEl.GetString()));

        Assert.True(doc.RootElement.TryGetProperty("data", out var dataEl));
        Assert.Equal("test", dataEl.GetProperty("tag").GetString());
        Assert.Equal("Test", dataEl.GetProperty("title").GetString());
    }

    [Fact]
    public void WriteError_StatusIsError()
    {
        // Act
        var output = CaptureOutput(() =>
        {
            JsonlOutput.WriteError("something went wrong", "TEST_ERROR");
        });

        // Assert
        var doc = ParseJsonLine(output);

        Assert.Equal("error", doc.RootElement.GetProperty("type").GetString());
        Assert.Equal("error", doc.RootElement.GetProperty("status").GetString());

        var data = doc.RootElement.GetProperty("data");
        Assert.Equal("something went wrong", data.GetProperty("message").GetString());
        Assert.Equal("TEST_ERROR", data.GetProperty("code").GetString());
    }

    [Fact]
    public void WriteError_WithoutCode_CodeNotPresent()
    {
        // Act
        var output = CaptureOutput(() =>
        {
            JsonlOutput.WriteError("generic error");
        });

        // Assert
        var doc = ParseJsonLine(output);
        var data = doc.RootElement.GetProperty("data");

        Assert.True(data.TryGetProperty("message", out _));
        // code should not be present when null
        Assert.False(data.TryGetProperty("code", out _));
    }

    [Fact]
    public void WriteSummary_StatusIsSuccess()
    {
        // Act
        var output = CaptureOutput(() =>
        {
            JsonlOutput.WriteSummary(new { total = 10, success = 8, failed = 2 });
        });

        // Assert
        var doc = ParseJsonLine(output);

        Assert.Equal("summary", doc.RootElement.GetProperty("type").GetString());
        Assert.Equal("success", doc.RootElement.GetProperty("status").GetString());

        var data = doc.RootElement.GetProperty("data");
        Assert.Equal(10, data.GetProperty("total").GetInt32());
        Assert.Equal(8, data.GetProperty("success").GetInt32());
        Assert.Equal(2, data.GetProperty("failed").GetInt32());
    }

    [Fact]
    public void WriteResult_OutputIsSingleLine()
    {
        // Act
        var output = CaptureOutput(() =>
        {
            JsonlOutput.WriteResult("test", new { value = 1 });
        });

        // Assert: output should be exactly one line (no embedded newlines)
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Single(lines);
        // On Windows, Console.WriteLine appends \r\n; the JSON content itself has no \r
        Assert.DoesNotContain('\r', lines[0].TrimEnd('\r'));
    }

    [Fact]
    public void WriteError_OutputIsSingleLine()
    {
        var output = CaptureOutput(() =>
        {
            JsonlOutput.WriteError("error msg", "CODE");
        });

        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Single(lines);
    }

    [Fact]
    public void WriteSummary_OutputIsSingleLine()
    {
        var output = CaptureOutput(() =>
        {
            JsonlOutput.WriteSummary(new { count = 1 });
        });

        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Single(lines);
    }

    [Fact]
    public void WriteResult_Timestamp_IsISO8601()
    {
        var output = CaptureOutput(() =>
        {
            JsonlOutput.WriteResult("test", new { });
        });

        var doc = ParseJsonLine(output);
        var timestamp = doc.RootElement.GetProperty("timestamp").GetString();

        Assert.NotNull(timestamp);
        // ISO 8601 format check: should contain 'T' and timezone info
        Assert.Contains('T', timestamp);
        // DateTimeOffset.ToString("o") produces format like "2024-01-15T10:30:00.0000000+08:00"
        Assert.Matches(@"\d{4}-\d{2}-\d{2}T", timestamp);
    }

    [Fact]
    public void WriteRaw_OutputsExactLine()
    {
        var json = "{\"custom\":true}";
        var output = CaptureOutput(() =>
        {
            JsonlOutput.WriteRaw(json);
        });

        Assert.Equal("{\"custom\":true}" + Environment.NewLine, output);
    }
}
