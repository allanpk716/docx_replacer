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
/// Replacement correctness tests for LD68 (three-column) and FD68 (two-column) formats.
/// Key insight from diagnostic: CE01 contains ~67 SDTs with tags like #产品名称#, #产品型号#,
/// #Basic UDI-DI#, #预期用途#, etc. Values are replaced in both body and headers/footers.
/// </summary>
public class ReplacementCorrectnessTests : IDisposable
{
    private readonly string _outputDir;
    private readonly Services.Interfaces.IDocumentProcessor _processor;
    private readonly Services.Interfaces.IExcelDataParser _excelParser;
    private readonly Dictionary<string, FormattedCellValue> _ld68Data;
    private readonly Dictionary<string, FormattedCellValue> _fd68Data;

    public ReplacementCorrectnessTests()
    {
        _outputDir = TestDataHelper.CreateTempOutputDir();
        _processor = ServiceFactory.GetProcessor();
        _excelParser = ServiceFactory.GetExcelParser();
        _ld68Data = _excelParser.ParseExcelFileAsync(TestDataHelper.LD68ExcelPath).GetAwaiter().GetResult();
        _fd68Data = _excelParser.ParseExcelFileAsync(TestDataHelper.FD68ExcelPath).GetAwaiter().GetResult();
    }

    /// <summary>Find the replaced text for a given content control tag in the output document</summary>
    private static string? GetControlValue(string docPath, string tag)
    {
        using var doc = WordprocessingDocument.Open(docPath, false);

        // Search in body
        var bodySdt = doc.MainDocumentPart!.Document.Body!
            .Descendants<SdtElement>()
            .FirstOrDefault(s => s.SdtProperties?.GetFirstChild<Tag>()?.Val?.Value == tag);
        if (bodySdt != null)
            return string.Join("", bodySdt.Descendants<Text>().Select(t => t.Text));

        // Search in headers
        foreach (var hp in doc.MainDocumentPart.HeaderParts)
        {
            var headerSdt = hp.Header
                .Descendants<SdtElement>()
                .FirstOrDefault(s => s.SdtProperties?.GetFirstChild<Tag>()?.Val?.Value == tag);
            if (headerSdt != null)
                return string.Join("", headerSdt.Descendants<Text>().Select(t => t.Text));
        }

        // Search in footers
        foreach (var fp in doc.MainDocumentPart.FooterParts)
        {
            var footerSdt = fp.Footer
                .Descendants<SdtElement>()
                .FirstOrDefault(s => s.SdtProperties?.GetFirstChild<Tag>()?.Val?.Value == tag);
            if (footerSdt != null)
                return string.Join("", footerSdt.Descendants<Text>().Select(t => t.Text));
        }

        return null;
    }

    private static void AssertControlValue(string docPath, string tag, string expectedValue)
    {
        var actual = GetControlValue(docPath, tag);
        Assert.True(actual != null, $"Content control '{tag}' not found in document");
        Assert.Contains(expectedValue, actual);
    }

    // === LD68 Three-column format ===

    [Fact]
    public async Task LD68_ThreeColumn_CE01_Replacement_Succeeds()
    {
        var templatePath = TestDataHelper.GetCE01Template();
        var outputPath = Path.Combine(_outputDir, "LD68_CE01.docx");

        var result = await _processor.ProcessDocumentWithFormattedDataAsync(
            templatePath, _ld68Data, outputPath);

        Assert.True(result.IsSuccess, $"CE01 processing failed: {result.Message}");
        Assert.True(File.Exists(outputPath), "Output file not created");
    }

    [Fact]
    public async Task LD68_ThreeColumn_CE01_ValuesMatchExcel()
    {
        var templatePath = TestDataHelper.GetCE01Template();
        var outputPath = Path.Combine(_outputDir, "LD68_CE01_vals.docx");

        await _processor.ProcessDocumentWithFormattedDataAsync(
            templatePath, _ld68Data, outputPath);

        // Verify specific control values match LD68 Excel data
        AssertControlValue(outputPath, "#产品名称#", "Lyse");
        AssertControlValue(outputPath, "#产品型号#", "BH-LD68");
        AssertControlValue(outputPath, "#Basic UDI-DI#", "69357407IBHS000018EF");
        AssertControlValue(outputPath, "#风险等级#", "Class A");
    }

    [Fact]
    public async Task LD68_ThreeColumn_CE00_Succeeds()
    {
        var templatePath = TestDataHelper.GetCE00Template();
        var outputPath = Path.Combine(_outputDir, "LD68_CE00.docx");

        var result = await _processor.ProcessDocumentWithFormattedDataAsync(
            templatePath, _ld68Data, outputPath);
        Assert.True(result.IsSuccess, $"CE00 failed: {result.Message}");
    }

    [Fact]
    public async Task LD68_ThreeColumn_CE0601_Succeeds()
    {
        var templatePath = TestDataHelper.GetCE0601Template();
        var outputPath = Path.Combine(_outputDir, "LD68_CE0601.docx");

        var result = await _processor.ProcessDocumentWithFormattedDataAsync(
            templatePath, _ld68Data, outputPath);
        Assert.True(result.IsSuccess, $"CE06-01 failed: {result.Message}");
    }

    // === FD68 Two-column format ===

    [Fact]
    public async Task FD68_TwoColumn_CE01_Replacement_Succeeds()
    {
        var templatePath = TestDataHelper.GetCE01Template();
        var outputPath = Path.Combine(_outputDir, "FD68_CE01.docx");

        var result = await _processor.ProcessDocumentWithFormattedDataAsync(
            templatePath, _fd68Data, outputPath);
        Assert.True(result.IsSuccess, $"FD68 CE01 failed: {result.Message}");
    }

    [Fact]
    public async Task FD68_TwoColumn_CE01_ValuesMatchExcel()
    {
        var templatePath = TestDataHelper.GetCE01Template();
        var outputPath = Path.Combine(_outputDir, "FD68_CE01_vals.docx");

        await _processor.ProcessDocumentWithFormattedDataAsync(
            templatePath, _fd68Data, outputPath);

        // Verify specific control values match FD68 Excel data (different from LD68)
        AssertControlValue(outputPath, "#产品名称#", "Fluorescent Dye");
        AssertControlValue(outputPath, "#产品型号#", "BH-FD68");
        AssertControlValue(outputPath, "#Basic UDI-DI#", "69357407IBHS000017ED");
    }

    // === Cross-format comparison ===

    [Fact]
    public async Task SameTemplate_DifferentDataSources_ProduceDifferentOutput()
    {
        var templatePath = TestDataHelper.GetCE01Template();
        var ld68Output = Path.Combine(_outputDir, "cross_ld68.docx");
        var fd68Output = Path.Combine(_outputDir, "cross_fd68.docx");

        await _processor.ProcessDocumentWithFormattedDataAsync(
            templatePath, _ld68Data, ld68Output);
        await _processor.ProcessDocumentWithFormattedDataAsync(
            templatePath, _fd68Data, fd68Output);

        // Same #产品型号# control, different values
        var ld68Model = GetControlValue(ld68Output, "#产品型号#");
        var fd68Model = GetControlValue(fd68Output, "#产品型号#");

        Assert.NotNull(ld68Model);
        Assert.NotNull(fd68Model);
        Assert.Contains("BH-LD68", ld68Model);
        Assert.Contains("BH-FD68", fd68Model);
        Assert.NotEqual(ld68Model, fd68Model);
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_outputDir)) Directory.Delete(_outputDir, true); } catch { }
    }
}
