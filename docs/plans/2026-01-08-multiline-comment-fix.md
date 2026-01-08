# Multiline Comment Range Fix Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use @superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 修复多行文本内容控件批注只覆盖第一行的问题，使批注范围正确覆盖所有替换的内容。

**Architecture:** 在 CommentManager 中新增支持多个 Run 元素的批注范围方法，在 ContentControlProcessor 中查找所有相关的 Run 元素，根据 Run 数量选择单批注或多批注方法。

**Tech Stack:** .NET 8, DocumentFormat.OpenXml, XUnit, Microsoft.Extensions.Logging

---

## Context

**问题：** 当内容控件替换的值包含多行文本时，`CreateFormattedRuns()` 会创建多个 Run 元素（文本 Run + Break Run），但 `FindTargetRun()` 只返回第一个 Run，导致 `AddCommentReference()` 只为第一个 Run 添加批注。

**解决方案：** 批注范围包含所有 Run 元素（包括文本 Run 和换行符 Break Run），使用 `CommentRangeStart` 在第一个 Run 之前，`CommentRangeEnd` 在最后一个 Run 之后。

---

### Task 1: Add AddCommentToRunRange method to CommentManager

**Files:**
- Modify: `Services/CommentManager.cs:60`

**Step 1: Write the failing test**

Create test file: `Tests/CommentManagerTests.cs`

```csharp
using Xunit;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml.Packaging;
using System.Collections.Generic;

namespace DocuFiller.Tests
{
    public class CommentManagerTests
    {
        [Fact]
        public void AddCommentToRunRange_WithMultipleRuns_AddsCommentRange()
        {
            // Arrange
            var testFilePath = Path.Combine(TestHelper.TestOutputPath, "test_run_range.docx");
            var logger = TestHelper.CreateLogger<CommentManager>();
            var commentManager = new CommentManager(logger);

            using (var document = WordprocessingDocument.Create(testFilePath, WordprocessingDocumentType.Document))
            {
                var mainPart = document.AddMainDocumentPart();
                mainPart.Document = new Document(new Body());

                var paragraph = new Paragraph();
                var run1 = new Run(new Text("Line1"));
                var run2 = new Run(new Break());
                var run3 = new Run(new Text("Line2"));

                paragraph.Append(run1, run2, run3);
                mainPart.Document.Body.Append(paragraph);
                mainPart.Document.Save();

                var runs = new List<Run> { run1, run2, run3 };

                // Act
                commentManager.AddCommentToRunRange(document, runs, "Test comment", "TestAuthor", "TestTag");
            }

            // Assert
            using (var document = WordprocessingDocument.Open(testFilePath, false))
            {
                var rangeStarts = document.MainDocumentPart.Document.Body.Descendants<CommentRangeStart>().ToList();
                var rangeEnds = document.MainDocumentPart.Document.Body.Descendants<CommentRangeEnd>().ToList();

                Assert.Single(rangeStarts);
                Assert.Single(rangeEnds);
                Assert.Equal(rangeStarts[0].Id.Value, rangeEnds[0].Id.Value);
            }
        }
    }
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test Tests/CommentManagerTests.cs -f net8.0`
Expected: FAIL with "AddCommentToRunRange method not found"

**Step 3: Add AddCommentToRunRange method**

File: `Services/CommentManager.cs:60`

Add after `AddCommentToElement` method:

```csharp
/// <summary>
/// 为多个连续的Run元素添加批注范围
/// </summary>
public void AddCommentToRunRange(
    WordprocessingDocument document,
    List<Run> targetRuns,
    string commentText,
    string author,
    string tag)
{
    try
    {
        if (targetRuns == null || targetRuns.Count == 0)
        {
            _logger.LogWarning($"目标Run列表为空，无法添加批注，标签: '{tag}'");
            return;
        }

        _logger.LogDebug($"开始为 {targetRuns.Count} 个Run元素添加批注范围，标签: '{tag}'");

        if (document.MainDocumentPart == null)
        {
            _logger.LogError("文档主体部分为空，无法添加批注");
            return;
        }

        WordprocessingCommentsPart? commentsPart = GetOrCreateCommentsPart(document.MainDocumentPart);
        string commentId = GenerateCommentId(commentsPart);
        Comment comment = CreateComment(commentId, commentText, author);
        SaveComment(commentsPart, comment);
        AddCommentRangeReference(targetRuns, commentId);

        _logger.LogInformation($"✓ 成功为 {targetRuns.Count} 个Run元素添加批注范围，标签: '{tag}'，ID: {commentId}");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, $"为Run范围添加批注时发生异常，标签: '{tag}': {ex.Message}");
        throw;
    }
}
```

**Step 4: Add AddCommentRangeReference private method**

File: `Services/CommentManager.cs:141`

Add after `AddCommentReference` method:

```csharp
/// <summary>
/// 为Run范围添加批注引用
/// </summary>
private void AddCommentRangeReference(List<Run> targetRuns, string commentId)
{
    if (targetRuns == null || targetRuns.Count == 0)
        return;

    Run firstRun = targetRuns[0];
    Run lastRun = targetRuns[targetRuns.Count - 1];

    firstRun.InsertBeforeSelf(new CommentRangeStart() { Id = commentId });
    lastRun.InsertAfterSelf(new CommentRangeEnd() { Id = commentId });
    lastRun.Append(new CommentReference() { Id = commentId });
}
```

**Step 5: Run test to verify it passes**

Run: `dotnet test Tests/CommentManagerTests.cs -f net8.0`
Expected: PASS

**Step 6: Commit**

```bash
git add Services/CommentManager.cs Tests/CommentManagerTests.cs
git commit -m "feat(comment): add support for multi-run comment ranges"
```

---

### Task 2: Add FindAllTargetRuns method to ContentControlProcessor

**Files:**
- Modify: `Services/ContentControlProcessor.cs:328`

**Step 1: Write the failing test**

File: `Tests/ContentControlProcessorTests.cs`

```csharp
[Fact]
public void FindAllTargetRuns_WithMultipleRunParagraph_ReturnsAllRuns()
{
    // Arrange
    var logger = TestHelper.CreateLogger<ContentControlProcessor>();
    var commentManager = new CommentManager(logger);
    var processor = new ContentControlProcessor(logger, commentManager);

    var paragraph = new Paragraph();
    var run1 = new Run(new Text("Line1"));
    var run2 = new Run(new Break());
    var run3 = new Run(new Text("Line2"));
    paragraph.Append(run1, run2, run3);

    var sdtContent = new SdtContentBlock(paragraph);
    var sdtBlock = new SdtBlock(sdtContent);

    // Act
    var runs = processor.InvokePrivateMethod<List<Run>>("FindAllTargetRuns", sdtBlock);

    // Assert
    Assert.Equal(3, runs.Count);
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test Tests/ContentControlProcessorTests.cs -f net8.0`
Expected: FAIL with "method not found"

**Step 3: Add FindAllTargetRuns method**

File: `Services/ContentControlProcessor.cs:328`

Add after `FindTargetRun` method:

```csharp
/// <summary>
/// 查找内容控件中所有相关的Run元素
/// </summary>
private List<Run> FindAllTargetRuns(SdtElement control)
{
    List<Run> runs = new List<Run>();

    OpenXmlElement? content = FindContentContainer(control);
    if (content != null)
    {
        if (content is SdtContentBlock || content is SdtContentCell)
        {
            runs = content.Descendants<Run>().ToList();
        }
        else if (content is SdtContentRun)
        {
            runs = content.Descendants<Run>().ToList();
        }
    }
    else
    {
        runs = control.Descendants<Run>().ToList();
    }

    _logger.LogDebug($"在内容控件中找到 {runs.Count} 个Run元素");
    return runs;
}
```

**Step 4: Run test to verify it passes**

Run: `dotnet test Tests/ContentControlProcessorTests.cs -f net8.0`
Expected: PASS

**Step 5: Commit**

```bash
git add Services/ContentControlProcessor.cs Tests/ContentControlProcessorTests.cs
git commit -m "feat(processor): add FindAllTargetRuns method"
```

---

### Task 3: Modify AddProcessingComment to support multiline

**Files:**
- Modify: `Services/ContentControlProcessor.cs:290-305`

**Step 1: Write the failing test**

File: `Tests/ContentControlProcessorTests.cs`

```csharp
[Fact]
public void AddProcessingComment_MultilineText_CoversAllLines()
{
    // Arrange
    var data = new Dictionary<string, object> { { "TestField", "Line1\nLine2\nLine3" } };
    var testFilePath = Path.Combine(_testOutputPath, "test_multiline_comment.docx");

    using (var document = WordprocessingDocument.Create(
        testFilePath, WordprocessingDocumentType.Document))
    {
        var mainPart = document.AddMainDocumentPart();
        mainPart.Document = new Document(new Body());

        var sdtBlock = new SdtBlock();
        var sdtProperties = new SdtProperties(new Tag { Val = "TestField" });
        var sdtContent = new SdtContentBlock(new Paragraph(new Run(new Text("OldValue"))));
        sdtBlock.Append(sdtProperties, sdtContent);
        mainPart.Document.Body.Append(sdtBlock);
        mainPart.Document.Save();

        // Act
        _processor.ProcessContentControl(
            sdtBlock, data, document, ContentControlLocation.Body);
    }

    // Assert - 验证批注覆盖所有行
    using (var document = WordprocessingDocument.Open(testFilePath, false))
    {
        var commentRangeStarts = document.MainDocumentPart?.Document.Body?.Descendants<CommentRangeStart>().ToList();
        var commentRangeEnds = document.MainDocumentPart?.Document.Body?.Descendants<CommentRangeEnd>().ToList();

        Assert.NotNull(commentRangeStarts);
        Assert.Single(commentRangeStarts);

        Assert.NotNull(commentRangeEnds);
        Assert.Single(commentRangeEnds);

        Assert.Equal(commentRangeStarts[0].Id?.Value, commentRangeEnds[0].Id?.Value);
    }

    CleanupTestFile(testFilePath);
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test Tests/ContentControlProcessorTests.cs -f net8.0`
Expected: FAIL - comment only covers first line

**Step 3: Modify AddProcessingComment method**

File: `Services/ContentControlProcessor.cs:290-305`

Replace entire method:

```csharp
/// <summary>
/// 添加处理批注
/// </summary>
private void AddProcessingComment(
    WordprocessingDocument document,
    SdtElement control,
    string tag,
    string newValue,
    string oldValue,
    ContentControlLocation location)
{
    List<Run> targetRuns = FindAllTargetRuns(control);

    if (targetRuns.Count == 0)
    {
        _logger.LogWarning($"未找到目标Run元素，跳过批注添加，标签: '{tag}'");
        return;
    }

    string currentTime = DateTime.Now.ToString("yyyy年M月d日 HH:mm:ss");
    string locationText = location switch
    {
        ContentControlLocation.Header => "页眉",
        ContentControlLocation.Footer => "页脚",
        _ => "正文"
    };
    string commentText = $"此字段（{locationText}）已于 {currentTime} 更新。标签：{tag}，旧值：[{oldValue}]，新值：{newValue}";

    if (targetRuns.Count == 1)
    {
        _commentManager.AddCommentToElement(document, targetRuns[0], commentText, "DocuFiller系统", tag);
    }
    else
    {
        _commentManager.AddCommentToRunRange(document, targetRuns, commentText, "DocuFiller系统", tag);
    }
}
```

**Step 4: Run test to verify it passes**

Run: `dotnet test Tests/ContentControlProcessorTests.cs -f net8.0`
Expected: PASS

**Step 5: Commit**

```bash
git add Services/ContentControlProcessor.cs Tests/ContentControlProcessorTests.cs
git commit -m "feat(processor): use range comment for multiline text"
```

---

### Task 4: Add test for single line backward compatibility

**Files:**
- Test: `Tests/ContentControlProcessorTests.cs`

**Step 1: Write the test**

```csharp
[Fact]
public void AddProcessingComment_SingleLineText_UsesOriginalMethod()
{
    // Arrange
    var data = new Dictionary<string, object> { { "TestField", "SingleLine" } };
    var testFilePath = Path.Combine(_testOutputPath, "test_singleline_comment.docx");

    using (var document = WordprocessingDocument.Create(
        testFilePath, WordprocessingDocumentType.Document))
    {
        var mainPart = document.AddMainDocumentPart();
        mainPart.Document = new Document(new Body());

        var sdtBlock = new SdtBlock();
        var sdtProperties = new SdtProperties(new Tag { Val = "TestField" });
        var sdtContent = new SdtContentBlock(new Paragraph(new Run(new Text("OldValue"))));
        sdtBlock.Append(sdtProperties, sdtContent);
        mainPart.Document.Body.Append(sdtBlock);
        mainPart.Document.Save();

        // Act
        _processor.ProcessContentControl(
            sdtBlock, data, document, ContentControlLocation.Body);
    }

    // Assert - 验证单行文本批注仍然正常工作
    using (var document = WordprocessingDocument.Open(testFilePath, false))
    {
        var comments = document.MainDocumentPart?.WordprocessingCommentsPart?.Comments?.Descendants<Comment>().ToList();

        Assert.NotNull(comments);
        Assert.Single(comments);
    }

    CleanupTestFile(testFilePath);
}
```

**Step 2: Run test to verify it passes**

Run: `dotnet test Tests/ContentControlProcessorTests.cs -f net8.0`
Expected: PASS

**Step 3: Commit**

```bash
git add Tests/ContentControlProcessorTests.cs
git commit -m "test(processor): add single line backward compatibility test"
```

---

### Task 5: Manual verification test

**Files:**
- Test data: `Examples/test-multiline.json`
- Template: Create test template with multiline content controls

**Step 1: Create test data file**

File: `Examples/test-multiline.json`

```json
[
  {
    "SingleField": "单行文本",
    "MultiField2": "第一行\n第二行",
    "MultiField3": "第一行\n第二行\n第三行",
    "MultiField5": "Line1\nLine2\nLine3\nLine4\nLine5"
  }
]
```

**Step 2: Create test template**

Create template with content controls for each field above.

**Step 3: Run application**

Run: `dotnet run`
Select template and test data file.

**Step 4: Open generated document in Word**

Verify:
- Each field shows correct content
- Each field has comment covering all lines
- Comment bubble appears and shows full content

**Step 5: Commit**

```bash
git add Examples/test-multiline.json
git commit -m "test(examples): add multiline test data and verification"
```

---

### Task 6: Run full test suite

**Files:**
- All test files

**Step 1: Run all tests**

Run: `dotnet test`
Expected: All tests PASS

**Step 2: Build project**

Run: `dotnet build -c Release`
Expected: Build succeeds

**Step 3: Final commit**

```bash
git add .
git commit -m "feat: complete multiline comment range fix implementation"
```

---

## Summary

**Modified Files:**
- `Services/CommentManager.cs` - Added `AddCommentToRunRange()` and `AddCommentRangeReference()` methods
- `Services/ContentControlProcessor.cs` - Added `FindAllTargetRuns()`, modified `AddProcessingComment()`
- `Tests/CommentManagerTests.cs` - New test file
- `Tests/ContentControlProcessorTests.cs` - Added multiline tests

**Backward Compatibility:** Single-line text continues using the original `AddCommentToElement()` method.

**OpenXML Structure:**
```xml
<w:commentRangeStart w:id="1"/>
<w:r><w:t>Line1</w:t></w:r>
<w:r><w:br/></w:r>
<w:r><w:t>Line2</w:t></w:r>
<w:commentRangeEnd w:id="1"/>
<w:r><w:commentReference w:id="1"/></w:r>
```
