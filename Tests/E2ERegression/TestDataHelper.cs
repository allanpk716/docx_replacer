using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DocuFiller.E2ERegression;

/// <summary>
/// Discovers test data files by navigating up from the assembly location
/// to find the test_data/2026年4月23日/ directory.
/// </summary>
public static class TestDataHelper
{
    private static readonly string _testDataRoot = FindTestDataRoot();

    /// <summary>
    /// test_data/2026年4月23日/ directory path
    /// </summary>
    public static string TestDataDirectory => Path.Combine(_testDataRoot, "2026年4月23日");

    /// <summary>
    /// LD68 IVDR.xlsx path (three-column format: ID|keyword|value, 74 keywords, 3 superscript cells)
    /// </summary>
    public static string LD68ExcelPath => Path.Combine(TestDataDirectory, "LD68 IVDR.xlsx");

    /// <summary>
    /// FD68 IVDR.xlsx path (two-column format: keyword|value, 59 keywords, plain text only)
    /// </summary>
    public static string FD68ExcelPath => Path.Combine(TestDataDirectory, "FD68 IVDR.xlsx");

    /// <summary>
    /// Template directory containing 43 docx templates organized by Chapter
    /// </summary>
    public static string TemplateDirectory => FindTemplateDirectory();

    /// <summary>
    /// Recursively list all .docx files in the template directory
    /// </summary>
    public static List<string> GetAllTemplates()
    {
        var dir = TemplateDirectory;
        if (!Directory.Exists(dir))
            return new List<string>();

        return Directory.GetFiles(dir, "*.docx", SearchOption.AllDirectories)
            .OrderBy(f => f)
            .ToList();
    }

    /// <summary>
    /// Get templates filtered by Chapter prefix (e.g., "Chapter 1")
    /// </summary>
    public static List<string> GetTemplatesByChapter(string chapterPrefix)
    {
        return GetAllTemplates()
            .Where(f => Path.GetDirectoryName(f)?.Contains(chapterPrefix) == true)
            .ToList();
    }

    /// <summary>
    /// Get a specific template by its filename
    /// </summary>
    public static string GetTemplateByFileName(string fileName)
    {
        return GetAllTemplates()
            .FirstOrDefault(f => Path.GetFileName(f) == fileName)
            ?? throw new FileNotFoundException($"Template not found: {fileName}");
    }

    /// <summary>
    /// Get path to CE01 template (Chapter 1, 82 content controls, has table+header+footer)
    /// </summary>
    public static string GetCE01Template()
    {
        return GetAllTemplates()
            .FirstOrDefault(f => f.Contains("CE01") && f.Contains("Device Description"))
            ?? throw new FileNotFoundException("CE01 template not found");
    }

    /// <summary>
    /// Get path to CE06-01 template (Chapter 6, 49 content controls, has table+header+footer)
    /// </summary>
    public static string GetCE0601Template()
    {
        return GetAllTemplates()
            .FirstOrDefault(f => f.Contains("CE06-01") && f.Contains("Performance Evaluation Plan"))
            ?? throw new FileNotFoundException("CE06-01 template not found");
    }

    /// <summary>
    /// Get path to CE00 template (Chapter 0, 35 content controls, has table+header+footer)
    /// </summary>
    public static string GetCE00Template()
    {
        return GetAllTemplates()
            .FirstOrDefault(f => f.Contains("CE00") && f.Contains("Overview"))
            ?? throw new FileNotFoundException("CE00 template not found");
    }

    /// <summary>
    /// Create a temp output directory for test output files
    /// </summary>
    public static string CreateTempOutputDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"E2E_Output_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static string FindTestDataRoot()
    {
        // Start from the assembly location and navigate up
        var dir = AppContext.BaseDirectory;

        for (int i = 0; i < 20; i++)
        {
            var candidate = Path.Combine(dir, "test_data");
            if (Directory.Exists(candidate))
                return candidate;

            var parent = Directory.GetParent(dir)?.FullName;
            if (parent == null || parent == dir)
                break;
            dir = parent;
        }

        throw new DirectoryNotFoundException(
            "Cannot find test_data/ directory. " +
            "Ensure test_data/2026年4月23日/ exists with LD68 IVDR.xlsx, FD68 IVDR.xlsx, and template files.");
    }

    private static string FindTemplateDirectory()
    {
        var testDir = TestDataDirectory;
        if (!Directory.Exists(testDir))
            throw new DirectoryNotFoundException($"Test data directory not found: {testDir}");

        // Find the subdirectory containing the docx templates (starts with 血细胞分析用染色液)
        var dirs = Directory.GetDirectories(testDir);
        var templateDir = dirs.FirstOrDefault(d => Directory.Exists(d) &&
            Directory.GetFiles(d, "*.docx", SearchOption.AllDirectories).Length > 0);

        return templateDir ?? throw new DirectoryNotFoundException(
            "Cannot find template subdirectory under " + testDir);
    }
}
