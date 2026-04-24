using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocuFiller.Models;
using Xunit;

namespace DocuFiller.E2ERegression;

/// <summary>
/// T04: Two-column (FD68) format replacement tests.
/// Verifies FD68 data correctly replaces controls and produces different output than LD68.
/// </summary>
public class TwoColumnFormatTests : IDisposable
{
    private readonly string _outputDir;
    private readonly Services.Interfaces.IDocumentProcessor _processor;
    private readonly Dictionary<string, FormattedCellValue> _fd68Data;
    private readonly Dictionary<string, FormattedCellValue> _ld68Data;

    public TwoColumnFormatTests()
    {
        _outputDir = TestDataHelper.CreateTempOutputDir();
        _processor = ServiceFactory.GetProcessor();
        var parser = ServiceFactory.GetExcelParser();
        _fd68Data = parser.ParseExcelFileAsync(TestDataHelper.FD68ExcelPath).GetAwaiter().GetResult();
        _ld68Data = parser.ParseExcelFileAsync(TestDataHelper.LD68ExcelPath).GetAwaiter().GetResult();
    }

    private static string? GetControlValue(string docPath, string tag)
    {
        using var doc = WordprocessingDocument.Open(docPath, false);
        var bodySdt = doc.MainDocumentPart!.Document.Body!
            .Descendants<SdtElement>()
            .FirstOrDefault(s => s.SdtProperties?.GetFirstChild<Tag>()?.Val?.Value == tag);
        if (bodySdt != null)
            return string.Join("", bodySdt.Descendants<Text>().Select(t => t.Text));

        foreach (var hp in doc.MainDocumentPart.HeaderParts)
        {
            var h = hp.Header.Descendants<SdtElement>()
                .FirstOrDefault(s => s.SdtProperties?.GetFirstChild<Tag>()?.Val?.Value == tag);
            if (h != null) return string.Join("", h.Descendants<Text>().Select(t => t.Text));
        }
        return null;
    }

    [Fact]
    public async Task FD68_TwoColumn_CE0601_Succeeds()
    {
        var templatePath = TestDataHelper.GetCE0601Template();
        var outputPath = Path.Combine(_outputDir, "FD68_CE0601.docx");

        var result = await _processor.ProcessDocumentWithFormattedDataAsync(
            templatePath, _fd68Data, outputPath);

        Assert.True(result.IsSuccess, $"FD68 CE06-01 failed: {result.Message}");
    }

    [Fact]
    public async Task FD68_TwoColumn_DifferentValuesFromLD68()
    {
        var templatePath = TestDataHelper.GetCE01Template();
        var fd68Output = Path.Combine(_outputDir, "diff_fd68.docx");
        var ld68Output = Path.Combine(_outputDir, "diff_ld68.docx");

        await _processor.ProcessDocumentWithFormattedDataAsync(
            templatePath, _fd68Data, fd68Output);
        await _processor.ProcessDocumentWithFormattedDataAsync(
            templatePath, _ld68Data, ld68Output);

        // Common keywords should have different values
        var fd68Name = GetControlValue(fd68Output, "#产品名称#");
        var ld68Name = GetControlValue(ld68Output, "#产品名称#");

        Assert.NotNull(fd68Name);
        Assert.NotNull(ld68Name);
        Assert.Contains("Fluorescent Dye", fd68Name);
        Assert.Contains("Lyse", ld68Name);
        Assert.NotEqual(fd68Name, ld68Name);

        // Different UDI-DI codes
        var fd68Udi = GetControlValue(fd68Output, "#Basic UDI-DI#");
        var ld68Udi = GetControlValue(ld68Output, "#Basic UDI-DI#");
        Assert.NotEqual(fd68Udi, ld68Udi);
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_outputDir)) Directory.Delete(_outputDir, true); } catch { }
    }
}
