using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using DocuFiller.Cli;
using DocuFiller.Cli.Commands;
using DocuFiller.Services.Interfaces;
using DocuFiller.Models;
using DocuFiller.Utils;

namespace DocuFiller.Tests.Cli;

/// <summary>
/// 子命令参数验证测试（FillCommand、CleanupCommand、InspectCommand）
/// </summary>
public class CommandValidationTests
{
    private static async Task<(string output, int exitCode)> CaptureOutputWithExitCode(Func<Task<int>> action)
    {
        var sw = new StringWriter();
        var originalOut = Console.Out;
        try
        {
            Console.SetOut(sw);
            var exitCode = await action();
            return (sw.ToString(), exitCode);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    private static string CaptureOutput(Func<Task<int>> action)
    {
        var sw = new StringWriter();
        var originalOut = Console.Out;
        try
        {
            Console.SetOut(sw);
            action().GetAwaiter().GetResult();
            return sw.ToString();
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    private static JsonDocument ParseErrorLine(string output)
    {
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.True(lines.Length > 0, "Expected output but got none");
        var doc = JsonDocument.Parse(lines[0]);
        Assert.Equal("error", doc.RootElement.GetProperty("type").GetString());
        return doc;
    }

    // === Stub services (parameter validation happens before service calls) ===

    private class StubDocumentProcessor : IDocumentProcessor
    {
        public event EventHandler<ProgressEventArgs>? ProgressUpdated { add { } remove { } }
        public Task<ProcessResult> ProcessDocumentsAsync(ProcessRequest request) => throw new NotImplementedException();
        public Task<bool> ProcessSingleDocumentAsync(string templatePath, string outputPath, Dictionary<string, object> data, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<ValidationResult> ValidateTemplateAsync(string templatePath) => throw new NotImplementedException();
        public Task<List<ContentControlData>> GetContentControlsAsync(string templatePath) => throw new NotImplementedException();
        public Task<ProcessResult> ProcessDocumentWithFormattedDataAsync(string templateFilePath, Dictionary<string, FormattedCellValue> formattedData, string outputFilePath) => throw new NotImplementedException();
        public Task<ProcessResult> ProcessFolderAsync(FolderProcessRequest request, CancellationToken ct = default) => throw new NotImplementedException();
        public void CancelProcessing() { }
    }

    private class StubExcelDataParser : IExcelDataParser
    {
        public Task<Dictionary<string, FormattedCellValue>> ParseExcelFileAsync(string filePath, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<ExcelValidationResult> ValidateExcelFileAsync(string filePath) => throw new NotImplementedException();
        public Task<List<Dictionary<string, FormattedCellValue>>> GetDataPreviewAsync(string filePath, int maxRows = 10) => throw new NotImplementedException();
        public Task<ExcelFileSummary> GetDataStatisticsAsync(string filePath) => throw new NotImplementedException();
    }

    private class StubCleanupService : IDocumentCleanupService
    {
        public event EventHandler<CleanupProgressEventArgs>? ProgressChanged { add { } remove { } }
        public Task<CleanupResult> CleanupAsync(string filePath, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<CleanupResult> CleanupAsync(CleanupFileItem fileItem, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<CleanupResult> CleanupAsync(CleanupFileItem fileItem, string outputDirectory, CancellationToken ct = default) => throw new NotImplementedException();
    }

    private static FillCommand CreateFillCommand()
    {
        return new FillCommand(
            new StubDocumentProcessor(),
            new StubExcelDataParser(),
            NullLogger<FillCommand>.Instance);
    }

    private static CleanupCommand CreateCleanupCommand()
    {
        return new CleanupCommand(
            new StubCleanupService(),
            NullLogger<CleanupCommand>.Instance);
    }

    private static InspectCommand CreateInspectCommand()
    {
        return new InspectCommand(new StubDocumentProcessor());
    }

    // === FillCommand Tests ===

    [Fact]
    public void FillCommand_MissingTemplate_ReturnsMissingArgument()
    {
        var cmd = CreateFillCommand();
        var options = new Dictionary<string, string> { ["data"] = "data.xlsx", ["output"] = "./out" };

        var output = CaptureOutput(() => cmd.ExecuteAsync(options));
        var doc = ParseErrorLine(output);

        Assert.Equal("MISSING_ARGUMENT", doc.RootElement.GetProperty("data").GetProperty("code").GetString());
    }

    [Fact]
    public void FillCommand_MissingData_ReturnsMissingArgument()
    {
        var cmd = CreateFillCommand();
        var options = new Dictionary<string, string> { ["template"] = "template.docx", ["output"] = "./out" };

        var output = CaptureOutput(() => cmd.ExecuteAsync(options));
        var doc = ParseErrorLine(output);

        Assert.Equal("MISSING_ARGUMENT", doc.RootElement.GetProperty("data").GetProperty("code").GetString());
    }

    [Fact]
    public void FillCommand_MissingOutput_ReturnsMissingArgument()
    {
        var cmd = CreateFillCommand();
        var options = new Dictionary<string, string> { ["template"] = "template.docx", ["data"] = "data.xlsx" };

        var output = CaptureOutput(() => cmd.ExecuteAsync(options));
        var doc = ParseErrorLine(output);

        Assert.Equal("MISSING_ARGUMENT", doc.RootElement.GetProperty("data").GetProperty("code").GetString());
    }

    [Fact]
    public void FillCommand_TemplateFileNotFound_ReturnsFileNotFound()
    {
        var cmd = CreateFillCommand();
        var options = new Dictionary<string, string>
        {
            ["template"] = "nonexistent_template.docx",
            ["data"] = "data.xlsx",
            ["output"] = "./out"
        };

        var output = CaptureOutput(() => cmd.ExecuteAsync(options));
        var doc = ParseErrorLine(output);

        Assert.Equal("FILE_NOT_FOUND", doc.RootElement.GetProperty("data").GetProperty("code").GetString());
    }

    [Fact]
    public void FillCommand_DataFileNotFound_ReturnsFileNotFound()
    {
        // Create a temp file to use as the template (so template validation passes)
        var tempTemplate = Path.GetTempFileName();
        try
        {
            var cmd = CreateFillCommand();
            var options = new Dictionary<string, string>
            {
                ["template"] = tempTemplate,
                ["data"] = "nonexistent_data.xlsx",
                ["output"] = "./out"
            };

            var output = CaptureOutput(() => cmd.ExecuteAsync(options));
            var doc = ParseErrorLine(output);

            Assert.Equal("FILE_NOT_FOUND", doc.RootElement.GetProperty("data").GetProperty("code").GetString());
        }
        finally
        {
            File.Delete(tempTemplate);
        }
    }

    [Fact]
    public void FillCommand_EmptyTemplateValue_ReturnsMissingArgument()
    {
        var cmd = CreateFillCommand();
        var options = new Dictionary<string, string>
        {
            ["template"] = "",
            ["data"] = "data.xlsx",
            ["output"] = "./out"
        };

        var output = CaptureOutput(() => cmd.ExecuteAsync(options));
        var doc = ParseErrorLine(output);

        Assert.Equal("MISSING_ARGUMENT", doc.RootElement.GetProperty("data").GetProperty("code").GetString());
    }

    // === CleanupCommand Tests ===

    [Fact]
    public void CleanupCommand_MissingInput_ReturnsMissingArgument()
    {
        var cmd = CreateCleanupCommand();
        var options = new Dictionary<string, string>();

        var output = CaptureOutput(() => cmd.ExecuteAsync(options));
        var doc = ParseErrorLine(output);

        Assert.Equal("MISSING_ARGUMENT", doc.RootElement.GetProperty("data").GetProperty("code").GetString());
    }

    [Fact]
    public void CleanupCommand_InputFileNotFound_ReturnsFileNotFound()
    {
        var cmd = CreateCleanupCommand();
        var options = new Dictionary<string, string> { ["input"] = "nonexistent_file.docx" };

        var output = CaptureOutput(() => cmd.ExecuteAsync(options));
        var doc = ParseErrorLine(output);

        Assert.Equal("FILE_NOT_FOUND", doc.RootElement.GetProperty("data").GetProperty("code").GetString());
    }

    [Fact]
    public void CleanupCommand_EmptyInputValue_ReturnsMissingArgument()
    {
        var cmd = CreateCleanupCommand();
        var options = new Dictionary<string, string> { ["input"] = "" };

        var output = CaptureOutput(() => cmd.ExecuteAsync(options));
        var doc = ParseErrorLine(output);

        Assert.Equal("MISSING_ARGUMENT", doc.RootElement.GetProperty("data").GetProperty("code").GetString());
    }

    // === InspectCommand Tests ===

    [Fact]
    public void InspectCommand_MissingTemplate_ReturnsMissingArgument()
    {
        var cmd = CreateInspectCommand();
        var options = new Dictionary<string, string>();

        var output = CaptureOutput(() => cmd.ExecuteAsync(options));
        var doc = ParseErrorLine(output);

        Assert.Equal("MISSING_ARGUMENT", doc.RootElement.GetProperty("data").GetProperty("code").GetString());
    }

    [Fact]
    public void InspectCommand_TemplateFileNotFound_ReturnsFileNotFound()
    {
        var cmd = CreateInspectCommand();
        var options = new Dictionary<string, string> { ["template"] = "nonexistent.docx" };

        var output = CaptureOutput(() => cmd.ExecuteAsync(options));
        var doc = ParseErrorLine(output);

        Assert.Equal("FILE_NOT_FOUND", doc.RootElement.GetProperty("data").GetProperty("code").GetString());
    }

    [Fact]
    public void InspectCommand_EmptyTemplateValue_ReturnsMissingArgument()
    {
        var cmd = CreateInspectCommand();
        var options = new Dictionary<string, string> { ["template"] = "" };

        var output = CaptureOutput(() => cmd.ExecuteAsync(options));
        var doc = ParseErrorLine(output);

        Assert.Equal("MISSING_ARGUMENT", doc.RootElement.GetProperty("data").GetProperty("code").GetString());
    }

    // === Exit code tests ===

    [Fact]
    public async Task FillCommand_MissingArgs_ReturnsExitCode1()
    {
        var cmd = CreateFillCommand();
        var options = new Dictionary<string, string>();

        var originalOut = Console.Out;
        try
        {
            Console.SetOut(new StringWriter());
            var exitCode = await cmd.ExecuteAsync(options);
            Assert.Equal(1, exitCode);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public async Task CleanupCommand_MissingArgs_ReturnsExitCode1()
    {
        var cmd = CreateCleanupCommand();
        var options = new Dictionary<string, string>();

        var originalOut = Console.Out;
        try
        {
            Console.SetOut(new StringWriter());
            var exitCode = await cmd.ExecuteAsync(options);
            Assert.Equal(1, exitCode);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public async Task InspectCommand_MissingArgs_ReturnsExitCode1()
    {
        var cmd = CreateInspectCommand();
        var options = new Dictionary<string, string>();

        var originalOut = Console.Out;
        try
        {
            Console.SetOut(new StringWriter());
            var exitCode = await cmd.ExecuteAsync(options);
            Assert.Equal(1, exitCode);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }
}
