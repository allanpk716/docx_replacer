using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace DocuFiller.E2ERegression;

/// <summary>
/// Smoke tests for infrastructure: ServiceFactory, TestDataHelper, Excel parsing
/// </summary>
public class InfrastructureTests
{
    [Fact]
    public void ServiceFactory_CreatesProcessor_Successfully()
    {
        var processor = ServiceFactory.CreateProcessor();
        Assert.NotNull(processor);
    }

    [Fact]
    public void ServiceFactory_GetProcessor_ReturnsIDocumentProcessor()
    {
        var processor = ServiceFactory.GetProcessor();
        Assert.NotNull(processor);
        Assert.IsAssignableFrom<Services.Interfaces.IDocumentProcessor>(processor);
    }

    [Fact]
    public void TestDataHelper_FindsTestDataDirectory()
    {
        Assert.True(System.IO.Directory.Exists(TestDataHelper.TestDataDirectory),
            $"TestDataDirectory not found: {TestDataHelper.TestDataDirectory}");
    }

    [Fact]
    public void TestDataHelper_FindsExcelFiles()
    {
        Assert.True(System.IO.File.Exists(TestDataHelper.LD68ExcelPath),
            $"LD68 Excel not found: {TestDataHelper.LD68ExcelPath}");
        Assert.True(System.IO.File.Exists(TestDataHelper.FD68ExcelPath),
            $"FD68 Excel not found: {TestDataHelper.FD68ExcelPath}");
    }

    [Fact]
    public void TestDataHelper_FindsTemplates()
    {
        var templates = TestDataHelper.GetAllTemplates();
        Assert.True(templates.Count >= 30,
            $"Expected at least 30 templates, found {templates.Count}");
    }

    [Fact]
    public async Task ExcelParsing_LD68_ThreeColumnFormat()
    {
        var parser = ServiceFactory.GetExcelParser();
        var data = await parser.ParseExcelFileAsync(TestDataHelper.LD68ExcelPath);

        // LD68 is three-column format: should have ~73 keywords (ID column excluded)
        Assert.True(data.Count >= 50,
            $"LD68: expected at least 50 keywords, got {data.Count}");

        // First keyword should be #产品名称#
        Assert.True(data.ContainsKey("#产品名称#"),
            "LD68: missing #产品名称# keyword");

        // Value should be "Lyse"
        Assert.Equal("Lyse", data["#产品名称#"].PlainText);
    }

    [Fact]
    public async Task ExcelParsing_FD68_TwoColumnFormat()
    {
        var parser = ServiceFactory.GetExcelParser();
        var data = await parser.ParseExcelFileAsync(TestDataHelper.FD68ExcelPath);

        // FD68 is two-column format: should have ~58 keywords
        Assert.True(data.Count >= 40,
            $"FD68: expected at least 40 keywords, got {data.Count}");

        // First keyword should be #产品名称#
        Assert.True(data.ContainsKey("#产品名称#"),
            "FD68: missing #产品名称# keyword");

        // Value should be "Fluorescent Dye" (different from LD68)
        Assert.Equal("Fluorescent Dye", data["#产品名称#"].PlainText);
    }

    [Fact]
    public async Task ExcelParsing_BothFormats_HaveCommonKeywords()
    {
        var parser = ServiceFactory.GetExcelParser();
        var ld68Data = await parser.ParseExcelFileAsync(TestDataHelper.LD68ExcelPath);
        var fd68Data = await parser.ParseExcelFileAsync(TestDataHelper.FD68ExcelPath);

        // Common keywords exist in both
        var commonKeywords = ld68Data.Keys.Intersect(fd68Data.Keys).ToList();
        Assert.True(commonKeywords.Count >= 30,
            $"Expected at least 30 common keywords, found {commonKeywords.Count}");

        // But values differ for same keywords
        Assert.NotEqual(ld68Data["#产品名称#"].PlainText, fd68Data["#产品名称#"].PlainText);
        Assert.NotEqual(ld68Data["#产品型号#"].PlainText, fd68Data["#产品型号#"].PlainText);
    }
}
