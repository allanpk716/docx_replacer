# 页眉页脚批注支持实施计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**目标：** 扩展 CommentManager 以支持页眉页脚中的内容控件批注，确保批注正确显示在页眉页脚位置。

**架构：** 修改批注管理器使其能够识别内容控件位置（正文/页眉/页脚），并根据位置将批注存储到对应的文档部分（MainDocumentPart、HeaderPart 或 FooterPart）。实现全局批注 ID 管理确保所有位置的批注 ID 唯一。

**技术栈：** .NET 8, DocumentFormat.OpenXml SDK, Microsoft.Extensions.Logging, xUnit

---

## Task 1: 为 CommentManager 添加位置参数支持

**Files:**
- Modify: `Services/CommentManager.cs:25-60`

**Step 1: 修改 AddCommentToElement 方法签名**

```csharp
public void AddCommentToElement(
    WordprocessingDocument document,
    Run targetRun,
    string commentText,
    string author,
    string tag,
    ContentControlLocation location = ContentControlLocation.Body)
```

**Step 2: 修改方法内部调用，传递 location 参数**

将 `GetOrCreateCommentsPart(document.MainDocumentPart)` 替换为：
```csharp
WordprocessingCommentsPart? commentsPart = GetCommentsPartForLocation(document, location);
```

**Step 3: 暂时编译查看错误**

Run: `dotnet build`
Expected: 编译失败，提示 `GetCommentsPartForLocation` 方法不存在

**Step 4: 提交**

```bash
git add Services/CommentManager.cs
git commit -m "refactor(comment): AddCommentToElement 添加 location 参数"
```

---

## Task 2: 为 AddCommentToRunRange 添加位置参数

**Files:**
- Modify: `Services/CommentManager.cs:65-106`

**Step 1: 修改 AddCommentToRunRange 方法签名**

```csharp
public void AddCommentToRunRange(
    WordprocessingDocument document,
    System.Collections.Generic.List<Run> targetRuns,
    string commentText,
    string author,
    string tag,
    ContentControlLocation location = ContentControlLocation.Body)
```

**Step 2: 修改方法内部调用，传递 location 参数**

将 `GetOrCreateCommentsPart(document.MainDocumentPart)` 替换为：
```csharp
WordprocessingCommentsPart? commentsPart = GetCommentsPartForLocation(document, location);
```

**Step 3: 编译验证**

Run: `dotnet build`
Expected: 编译失败，提示 `GetCommentsPartForLocation` 方法不存在

**Step 4: 提交**

```bash
git add Services/CommentManager.cs
git commit -m "refactor(comment): AddCommentToRunRange 添加 location 参数"
```

---

## Task 3: 实现位置感知的批注部分获取方法

**Files:**
- Modify: `Services/CommentManager.cs:108-123`

**Step 1: 删除原有的 GetOrCreateCommentsPart 方法**

删除第 111-123 行的 `GetOrCreateCommentsPart` 方法。

**Step 2: 添加新的位置感知方法**

```csharp
/// <summary>
/// 根据位置获取或创建批注部分
/// </summary>
private WordprocessingCommentsPart GetCommentsPartForLocation(
    WordprocessingDocument document,
    ContentControlLocation location)
{
    return location switch
    {
        ContentControlLocation.Body => GetOrCreateMainCommentsPart(document.MainDocumentPart),
        ContentControlLocation.Header => GetOrCreateMainCommentsPart(document.MainDocumentPart),
        ContentControlLocation.Footer => GetOrCreateMainCommentsPart(document.MainDocumentPart),
        _ => throw new ArgumentException($"不支持的位置: {location}")
    };
}

/// <summary>
/// 获取或创建主文档的批注部分
/// </summary>
private WordprocessingCommentsPart GetOrCreateMainCommentsPart(MainDocumentPart mainDocumentPart)
{
    WordprocessingCommentsPart? commentsPart = mainDocumentPart.WordprocessingCommentsPart;

    if (commentsPart == null)
    {
        _logger.LogDebug("创建新的批注部分");
        commentsPart = mainDocumentPart.AddNewPart<WordprocessingCommentsPart>();
        commentsPart.Comments = new Comments();
    }

    return commentsPart;
}
```

**Step 3: 编译验证**

Run: `dotnet build`
Expected: 编译成功

**Step 4: 提交**

```bash
git add Services/CommentManager.cs
git commit -m "refactor(comment): 实现位置感知的批注部分获取方法"
```

---

## Task 4: ContentControlProcessor 传递位置参数

**Files:**
- Modify: `Services/ContentControlProcessor.cs:290-321`

**Step 1: 修改 AddProcessingComment 方法中的批注添加调用**

将第 314 行：
```csharp
_commentManager.AddCommentToElement(document, targetRuns[0], commentText, "DocuFiller系统", tag);
```

改为：
```csharp
_commentManager.AddCommentToElement(document, targetRuns[0], commentText, "DocuFiller系统", tag, location);
```

将第 319 行：
```csharp
_commentManager.AddCommentToRunRange(document, targetRuns, commentText, "DocuFiller系统", tag);
```

改为：
```csharp
_commentManager.AddCommentToRunRange(document, targetRuns, commentText, "DocuFiller系统", tag, location);
```

**Step 2: 编译验证**

Run: `dotnet build`
Expected: 编译成功

**Step 3: 运行现有测试确保没有破坏功能**

Run: `dotnet test`
Expected: 所有现有测试通过

**Step 4: 提交**

```bash
git add Services/ContentControlProcessor.cs
git commit -m "refactor(comment): ContentControlProcessor 传递 location 参数"
```

---

## Task 5: 实现全局批注 ID 管理

**Files:**
- Modify: `Services/CommentManager.cs:125-144`

**Step 1: 修改 GenerateCommentId 方法为全局 ID**

```csharp
/// <summary>
/// 生成全局唯一的批注ID
/// </summary>
private string GenerateCommentId(WordprocessingDocument document)
{
    int maxId = 0;

    // 检查主文档的批注
    if (document.MainDocumentPart?.WordprocessingCommentsPart?.Comments != null)
    {
        maxId = Math.Max(maxId, document.MainDocumentPart.WordprocessingCommentsPart.Comments.Descendants<Comment>()
            .Select(c => int.TryParse(c.Id?.Value, out int commentId) ? commentId : 0)
            .DefaultIfEmpty(0)
            .Max());
    }

    // 检查所有页眉的批注
    if (document.MainDocumentPart?.HeaderParts != null)
    {
        foreach (var headerPart in document.MainDocumentPart.HeaderParts)
        {
            if (headerPart.WordprocessingCommentsPart?.Comments != null)
            {
                maxId = Math.Max(maxId, headerPart.WordprocessingCommentsPart.Comments.Descendants<Comment>()
                    .Select(c => int.TryParse(c.Id?.Value, out int commentId) ? commentId : 0)
                    .DefaultIfEmpty(0)
                    .Max());
            }
        }
    }

    // 检查所有页脚的批注
    if (document.MainDocumentPart?.FooterParts != null)
    {
        foreach (var footerPart in document.MainDocumentPart.FooterParts)
        {
            if (footerPart.WordprocessingCommentsPart?.Comments != null)
            {
                maxId = Math.Max(maxId, footerPart.WordprocessingCommentsPart.Comments.Descendants<Comment>()
                    .Select(c => int.TryParse(c.Id?.Value, out int commentId) ? commentId : 0)
                    .DefaultIfEmpty(0)
                    .Max());
            }
        }
    }

    string id = (maxId + 1).ToString();
    _logger.LogDebug($"生成全局批注ID: {id}");
    return id;
}
```

**Step 2: 更新调用处**

将第 42 行和第 88 行的：
```csharp
string commentId = GenerateCommentId(commentsPart);
```

改为：
```csharp
string commentId = GenerateCommentId(document);
```

**Step 3: 编译验证**

Run: `dotnet build`
Expected: 编译成功

**Step 4: 提交**

```bash
git add Services/CommentManager.cs
git commit -m "feat(comment): 实现全局批注 ID 管理"
```

---

## Task 6: 实现页眉页脚批注部分支持

**Files:**
- Modify: `Services/CommentManager.cs`

**Step 1: 添加查找控件所在部分的方法**

在 `GetCommentsPartForLocation` 方法后添加：

```csharp
/// <summary>
/// 查找包含指定控件的页眉部分
/// </summary>
private HeaderPart? FindContainingHeaderPart(WordprocessingDocument document, SdtElement control)
{
    if (document.MainDocumentPart?.HeaderParts == null)
        return null;

    foreach (var headerPart in document.MainDocumentPart.HeaderParts)
    {
        if (headerPart.Header != null && headerPart.Header.Descendants<SdtElement>().Contains(control))
            return headerPart;
    }

    return null;
}

/// <summary>
/// 查找包含指定控件的页脚部分
/// </summary>
private FooterPart? FindContainingFooterPart(WordprocessingDocument document, SdtElement control)
{
    if (document.MainDocumentPart?.FooterParts == null)
        return null;

    foreach (var footerPart in document.MainDocumentPart.FooterParts)
    {
        if (footerPart.Footer != null && footerPart.Footer.Descendants<SdtElement>().Contains(footerPart))
            return footerPart;
    }

    return null;
}
```

**Step 2: 添加获取页眉页脚批注部分的方法**

```csharp
/// <summary>
/// 获取或创建页眉/页脚的批注部分
/// </summary>
private WordprocessingCommentsPart GetOrCreateHeaderFooterCommentsPart(OpenXmlPart part)
{
    WordprocessingCommentsPart? commentsPart = null;

    if (part is HeaderPart headerPart)
    {
        commentsPart = headerPart.WordprocessingCommentsPart;
        if (commentsPart == null)
        {
            _logger.LogDebug("创建页眉的批注部分");
            commentsPart = headerPart.AddNewPart<WordprocessingCommentsPart>();
            commentsPart.Comments = new Comments();
        }
    }
    else if (part is FooterPart footerPart)
    {
        commentsPart = footerPart.WordprocessingCommentsPart;
        if (commentsPart == null)
        {
            _logger.LogDebug("创建页脚的批注部分");
            commentsPart = footerPart.AddNewPart<WordprocessingCommentsPart>();
            commentsPart.Comments = new Comments();
        }
    }

    return commentsPart ?? throw new InvalidOperationException("无法创建批注部分");
}
```

**Step 3: 修改 GetCommentsPartForLocation 以支持页眉页脚**

```csharp
/// <summary>
/// 根据位置获取或创建批注部分
/// </summary>
private WordprocessingCommentsPart GetCommentsPartForLocation(
    WordprocessingDocument document,
    ContentControlLocation location,
    SdtElement? control = null)
{
    return location switch
    {
        ContentControlLocation.Body => GetOrCreateMainCommentsPart(document.MainDocumentPart!),
        ContentControlLocation.Header when control != null =>
        {
            var headerPart = FindContainingHeaderPart(document, control);
            if (headerPart == null)
            {
                _logger.LogWarning("未找到包含控件的页眉部分，使用主文档批注部分");
                return GetOrCreateMainCommentsPart(document.MainDocumentPart!);
            }
            return GetOrCreateHeaderFooterCommentsPart(headerPart);
        },
        ContentControlLocation.Footer when control != null =>
        {
            var footerPart = FindContainingFooterPart(document, control);
            if (footerPart == null)
            {
                _logger.LogWarning("未找到包含控件的页脚部分，使用主文档批注部分");
                return GetOrCreateMainCommentsPart(document.MainDocumentPart!);
            }
            return GetOrCreateHeaderFooterCommentsPart(footerPart);
        },
        _ => throw new ArgumentException($"不支持的位置: {location}")
    };
}
```

**Step 4: 编译验证**

Run: `dotnet build`
Expected: 编译成功

**Step 5: 提交**

```bash
git add Services/CommentManager.cs
git commit -m "feat(comment): 添加页眉页脚批注部分支持"
```

---

## Task 7: 更新批注方法签名传递控件

**Files:**
- Modify: `Services/CommentManager.cs:25-106`
- Modify: `Services/ContentControlProcessor.cs:290-321`

**Step 1: 修改 CommentManager 方法签名**

```csharp
public void AddCommentToElement(
    WordprocessingDocument document,
    Run targetRun,
    string commentText,
    string author,
    string tag,
    ContentControlLocation location = ContentControlLocation.Body,
    SdtElement? control = null)
{
    // ...
    WordprocessingCommentsPart? commentsPart = GetCommentsPartForLocation(document, location, control);
    // ...
}

public void AddCommentToRunRange(
    WordprocessingDocument document,
    System.Collections.Generic.List<Run> targetRuns,
    string commentText,
    string author,
    string tag,
    ContentControlLocation location = ContentControlLocation.Body,
    SdtElement? control = null)
{
    // ...
    WordprocessingCommentsPart? commentsPart = GetCommentsPartForLocation(document, location, control);
    // ...
}
```

**Step 2: 更新 ContentControlProcessor 调用处**

```csharp
_commentManager.AddCommentToElement(document, targetRuns[0], commentText, "DocuFiller系统", tag, location, control);

_commentManager.AddCommentToRunRange(document, targetRuns, commentText, "DocuFiller系统", tag, location, control);
```

**Step 3: 编译验证**

Run: `dotnet build`
Expected: 编译成功

**Step 4: 提交**

```bash
git add Services/CommentManager.cs Services/ContentControlProcessor.cs
git commit -m "refactor(comment): 批注方法添加 control 参数"
```

---

## Task 8: 编写页眉批注单元测试

**Files:**
- Create: `Tests/HeaderFooterCommentTests.cs`

**Step 1: 创建测试文件基础结构**

```csharp
using System;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocuFiller.Models;
using DocuFiller.Services;
using Microsoft.Extensions.Logging;
using Xunit;

namespace DocuFiller.Tests
{
    public class HeaderFooterCommentTests : IDisposable
    {
        private readonly string _testOutputDir;
        private readonly ILogger<CommentManager> _logger;
        private readonly CommentManager _commentManager;

        public HeaderFooterCommentTests()
        {
            _testOutputDir = Path.Combine(Path.GetTempPath(), "DocuFiller_Tests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testOutputDir);

            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _logger = loggerFactory.CreateLogger<CommentManager>();
            _commentManager = new CommentManager(_logger);
        }

        public void Dispose()
        {
            if (Directory.Exists(_testOutputDir))
            {
                try { Directory.Delete(_testOutputDir, true); }
                catch { /* 忽略清理失败 */ }
            }
        }

        [Fact]
        public void AddCommentToHeader_ShouldCreateHeaderCommentsPart()
        {
            // Arrange
            string templatePath = Path.Combine(_testOutputDir, "template.docx");
            CreateTestDocumentWithHeader(templatePath);

            using var document = WordprocessingDocument.Open(templatePath, true);
            var headerPart = document.MainDocumentPart!.HeaderParts.First();
            var header = headerPart.Header!;
            var sdtBlock = header.Descendants<SdtBlock>().First();
            var run = sdtBlock.Descendants<Run>().First();

            // Act
            _commentManager.AddCommentToElement(
                document,
                run,
                "测试批注",
                "测试作者",
                "TestTag",
                ContentControlLocation.Header,
                sdtBlock);

            // Assert
            Assert.NotNull(headerPart.WordprocessingCommentsPart);
            Assert.Equal(1, headerPart.WordprocessingCommentsPart.Comments.Count());
            Assert.Equal("测试批注", headerPart.WordprocessingCommentsPart.Comments.First().GetFirstChild<Paragraph>()?.InnerText);
        }

        private void CreateTestDocumentWithHeader(string path)
        {
            using var document = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new Document(new Body());

            // 添加页眉
            var headerPart = mainPart.AddNewPart<HeaderPart>();
            var header = new Header();
            var sdtBlock = new SdtBlock(
                new SdtProperties(
                    new Tag() { Val = "HeaderField" }
                ),
                new SdtContentBlock(
                    new Paragraph(new Run(new Text("页眉内容")))
                )
            );
            header.Append(sdtBlock);
            headerPart.Header = header;
        }
    }
}
```

**Step 2: 运行测试验证失败**

Run: `dotnet test Tests/HeaderFooterCommentTests.cs`
Expected: FAIL（测试文件存在但功能未实现）

**Step 3: 修复 FindContainingFooterPart 方法中的 Bug**

在 `CommentManager.cs` 中，第 XX 行有个错误：
```csharp
if (footerPart.Footer != null && footerPart.Footer.Descendants<SdtElement>().Contains(footerPart))
```

应该改为：
```csharp
if (footerPart.Footer != null && footerPart.Footer.Descendants<SdtElement>().Contains(control))
```

**Step 4: 运行测试验证通过**

Run: `dotnet test Tests/HeaderFooterCommentTests.cs`
Expected: PASS

**Step 5: 提交**

```bash
git add Tests/HeaderFooterCommentTests.cs Services/CommentManager.cs
git commit -m "test(comment): 添加页眉批注单元测试并修复 Bug"
```

---

## Task 9: 编写页脚批注单元测试

**Files:**
- Modify: `Tests/HeaderFooterCommentTests.cs`

**Step 1: 添加页脚批注测试**

```csharp
[Fact]
public void AddCommentToFooter_ShouldCreateFooterCommentsPart()
{
    // Arrange
    string templatePath = Path.Combine(_testOutputDir, "template.docx");
    CreateTestDocumentWithFooter(templatePath);

    using var document = WordprocessingDocument.Open(templatePath, true);
    var footerPart = document.MainDocumentPart!.FooterParts.First();
    var footer = footerPart.Footer!;
    var sdtBlock = footer.Descendants<SdtBlock>().First();
    var run = sdtBlock.Descendants<Run>().First();

    // Act
    _commentManager.AddCommentToElement(
        document,
        run,
        "测试批注",
        "测试作者",
        "TestTag",
        ContentControlLocation.Footer,
        sdtBlock);

    // Assert
    Assert.NotNull(footerPart.WordprocessingCommentsPart);
    Assert.Equal(1, footerPart.WordprocessingCommentsPart.Comments.Count());
    Assert.Equal("测试批注", footerPart.WordprocessingCommentsPart.Comments.First().GetFirstChild<Paragraph>()?.InnerText);
}

private void CreateTestDocumentWithFooter(string path)
{
    using var document = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
    var mainPart = document.AddMainDocumentPart();
    mainPart.Document = new Document(new Body());

    // 添加页脚
    var footerPart = mainPart.AddNewPart<FooterPart>();
    var footer = new Footer();
    var sdtBlock = new SdtBlock(
        new SdtProperties(
            new Tag() { Val = "FooterField" }
        ),
        new SdtContentBlock(
            new Paragraph(new Run(new Text("页脚内容")))
        )
    );
    footer.Append(sdtBlock);
    footerPart.Footer = footer;
}
```

**Step 2: 运行测试**

Run: `dotnet test Tests/HeaderFooterCommentTests.cs --filter "AddCommentToFooter"`
Expected: PASS

**Step 3: 提交**

```bash
git add Tests/HeaderFooterCommentTests.cs
git commit -m "test(comment): 添加页脚批注单元测试"
```

---

## Task 10: 编写全局批注 ID 唯一性测试

**Files:**
- Modify: `Tests/HeaderFooterCommentTests.cs`

**Step 1: 添加批注 ID 唯一性测试**

```csharp
[Fact]
public void CommentsInDifferentParts_ShouldHaveUniqueIds()
{
    // Arrange
    string templatePath = Path.Combine(_testOutputDir, "template.docx");
    CreateTestDocumentWithHeaderAndFooter(templatePath);

    using var document = WordprocessingDocument.Open(templatePath, true);
    var headerPart = document.MainDocumentPart!.HeaderParts.First();
    var footerPart = document.MainDocumentPart.FooterParts.First();
    var body = document.MainDocumentPart.Document.Body!;

    var headerRun = headerPart.Header!.Descendants<Run>().First();
    var footerRun = footerPart.Footer!.Descendants<Run>().First();
    var bodyRun = body.Descendants<Run>().First();

    // Act
    _commentManager.AddCommentToElement(document, headerRun, "页眉批注", "作者", "Tag1", ContentControlLocation.Header, headerPart.Header!.Descendants<SdtBlock>().First());
    _commentManager.AddCommentToElement(document, footerRun, "页脚批注", "作者", "Tag2", ContentControlLocation.Footer, footerPart.Footer!.Descendants<SdtBlock>().First());
    _commentManager.AddCommentToElement(document, bodyRun, "正文批注", "作者", "Tag3", ContentControlLocation.Body, body.Descendants<SdtBlock>().First());

    // Assert - 验证所有批注 ID 全局唯一
    var allCommentIds = new System.Collections.Generic.List<string>();

    if (document.MainDocumentPart.WordprocessingCommentsPart?.Comments != null)
    {
        allCommentIds.AddRange(document.MainDocumentPart.WordprocessingCommentsPart.Comments.Select(c => c.Id!.Value!));
    }

    foreach (var header in document.MainDocumentPart.HeaderParts)
    {
        if (header.WordprocessingCommentsPart?.Comments != null)
        {
            allCommentIds.AddRange(header.WordprocessingCommentsPart.Comments.Select(c => c.Id!.Value!));
        }
    }

    foreach (var footer in document.MainDocumentPart.FooterParts)
    {
        if (footer.WordprocessingCommentsPart?.Comments != null)
        {
            allCommentIds.AddRange(footer.WordprocessingCommentsPart.Comments.Select(c => c.Id!.Value!));
        }
    }

    Assert.Equal(3, allCommentIds.Count);
    Assert.Equal(3, allCommentIds.Distinct().Count());
}

private void CreateTestDocumentWithHeaderAndFooter(string path)
{
    using var document = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
    var mainPart = document.AddMainDocumentPart();
    mainPart.Document = new Document(new Body(
        new SdtBlock(
            new SdtProperties(new Tag() { Val = "BodyField" }),
            new SdtContentBlock(new Paragraph(new Run(new Text("正文内容"))))
        )
    ));

    // 添加页眉
    var headerPart = mainPart.AddNewPart<HeaderPart>();
    headerPart.Header = new Header(new SdtBlock(
        new SdtProperties(new Tag() { Val = "HeaderField" }),
        new SdtContentBlock(new Paragraph(new Run(new Text("页眉内容"))))
    ));

    // 添加页脚
    var footerPart = mainPart.AddNewPart<FooterPart>();
    footerPart.Footer = new Footer(new SdtBlock(
        new SdtProperties(new Tag() { Val = "FooterField" }),
        new SdtContentBlock(new Paragraph(new Run(new Text("页脚内容"))))
    ));
}
```

**Step 2: 运行测试**

Run: `dotnet test Tests/HeaderFooterCommentTests.cs --filter "CommentsInDifferentParts"
Expected: PASS

**Step 3: 提交**

```bash
git add Tests/HeaderFooterCommentTests.cs
git commit -m "test(comment): 添加全局批注 ID 唯一性测试"
```

---

## Task 11: 创建集成测试验证真实文档

**Files:**
- Create: `Tests/Integration/HeaderFooterCommentIntegrationTests.cs`

**Step 1: 创建集成测试**

```csharp
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Packaging;
using DocuFiller.Services;
using Microsoft.Extensions.Logging;
using Xunit;

namespace DocuFiller.Tests.Integration
{
    public class HeaderFooterCommentIntegrationTests : IDisposable
    {
        private readonly string _testDir;
        private readonly ILoggerFactory _loggerFactory;

        public HeaderFooterCommentIntegrationTests()
        {
            _testDir = Path.Combine(Path.GetTempPath(), "Integration_Tests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDir);
            _loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
        }

        public void Dispose()
        {
            if (Directory.Exists(_testDir))
            {
                try { Directory.Delete(_testDir, true); }
                catch { /* 忽略清理失败 */ }
            }
        }

        [Fact]
        public async Task ProcessDocumentWithHeaderFooter_ShouldAddCommentsToAllParts()
        {
            // Arrange
            string templatePath = Path.Combine(_testDir, "template.docx");
            string outputPath = Path.Combine(_testDir, "output.docx");
            string dataPath = Path.Combine(_testDir, "data.json");

            CreateTestTemplate(templatePath);
            File.WriteAllText(dataPath, @"[{""HeaderField"":""新页眉"",""BodyField"":""新正文"",""FooterField"":""新页脚""}]");

            var processor = new DocumentProcessorService(
                _loggerFactory.CreateLogger<DocumentProcessorService>(),
                new JsonDataParser(_loggerFactory.CreateLogger<JsonDataParser>()),
                new FileService(_loggerFactory.CreateLogger<FileService>()),
                new ProgressReporter(),
                new ContentControlProcessor(
                    _loggerFactory.CreateLogger<ContentControlProcessor>(),
                    new CommentManager(_loggerFactory.CreateLogger<CommentManager>())),
                new CommentManager(_loggerFactory.CreateLogger<CommentManager>()));

            // Act
            bool success = await processor.ProcessSingleDocumentAsync(
                templatePath,
                outputPath,
                (await new JsonDataParser(_loggerFactory.CreateLogger<JsonDataParser>()).ParseJsonFileAsync(dataPath)).First());

            // Assert
            Assert.True(success);
            Assert.True(File.Exists(outputPath));

            using var document = WordprocessingDocument.Open(outputPath, false);

            // 验证页眉有批注
            var headerPart = document.MainDocumentPart!.HeaderParts.First();
            Assert.NotNull(headerPart.WordprocessingCommentsPart);
            Assert.True(headerPart.WordprocessingCommentsPart.Comments.Any());

            // 验证页脚有批注
            var footerPart = document.MainDocumentPart.FooterParts.First();
            Assert.NotNull(footerPart.WordprocessingCommentsPart);
            Assert.True(footerPart.WordprocessingCommentsPart.Comments.Any());

            // 验证正文有批注
            Assert.NotNull(document.MainDocumentPart.WordprocessingCommentsPart);
            Assert.True(document.MainDocumentPart.WordprocessingCommentsPart.Comments.Any());

            // 验证所有批注 ID 唯一
            var allIds = System.Collections.Generic.List<string>();
            allIds.AddRange(document.MainDocumentPart.WordprocessingCommentsPart.Comments.Select(c => c.Id!.Value!));
            allIds.AddRange(headerPart.WordprocessingCommentsPart.Comments.Select(c => c.Id!.Value!));
            allIds.AddRange(footerPart.WordprocessingCommentsPart.Comments.Select(c => c.Id!.Value!));

            Assert.Equal(allIds.Count, allIds.Distinct().Count());
        }

        private void CreateTestTemplate(string path)
        {
            using var document = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new Document(new Body(
                new SdtBlock(
                    new SdtProperties(new Tag() { Val = "BodyField" }),
                    new SdtContentBlock(new Paragraph(new Run(new Text("正文占位符"))))
                )
            ));

            var headerPart = mainPart.AddNewPart<HeaderPart>();
            headerPart.Header = new Header(new SdtBlock(
                new SdtProperties(new Tag() { Val = "HeaderField" }),
                new SdtContentBlock(new Paragraph(new Run(new Text("页眉占位符"))))
            ));

            var footerPart = mainPart.AddNewPart<FooterPart>();
            footerPart.Footer = new Footer(new SdtBlock(
                new SdtProperties(new Tag() { Val = "FooterField" }),
                new SdtContentBlock(new Paragraph(new Run(new Text("页脚占位符"))))
            ));
        }
    }
}
```

**Step 2: 运行集成测试**

Run: `dotnet test Tests/Integration/HeaderFooterCommentIntegrationTests.cs`
Expected: PASS

**Step 3: 提交**

```bash
git add Tests/Integration/HeaderFooterCommentIntegrationTests.cs
git commit -m "test(comment): 添加页眉页脚批注集成测试"
```

---

## Task 12: 更新功能文档

**Files:**
- Modify: `docs/features/header-footer-support.md`

**Step 1: 在文档中添加批注功能说明**

在 "批注支持" 部分添加详细说明：

```markdown
### 批注支持

页眉和页脚中的控件替换会添加位置标识到批注中：
- "此字段（页眉）于 [时间] 更新..."
- "此字段（页脚）于 [时间] 更新..."
- "此字段（正文）于 [时间] 更新..."

**批注存储**：
- 页眉批注存储在对应的 `HeaderPart.WordprocessingCommentsPart`
- 页脚批注存储在对应的 `FooterPart.WordprocessingCommentsPart`
- 正文批注存储在 `MainDocumentPart.WordprocessingCommentsPart`

**批注 ID 管理**：
- 所有批注（包括页眉页脚）共享全局唯一 ID 序列
- 确保文档中所有批注引用正确且无冲突
```

**Step 2: 提交**

```bash
git add docs/features/header-footer-support.md
git commit -m "docs(comment): 更新页眉页脚批注功能文档"
```

---

## Task 13: 手动验证测试

**Step 1: 创建测试脚本**

创建 `Tests/verify-header-footer-comments.bat`:

```batch
@echo off
cd /d "%~dp0"
echo Running header-footer comment verification...
dotnet test --filter "FullyQualifiedName~HeaderFooterComment"
echo Done.
pause
```

**Step 2: 运行所有测试**

Run: `cd Tests && verify-header-footer-comments.bat`
Expected: 所有测试通过

**Step 3: 提交**

```bash
git add Tests/verify-header-footer-comments.bat
git commit -m "test(comment): 添加页眉页脚批注验证脚本"
```

---

## Task 14: 最终清理和代码审查

**Files:**
- Modify: `Services/CommentManager.cs`
- Modify: `Services/ContentControlProcessor.cs`

**Step 1: 代码审查检查项**

- [ ] 所有公共方法有 XML 注释
- [ ] 日志记录完整且有意义
- [ ] 异常处理适当
- [ ] 没有 TODO 或 FIXME 注释
- [ ] 代码风格一致

**Step 2: 运行完整测试套件**

Run: `dotnet test`
Expected: 所有测试通过（包括现有测试）

**Step 3: 运行代码分析**

Run: `dotnet build`
Expected: 无警告或错误

**Step 4: 最终提交**

```bash
git add -A
git commit -m "feat(comment): 完成页眉页脚批注支持功能

- CommentManager 支持页眉页脚批注
- 批注存储在各自的 HeaderPart/FooterPart 中
- 实现全局批注 ID 管理
- 添加完整的单元测试和集成测试
- 更新功能文档

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude <noreply@anthropic.com>"
```

---

## 验收标准

✅ 页眉中的内容控件替换后显示批注
✅ 页脚中的内容控件替换后显示批注
✅ 所有批注 ID 在文档中全局唯一
✅ 现有功能（正文批注）不受影响
✅ 所有单元测试通过
✅ 集成测试通过
✅ 代码无编译警告
✅ 文档已更新
