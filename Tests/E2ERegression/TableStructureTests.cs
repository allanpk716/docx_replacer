using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocuFiller.Models;
using Xunit;

namespace DocuFiller.E2ERegression;

/// <summary>
/// T05: Verify table structure is preserved after replacement.
/// Counts TableRow and TableCell elements before/after processing.
/// </summary>
public class TableStructureTests : IDisposable
{
    private readonly string _outputDir;
    private readonly Services.Interfaces.IDocumentProcessor _processor;
    private readonly Dictionary<string, FormattedCellValue> _ld68Data;

    public TableStructureTests()
    {
        _outputDir = TestDataHelper.CreateTempOutputDir();
        _processor = ServiceFactory.GetProcessor();
        _ld68Data = ServiceFactory.GetExcelParser()
            .ParseExcelFileAsync(TestDataHelper.LD68ExcelPath).GetAwaiter().GetResult();
    }

    [Fact]
    public async Task LD68_CE01_TableStructure_Preserved()
    {
        var templatePath = TestDataHelper.GetCE01Template();
        var outputPath = Path.Combine(_outputDir, "table_ce01.docx");

        // Count before
        var (beforeRows, beforeCells) = CountTableElements(templatePath);

        // Process
        await _processor.ProcessDocumentWithFormattedDataAsync(
            templatePath, _ld68Data, outputPath);

        // Count after
        var (afterRows, afterCells) = CountTableElements(outputPath);

        Assert.Equal(beforeRows, afterRows);
        Assert.Equal(beforeCells, afterCells);
    }

    [Fact]
    public async Task LD68_CE0601_TableStructure_Preserved()
    {
        var templatePath = TestDataHelper.GetCE0601Template();
        var outputPath = Path.Combine(_outputDir, "table_ce0601.docx");

        var (beforeRows, beforeCells) = CountTableElements(templatePath);

        await _processor.ProcessDocumentWithFormattedDataAsync(
            templatePath, _ld68Data, outputPath);

        var (afterRows, afterCells) = CountTableElements(outputPath);

        Assert.Equal(beforeRows, afterRows);
        Assert.Equal(beforeCells, afterCells);
    }

    private static (int rows, int cells) CountTableElements(string docPath)
    {
        using var doc = WordprocessingDocument.Open(docPath, false);
        var body = doc.MainDocumentPart!.Document.Body!;

        var rows = body.Descendants<TableRow>().Count();
        var cells = body.Descendants<TableCell>().Count();

        return (rows, cells);
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_outputDir)) Directory.Delete(_outputDir, true); } catch { }
    }
}
