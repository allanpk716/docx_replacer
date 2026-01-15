# 表格单元格内容控件格式保留修复

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**目标:** 修复表格单元格中内容控件替换时导致表格格式错乱和内容丢失的问题

**架构:** 通过检测表格单元格上下文，采用安全的文本替换策略，保留表格单元格的段落结构和格式设置，避免破坏 OpenXML 表格结构

**技术栈:** .NET 8, OpenXML SDK 2.20, WPF, xUnit

---

## 问题背景

当前系统在处理 Word 文档表格单元格内的内容控件时，使用 `RemoveAllChildren()` 清空内容后重建，这会破坏表格单元格的段落结构，导致：
1. 表格格式错乱（边框、列宽等丢失）
2. 内容"跑到下一行"
3. 部分内容完全丢失

**根本原因:**
- `ContentControlProcessor.ReplaceContentInContainer()` (行 234-257) 和 `DocumentProcessorService.FillContentControlWithFormattedValue()` (行 718-776) 在处理表格单元格内容控件时，删除了单元格的所有子元素并重建段落，破坏了 OpenXML 表格结构

---

## 实现任务

### Task 1: 创建表格单元格检测工具类

**文件:**
- Create: `DocuFiller/Utils/OpenXmlTableCellHelper.cs`

**Step 1: 编写测试 - 检测内容控件是否在表格单元格中**

```csharp
// Tests/DocuFiller.Tests/Utils/OpenXmlTableCellHelperTests.cs
using DocumentFormat.OpenXml.Wordprocessing;
using DocuFiller.Utils;
using Xunit;

namespace DocuFiller.Tests.Utils
{
    public class OpenXmlTableCellHelperTests
    {
        [Fact]
        public void IsInTableCell_WhenControlInTableCell_ReturnsTrue()
        {
            // Arrange
            var table = new Table(
                new TableRow(
                    new TableCell(
                        new Paragraph(
                            new SdtRun(new SdtProperties(new Tag() { Val = "test" }))
                        )
                    )
                )
            );
            var control = table.Descendants<SdtRun>().First();

            // Act
            bool result = OpenXmlTableCellHelper.IsInTableCell(control);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsInTableCell_WhenControlInBody_ReturnsFalse()
        {
            // Arrange
            var body = new Document(
                new Paragraph(
                    new SdtRun(new SdtProperties(new Tag() { Val = "test" }))
                )
            );
            var control = body.Descendants<SdtRun>().First();

            // Act
            bool result = OpenXmlTableCellHelper.IsInTableCell(control);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void GetParentTableCell_WhenInTableCell_ReturnsCell()
        {
            // Arrange
            var table = new Table(
                new TableRow(
                    new TableCell(
                        new Paragraph(
                            new SdtRun(new SdtProperties(new Tag() { Val = "test" }))
                        )
                    )
                )
            );
            var control = table.Descendants<SdtRun>().First();

            // Act
            var cell = OpenXmlTableCellHelper.GetParentTableCell(control);

            // Assert
            Assert.NotNull(cell);
            Assert.IsType<TableCell>(cell);
        }

        [Fact]
        public void GetParentTableCell_WhenNotInTableCell_ReturnsNull()
        {
            // Arrange
            var body = new Document(
                new Paragraph(
                    new SdtRun(new SdtProperties(new Tag() { Val = "test" }))
                )
            );
            var control = body.Descendants<SdtRun>().First();

            // Act
            var cell = OpenXmlTableCellHelper.GetParentTableCell(control);

            // Assert
            Assert.Null(cell);
        }
    }
}
```

**Step 2: 运行测试验证失败**

```bash
cd C:\WorkSpace\Go2Hell\src\github.com\allanpk716\docx_replacer
dotnet test Tests/DocuFiller.Tests/Utils/OpenXmlTableCellHelperTests.cs -v n
```

预期: FAIL - "OpenXmlTableCellHelper does not exist"

**Step 3: 实现工具类**

```csharp
// DocuFiller/Utils/OpenXmlTableCellHelper.cs
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

namespace DocuFiller.Utils
{
    /// <summary>
    /// OpenXML 表格单元格辅助工具类
    /// </summary>
    public static class OpenXmlTableCellHelper
    {
        /// <summary>
        /// 检测指定元素是否在表格单元格内
        /// </summary>
        /// <param name="element">要检测的元素</param>
        /// <returns>如果在表格单元格内返回 true，否则返回 false</returns>
        public static bool IsInTableCell(OpenXmlElement element)
        {
            if (element == null)
                return false;

            // 向上遍历父元素链，查找是否有 TableCell
            OpenXmlElement? current = element;
            while (current != null)
            {
                if (current is TableCell)
                    return true;

                // 避免无限循环和过度遍历
                current = current.Parent;
                if (current is Document || current is Header || current is Footer)
                    break;
            }

            return false;
        }

        /// <summary>
        /// 获取元素所在的表格单元格
        /// </summary>
        /// <param name="element">要查找的元素</param>
        /// <returns>如果找到返回 TableCell，否则返回 null</returns>
        public static TableCell? GetParentTableCell(OpenXmlElement element)
        {
            if (element == null)
                return null;

            // 向上遍历父元素链，查找 TableCell
            OpenXmlElement? current = element;
            while (current != null)
            {
                if (current is TableCell cell)
                    return cell;

                // 避免无限循环和过度遍历
                current = current.Parent;
                if (current is Document || current is Header || current is Footer)
                    break;
            }

            return null;
        }

        /// <summary>
        /// 获取表格单元格中唯一的段落（用于表格单元格内容控件处理）
        /// </summary>
        /// <param name="cell">表格单元格</param>
        /// <returns>单元格中的段落，如果没有则创建并返回一个新段落</returns>
        public static Paragraph GetOrCreateSingleParagraph(TableCell cell)
        {
            if (cell == null)
                throw new ArgumentNullException(nameof(cell));

            // 获取单元格中的所有段落
            var paragraphs = cell.Elements<Paragraph>().ToList();

            // 如果没有段落，创建一个
            if (paragraphs.Count == 0)
            {
                var newParagraph = new Paragraph();
                cell.AppendChild(newParagraph);
                return newParagraph;
            }

            // 如果有多个段落，保留第一个，删除其他段落（但保留其内容）
            if (paragraphs.Count > 1)
            {
                var firstParagraph = paragraphs[0];
                for (int i = 1; i < paragraphs.Count; i++)
                {
                    var extraParagraph = paragraphs[i];
                    // 将段落中的 Run 移动到第一个段落
                    foreach (var run in extraParagraph.Elements<Run>().ToList())
                    {
                        run.Remove();
                        firstParagraph.AppendChild(run);
                    }
                    // 删除多余段落
                    extraParagraph.Remove();
                }
                return firstParagraph;
            }

            return paragraphs[0];
        }
    }
}
```

**Step 4: 运行测试验证通过**

```bash
cd C:\WorkSpace\Go2Hell\src\github.com\allanpk716\docx_replacer
dotnet test Tests/DocuFiller.Tests/Utils/OpenXmlTableCellHelperTests.cs -v n
```

预期: PASS

**Step 5: 提交**

```bash
git add DocuFiller/Utils/OpenXmlTableCellHelper.cs Tests/DocuFiller.Tests/Utils/OpenXmlTableCellHelperTests.cs
git commit -m "feat: 添加表格单元格检测工具类"
```

---

### Task 2: 创建安全的文本替换服务

**文件:**
- Create: `DocuFiller/Services/Interfaces/ISafeTextReplacer.cs`
- Create: `DocuFiller/Services/SafeTextReplacer.cs`
- Test: `Tests/DocuFiller.Tests/Services/SafeTextReplacerTests.cs`

**Step 1: 编写测试 - 安全替换表格单元格内容控件文本**

```csharp
// Tests/DocuFiller.Tests/Services/SafeTextReplacerTests.cs
using DocumentFormat.OpenXml.Wordprocessing;
using DocuFiller.Services;
using Xunit;

namespace DocuFiller.Tests.Services
{
    public class SafeTextReplacerTests
    {
        [Fact]
        public void ReplaceTextInControl_SimpleText_ReplacesCorrectly()
        {
            // Arrange
            var run = new Run(new Text("old text"));
            var sdtRun = new SdtRun(
                new SdtProperties(new Tag() { Val = "test" }),
                new SdtContentRun(run)
            );

            // Act
            SafeTextReplacer.ReplaceTextInControl(sdtRun, "new text");

            // Assert
            var newText = run.Descendants<Text>().First();
            Assert.Equal("new text", newText.Text);
        }

        [Fact]
        public void ReplaceTextInControl_PreservesTableCellStructure()
        {
            // Arrange
            var cell = new TableCell(
                new Paragraph(
                    new SdtRun(
                        new SdtProperties(new Tag() { Val = "test" }),
                        new SdtContentRun(new Run(new Text("old")))
                    )
                )
            );

            var originalChildCount = cell.ChildElements.Count;

            // Act
            var control = cell.Descendants<SdtRun>().First();
            SafeTextReplacer.ReplaceTextInControl(control, "new");

            // Assert
            // 验证单元格子元素数量没有增加
            Assert.Equal(originalChildCount, cell.ChildElements.Count);
            // 验证文本被替换
            var newText = cell.Descendants<Text>().First();
            Assert.Equal("new", newText.Text);
        }

        [Fact]
        public void ReplaceTextInControl_MultipleRuns_MergesIntoSingleRun()
        {
            // Arrange
            var sdtRun = new SdtRun(
                new SdtProperties(new Tag() { Val = "test" }),
                new SdtContentRun(
                    new Run(new Text("part1")),
                    new Run(new Text("part2"))
                )
            );

            // Act
            SafeTextReplacer.ReplaceTextInControl(sdtRun, "new text");

            // Assert
            var runs = sdtRun.Descendants<Run>().ToList();
            // 应该只剩一个 Run
            Assert.Single(runs);
            Assert.Equal("new text", runs[0].Descendants<Text>().First().Text);
        }
    }
}
```

**Step 2: 运行测试验证失败**

```bash
cd C:\WorkSpace\Go2Hell\src\github.com\allanpk716\docx_replacer
dotnet test Tests/DocuFiller.Tests/Services/SafeTextReplacerTests.cs -v n
```

预期: FAIL - "SafeTextReplacer does not exist"

**Step 3: 创建接口**

```csharp
// DocuFiller/Services/Interfaces/ISafeTextReplacer.cs
using DocumentFormat.OpenXml.Wordprocessing;

namespace DocuFiller.Services.Interfaces
{
    /// <summary>
    /// 安全文本替换服务接口
    /// </summary>
    public interface ISafeTextReplacer
    {
        /// <summary>
        /// 安全地替换内容控件中的文本，保留结构
        /// </summary>
        /// <param name="control">内容控件元素</param>
        /// <param name="newText">新文本</param>
        void ReplaceTextInControl(SdtElement control, string newText);
    }
}
```

**Step 4: 实现安全文本替换服务**

```csharp
// DocuFiller/Services/SafeTextReplacer.cs
using System.Linq;
using DocumentFormat.OpenXml.Wordprocessing;
using DocuFiller.Services.Interfaces;
using DocuFiller.Utils;
using Microsoft.Extensions.Logging;

namespace DocuFiller.Services
{
    /// <summary>
    /// 安全文本替换服务实现
    /// 通过精确替换文本节点而非删除重建，保留表格单元格结构
    /// </summary>
    public class SafeTextReplacer : ISafeTextReplacer
    {
        private readonly ILogger<SafeTextReplacer> _logger;

        public SafeTextReplacer(ILogger<SafeTextReplacer> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 安全地替换内容控件中的文本，保留结构
        /// </summary>
        public void ReplaceTextInControl(SdtElement control, string newText)
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));

            _logger.LogDebug($"开始安全替换内容控件文本: '{newText}'");

            // 1. 查找内容容器
            var contentContainer = FindContentContainer(control);
            if (contentContainer == null)
            {
                _logger.LogWarning("未找到内容容器，无法替换文本");
                return;
            }

            // 2. 检查是否在表格单元格中
            bool isInTableCell = OpenXmlTableCellHelper.IsInTableCell(control);

            // 3. 根据位置选择替换策略
            if (isInTableCell)
            {
                _logger.LogDebug("检测到表格单元格内容控件，使用安全替换策略");
                ReplaceTextInTableCell(contentContainer, newText, control);
            }
            else
            {
                _logger.LogDebug("非表格单元格内容控件，使用标准替换策略");
                ReplaceTextStandard(contentContainer, newText, control);
            }

            _logger.LogInformation("✓ 安全替换完成");
        }

        /// <summary>
        /// 查找内容控件的内容容器
        /// </summary>
        private OpenXmlElement? FindContentContainer(SdtElement control)
        {
            var runContent = control.Descendants<SdtContentRun>().FirstOrDefault();
            if (runContent != null) return runContent;

            var blockContent = control.Descendants<SdtContentBlock>().FirstOrDefault();
            if (blockContent != null) return blockContent;

            var cellContent = control.Descendants<SdtContentCell>().FirstOrDefault();
            return cellContent;
        }

        /// <summary>
        /// 表格单元格安全替换策略
        /// 关键：不删除段落结构，只替换文本内容
        /// </summary>
        private void ReplaceTextInTableCell(OpenXmlElement contentContainer, string newText, SdtElement control)
        {
            // 获取所有 Run 元素
            var runs = contentContainer.Descendants<Run>().ToList();

            if (runs.Count == 0)
            {
                _logger.LogWarning("内容容器中没有找到 Run 元素");
                return;
            }

            // 策略：保留第一个 Run，清空其内容并设置新文本
            // 删除其他多余的 Run（但保留第一个 Run 的格式）
            var firstRun = runs[0];

            // 清空第一个 Run 的所有子元素
            firstRun.RemoveAllChildren();

            // 创建新的 Text 元素
            var textElement = new Text(newText)
            {
                Space = SpaceProcessingModeValues.Preserve
            };
            firstRun.AppendChild(textElement);

            // 移除其他多余的 Run
            for (int i = 1; i < runs.Count; i++)
            {
                runs[i].Remove();
            }

            _logger.LogDebug($"表格单元格安全替换: 保留第一个 Run，删除 {runs.Count - 1} 个多余 Run");
        }

        /// <summary>
        /// 标准替换策略（非表格单元格）
        /// </summary>
        private void ReplaceTextStandard(OpenXmlElement contentContainer, string newText, SdtElement control)
        {
            // 标准逻辑：清空容器并重新创建内容
            // 这个逻辑与现有代码类似，但更安全
            contentContainer.RemoveAllChildren();

            if (control is SdtBlock)
            {
                var paragraph = new Paragraph(new Run(new Text(newText)
                {
                    Space = SpaceProcessingModeValues.Preserve
                }));
                contentContainer.AppendChild(paragraph);
            }
            else
            {
                var run = new Run(new Text(newText)
                {
                    Space = SpaceProcessingModeValues.Preserve
                });
                contentContainer.AppendChild(run);
            }
        }
    }
}
```

**Step 5: 运行测试验证通过**

```bash
cd C:\WorkSpace\Go2Hell\src\github.com\allanpk716\docx_replacer
dotnet test Tests/DocuFiller.Tests/Services/SafeTextReplacerTests.cs -v n
```

预期: PASS

**Step 6: 提交**

```bash
git add DocuFiller/Services/Interfaces/ISafeTextReplacer.cs DocuFiller/Services/SafeTextReplacer.cs Tests/DocuFiller.Tests/Services/SafeTextReplacerTests.cs
git commit -m "feat: 添加安全文本替换服务"
```

---

### Task 3: 创建格式化内容的表格单元格安全替换服务

**文件:**
- Create: `DocuFiller/Services/Interfaces/ISafeFormattedContentReplacer.cs`
- Create: `DocuFiller/Services/SafeFormattedContentReplacer.cs`
- Test: `Tests/DocuFiller.Tests/Services/SafeFormattedContentReplacerTests.cs`

**Step 1: 编写测试 - 表格单元格中保留富文本格式**

```csharp
// Tests/DocuFiller.Tests/Services/SafeFormattedContentReplacerTests.cs
using System.Collections.Generic;
using DocuFiller.Models;
using DocuFiller.Services;
using Xunit;

namespace DocuFiller.Tests.Services
{
    public class SafeFormattedContentReplacerTests
    {
        [Fact]
        public void ReplaceFormattedContentInTableCell_PreservesStructure()
        {
            // Arrange
            var cell = new DocumentFormat.OpenXml.Wordprocessing.TableCell(
                new DocumentFormat.OpenXml.Wordprocessing.Paragraph(
                    new DocumentFormat.OpenXml.Wordprocessing.SdtRun(
                        new DocumentFormat.OpenXml.Wordprocessing.SdtProperties(
                            new DocumentFormat.OpenXml.Wordprocessing.Tag() { Val = "test" }
                        ),
                        new DocumentFormat.OpenXml.Wordprocessing.SdtContentRun(
                            new DocumentFormat.OpenXml.Wordprocessing.Run(
                                new DocumentFormat.OpenXml.Wordprocessing.Text("old")
                            )
                        )
                    )
                )
            );

            var formattedValue = new FormattedCellValue("H₂O is water");
            formattedValue.Fragments = new List<TextFragment>
            {
                new TextFragment { Text = "H", IsSubscript = false },
                new TextFragment { Text = "2", IsSubscript = true },
                new TextFragment { Text = "O is water", IsSubscript = false }
            };

            // Act
            SafeFormattedContentReplacer.ReplaceFormattedContentInTableCell(
                cell.Descendants<DocumentFormat.OpenXml.Wordprocessing.SdtRun>().First(),
                formattedValue
            );

            // Assert
            // 验证单元格结构未被破坏
            var paragraphs = cell.Elements<DocumentFormat.OpenXml.Wordprocessing.Paragraph>().ToList();
            Assert.Single(paragraphs); // 应该只有一个段落

            // 验证下标格式被保留
            var runs = cell.Descendants<DocumentFormat.OpenXml.Wordprocessing.Run>().ToList();
            Assert.Equal(3, runs.Count); // H, 2 (subscript), O is water
        }
    }
}
```

**Step 2: 运行测试验证失败**

```bash
cd C:\WorkSpace\Go2Hell\src\github.com\allanpk716\docx_replacer
dotnet test Tests/DocuFiller.Tests/Services/SafeFormattedContentReplacerTests.cs -v n
```

预期: FAIL - "SafeFormattedContentReplacer does not exist"

**Step 3: 创建接口和实现**

```csharp
// DocuFiller/Services/Interfaces/ISafeFormattedContentReplacer.cs
using DocumentFormat.OpenXml.Wordprocessing;
using DocuFiller.Models;

namespace DocuFiller.Services.Interfaces
{
    /// <summary>
    /// 安全格式化内容替换服务接口
    /// </summary>
    public interface ISafeFormattedContentReplacer
    {
        /// <summary>
        /// 安全地替换内容控件中的格式化内容，保留表格单元格结构
        /// </summary>
        void ReplaceFormattedContentInTableCell(SdtElement control, FormattedCellValue formattedValue);
    }
}
```

```csharp
// DocuFiller/Services/SafeFormattedContentReplacer.cs
using System.Linq;
using DocumentFormat.OpenXml.Wordprocessing;
using DocuFiller.Models;
using DocuFiller.Services.Interfaces;
using DocuFiller.Utils;
using Microsoft.Extensions.Logging;

namespace DocuFiller.Services
{
    /// <summary>
    /// 安全格式化内容替换服务实现
    /// 专门处理表格单元格中的格式化内容替换，保留单元格结构
    /// </summary>
    public class SafeFormattedContentReplacer : ISafeFormattedContentReplacer
    {
        private readonly ILogger<SafeFormattedContentReplacer> _logger;

        public SafeFormattedContentReplacer(ILogger<SafeFormattedContentReplacer> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void ReplaceFormattedContentInTableCell(SdtElement control, FormattedCellValue formattedValue)
        {
            if (control == null)
                throw new ArgumentNullException(nameof(control));

            if (formattedValue == null)
                throw new ArgumentNullException(nameof(formattedValue));

            _logger.LogDebug($"开始安全替换格式化内容控件文本: '{formattedValue.PlainText}'");

            // 1. 查找内容容器
            var contentContainer = FindContentContainer(control);
            if (contentContainer == null)
            {
                _logger.LogWarning("未找到内容容器，无法替换文本");
                return;
            }

            // 2. 获取所有 Run 元素
            var runs = contentContainer.Descendants<Run>().ToList();

            if (runs.Count == 0)
            {
                _logger.LogWarning("内容容器中没有找到 Run 元素");
                return;
            }

            // 3. 清空所有 Run 的内容
            foreach (var run in runs)
            {
                run.RemoveAllChildren();
            }

            // 4. 在第一个 Run 中添加所有格式化片段
            var firstRun = runs[0];
            foreach (var fragment in formattedValue.Fragments)
            {
                var run = CreateFormattedRun(fragment);
                firstRun.AppendChild(run);
            }

            // 5. 移除其他多余的 Run（保留第一个 Run 的格式属性）
            for (int i = 1; i < runs.Count; i++)
            {
                runs[i].Remove();
            }

            _logger.LogDebug($"格式化内容安全替换: 保留第一个 Run，添加 {formattedValue.Fragments.Count} 个格式化片段");
        }

        private OpenXmlElement? FindContentContainer(SdtElement control)
        {
            var runContent = control.Descendants<SdtContentRun>().FirstOrDefault();
            if (runContent != null) return runContent;

            var blockContent = control.Descendants<SdtContentBlock>().FirstOrDefault();
            if (blockContent != null) return blockContent;

            var cellContent = control.Descendants<SdtContentCell>().FirstOrDefault();
            return cellContent;
        }

        private Run CreateFormattedRun(TextFragment fragment)
        {
            var run = new Run();
            var text = new Text(fragment.Text) { Space = SpaceProcessingModeValues.Preserve };
            run.Append(text);

            if (fragment.IsSuperscript || fragment.IsSubscript)
            {
                var runProperties = new RunProperties();
                runProperties.Append(new VerticalTextAlignment
                {
                    Val = fragment.IsSuperscript
                        ? VerticalPositionValues.Superscript
                        : VerticalPositionValues.Subscript
                });
                run.InsertBefore(runProperties, text);
            }

            return run;
        }
    }
}
```

**Step 4: 运行测试验证通过**

```bash
cd C:\WorkSpace\Go2Hell\src\github.com\allanpk716\docx_replacer
dotnet test Tests/DocuFiller.Tests/Services/SafeFormattedContentReplacerTests.cs -v n
```

预期: PASS

**Step 5: 提交**

```bash
git add DocuFiller/Services/Interfaces/ISafeFormattedContentReplacer.cs DocuFiller/Services/SafeFormattedContentReplacer.cs Tests/DocuFiller.Tests/Services/SafeFormattedContentReplacerTests.cs
git commit -m "feat: 添加安全格式化内容替换服务"
```

---

### Task 4: 修改 ContentControlProcessor 使用安全替换服务

**文件:**
- Modify: `DocuFiller/Services/ContentControlProcessor.cs:206-257`
- Modify: `DocuFiller/Services/ContentControlProcessor.cs:21-24` (构造函数)

**Step 1: 编写集成测试**

```csharp
// Tests/DocuFiller.Tests/Services/ContentControlProcessorIntegrationTests.cs
using System.IO;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocuFiller.Services;
using Xunit;

namespace DocuFiller.Tests.Services
{
    public class ContentControlProcessorIntegrationTests
    {
        [Fact]
        public void ProcessContentControl_InTableCell_PreservesTableStructure()
        {
            // Arrange
            var testFile = Path.Combine("TestData", "table_template.docx");
            var outputFile = Path.Combine("TestData", "table_output.docx");

            // 创建测试文档（包含表格和内容控件）
            CreateTestDocumentWithTableContentControl(testFile);

            var processor = new ContentControlProcessor(
                new Microsoft.Extensions.Logging.Abstractions.NullLogger<ContentControlProcessor>(),
                new CommentManager(new Microsoft.Extensions.Logging.Abstractions.NullLogger<CommentManager>())
            );

            var data = new System.Collections.Generic.Dictionary<string, object>
            {
                { "table_field", "new value in table" }
            };

            // Act
            File.Copy(testFile, outputFile, true);
            using var document = WordprocessingDocument.Open(outputFile, true);
            var control = document.MainDocumentPart.Document.Descendants<SdtRun>()
                .FirstOrDefault(c => c.SdtProperties?.GetFirstChild<Tag>()?.Val?.Value == "table_field");

            processor.ProcessContentControl(control, data, document, Models.ContentControlLocation.Body);
            document.Save();

            // Assert
            // 验证表格结构完整
            var table = document.MainDocumentPart.Document.Descendants<Table>().First();
            Assert.NotNull(table);

            var cell = table.Descendants<TableCell>().First();
            var paragraphs = cell.Elements<Paragraph>().ToList();
            // 单元格应该只有一个段落
            Assert.Single(paragraphs);

            // 验证内容被替换
            var text = cell.Descendants<Text>().First()?.Text;
            Assert.Equal("new value in table", text);
        }

        private void CreateTestDocumentWithTableContentControl(string path)
        {
            // 创建包含表格和内容控件的测试文档
            using var document = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new Document();

            var table = new Table(
                new TableRow(
                    new TableCell(
                        new Paragraph(
                            new SdtRun(
                                new SdtProperties(new Tag() { Val = "table_field" }),
                                new SdtContentRun(new Run(new Text("old value")))
                            )
                        )
                    )
                )
            );

            mainPart.Document.Body = new Body(table);
            mainPart.Document.Save();
        }
    }
}
```

**Step 2: 运行测试验证失败**

```bash
cd C:\WorkSpace\Go2Hell\src\github.com\allanpk716\docx_replacer
dotnet test Tests/DocuFiller.Tests/Services/ContentControlProcessorIntegrationTests.cs -v n
```

预期: FAIL - 当前实现在表格中会创建多个段落

**Step 3: 修改 ContentControlProcessor 构造函数**

```csharp
// DocuFiller/Services/ContentControlProcessor.cs (行 15-24)

private readonly ILogger<ContentControlProcessor> _logger;
private readonly CommentManager _commentManager;
private readonly ISafeTextReplacer _safeTextReplacer;

public ContentControlProcessor(
    ILogger<ContentControlProcessor> logger,
    CommentManager commentManager,
    ISafeTextReplacer safeTextReplacer)
{
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    _commentManager = commentManager ?? throw new ArgumentNullException(nameof(commentManager));
    _safeTextReplacer = safeTextReplacer ?? throw new ArgumentNullException(nameof(safeTextReplacer));
}
```

**Step 4: 修改 ProcessContentReplacement 方法**

```csharp
// DocuFiller/Services/ContentControlProcessor.cs (行 204-219)

/// <summary>
/// 处理内容替换
/// </summary>
private void ProcessContentReplacement(SdtElement control, string value)
{
    // 使用安全文本替换服务
    _safeTextReplacer.ReplaceTextInControl(control, value);

    _logger.LogDebug($"使用安全文本替换服务替换内容: '{value}'");
}
```

**Step 5: 运行测试验证通过**

```bash
cd C:\WorkSpace\Go2Hell\src\github.com\allanpk716\docx_replacer
dotnet test Tests/DocuFiller.Tests/Services/ContentControlProcessorIntegrationTests.cs -v n
```

预期: PASS

**Step 6: 提交**

```bash
git add DocuFiller/Services/ContentControlProcessor.cs Tests/DocuFiller.Tests/Services/ContentControlProcessorIntegrationTests.cs
git commit -m "refactor: ContentControlProcessor 使用安全文本替换服务"
```

---

### Task 5: 修改 DocumentProcessorService 使用安全格式化内容替换服务

**文件:**
- Modify: `DocuFiller/Services/DocumentProcessorService.cs:718-776`
- Modify: `DocuFiller/Services/DocumentProcessorService.cs:36-54` (构造函数)

**Step 1: 编写集成测试**

```csharp
// Tests/DocuFiller.Tests/Services/DocumentProcessorServiceIntegrationTests.cs
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DocuFiller.Models;
using DocuFiller.Services;
using Xunit;

namespace DocuFiller.Tests.Services
{
    public class DocumentProcessorServiceIntegrationTests
    {
        [Fact]
        public async Task ProcessDocumentWithFormattedDataAsync_InTableCell_PreservesTableStructure()
        {
            // Arrange
            var testFile = Path.Combine("TestData", "formatted_table_template.docx");
            var outputFile = Path.Combine("TestData", "formatted_table_output.docx");

            // 创建测试文档
            CreateTestDocumentWithTableContentControl(testFile);

            var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<DocumentProcessorService>();
            var dataParser = new MockDataParser();
            var excelDataParser = new MockExcelDataParser();
            var fileService = new FileService(logger);
            var progressReporter = new ProgressReporter(logger);
            var contentControlProcessor = new ContentControlProcessor(
                new Microsoft.Extensions.Logging.Abstractions.NullLogger<ContentControlProcessor>(),
                new CommentManager(new Microsoft.Extensions.Logging.Abstractions.NullLogger<CommentManager>())
            );
            var commentManager = new CommentManager(new Microsoft.Extensions.Logging.Abstractions.NullLogger<CommentManager>());
            var serviceProvider = new MockServiceProvider();

            var processor = new DocumentProcessorService(
                logger, dataParser, excelDataParser, fileService, progressReporter,
                contentControlProcessor, commentManager, serviceProvider
            );

            var formattedData = new Dictionary<string, FormattedCellValue>
            {
                { "table_field", new FormattedCellValue("H₂O") }
            };
            formattedData["table_field"].Fragments = new List<TextFragment>
            {
                new TextFragment { Text = "H", IsSubscript = false },
                new TextFragment { Text = "2", IsSubscript = true },
                new TextFragment { Text = "O", IsSubscript = false }
            };

            // Act
            var result = await processor.ProcessDocumentWithFormattedDataAsync(
                testFile, formattedData, outputFile
            );

            // Assert
            Assert.True(result.IsSuccess);

            using var document = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Open(outputFile, false);
            var table = document.MainDocumentPart.Document.Descendants<DocumentFormat.OpenXml.Wordprocessing.Table>().First();
            var cell = table.Descendants<DocumentFormat.OpenXml.Wordprocessing.TableCell>().First();

            // 验证单元格结构完整
            var paragraphs = cell.Elements<DocumentFormat.OpenXml.Wordprocessing.Paragraph>().ToList();
            Assert.Single(paragraphs);
        }

        private void CreateTestDocumentWithTableContentControl(string path)
        {
            // 创建测试文档
            using var document = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Create(path, DocumentFormat.OpenXml.WordprocessingDocumentType.Document);
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new DocumentFormat.OpenXml.Wordprocessing.Document();

            var table = new DocumentFormat.OpenXml.Wordprocessing.Table(
                new DocumentFormat.OpenXml.Wordprocessing.TableRow(
                    new DocumentFormat.OpenXml.Wordprocessing.TableCell(
                        new DocumentFormat.OpenXml.Wordprocessing.Paragraph(
                            new DocumentFormat.OpenXml.Wordprocessing.SdtRun(
                                new DocumentFormat.OpenXml.Wordprocessing.SdtProperties(
                                    new DocumentFormat.OpenXml.Wordprocessing.Tag() { Val = "table_field" }
                                ),
                                new DocumentFormat.OpenXml.Wordprocessing.SdtContentRun(
                                    new DocumentFormat.OpenXml.Wordprocessing.Run(new DocumentFormat.OpenXml.Wordprocessing.Text("old"))
                                )
                            )
                        )
                    )
                )
            );

            mainPart.Document.Body = new DocumentFormat.OpenXml.Wordprocessing.Body(table);
            mainPart.Document.Save();
        }
    }
}
```

**Step 2: 运行测试验证失败**

```bash
cd C:\WorkSpace\Go2Hell\src\github.com\allanpk716\docx_replacer
dotnet test Tests/DocuFiller.Tests/Services/DocumentProcessorServiceIntegrationTests.cs -v n
```

预期: FAIL - 当前实现会破坏表格结构

**Step 3: 修改 DocumentProcessorService 构造函数**

```csharp
// DocuFiller/Services/DocumentProcessorService.cs (行 21-54)

private readonly ILogger<DocumentProcessorService> _logger;
private readonly IDataParser _dataParser;
private readonly IExcelDataParser _excelDataParser;
private readonly IFileService _fileService;
private readonly IProgressReporter _progressReporter;
private readonly ContentControlProcessor _contentControlProcessor;
private readonly CommentManager _commentManager;
private readonly IServiceProvider _serviceProvider;
private readonly ISafeFormattedContentReplacer _safeFormattedContentReplacer;
private CancellationTokenSource? _cancellationTokenSource;
private bool _disposed = false;

public DocumentProcessorService(
    ILogger<DocumentProcessorService> logger,
    IDataParser dataParser,
    IExcelDataParser excelDataParser,
    IFileService fileService,
    IProgressReporter progressReporter,
    ContentControlProcessor contentControlProcessor,
    CommentManager commentManager,
    IServiceProvider serviceProvider,
    ISafeFormattedContentReplacer safeFormattedContentReplacer)
{
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    _dataParser = dataParser ?? throw new ArgumentNullException(nameof(dataParser));
    _excelDataParser = excelDataParser ?? throw new ArgumentNullException(nameof(excelDataParser));
    _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
    _progressReporter = progressReporter ?? throw new ArgumentNullException(nameof(progressReporter));
    _contentControlProcessor = contentControlProcessor ?? throw new ArgumentNullException(nameof(contentControlProcessor));
    _commentManager = commentManager ?? throw new ArgumentNullException(nameof(commentManager));
    _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    _safeFormattedContentReplacer = safeFormattedContentReplacer ?? throw new ArgumentNullException(nameof(safeFormattedContentReplacer)));

    _progressReporter.ProgressUpdated += OnProgressUpdated;
}
```

**Step 4: 修改 FillContentControlWithFormattedValue 方法**

```csharp
// DocuFiller/Services/DocumentProcessorService.cs (行 718-776)

/// <summary>
/// 用格式化值填充内容控件（支持富文本）
/// </summary>
private void FillContentControlWithFormattedValue(
    SdtElement control,
    FormattedCellValue formattedValue,
    WordprocessingDocument document,
    ContentControlLocation location = ContentControlLocation.Body)
{
    // 1. 获取控件标签（用于日志）
    string? tag = GetControlTag(control);
    if (string.IsNullOrWhiteSpace(tag))
    {
        _logger.LogWarning("内容控件标签为空，跳过处理");
        return;
    }

    _logger.LogDebug($"开始填充格式化内容控件: {tag}");

    // 2. 检查是否在表格单元格中
    bool isInTableCell = Utils.OpenXmlTableCellHelper.IsInTableCell(control);

    // 3. 记录旧值（用于批注）
    string oldValue = ExtractExistingText(control);

    // 4. 根据位置选择填充策略
    if (isInTableCell)
    {
        _logger.LogDebug("检测到表格单元格内容控件，使用安全填充策略");
        _safeFormattedContentReplacer.ReplaceFormattedContentInTableCell(control, formattedValue);
    }
    else
    {
        _logger.LogDebug("非表格单元格内容控件，使用标准填充策略");
        FillFormattedContentStandard(control, formattedValue);
    }

    // 5. 添加批注（仅正文区域支持，页眉页脚不支持批注）
    if (location == ContentControlLocation.Body)
    {
        AddProcessingComment(document, control, tag, formattedValue.PlainText, oldValue, location);
    }
    else
    {
        _logger.LogDebug($"跳过批注添加(页眉页脚不支持批注功能),标签: '{tag}', 位置: {location}");
    }

    _logger.LogInformation($"✓ 成功填充格式化控件 '{tag}' ({location})");
}

/// <summary>
/// 标准格式化内容填充（非表格单元格）
/// </summary>
private void FillFormattedContentStandard(SdtElement control, FormattedCellValue formattedValue)
{
    // 查找内容容器
    var contentContainer = FindContentContainer(control);
    if (contentContainer == null)
    {
        _logger.LogWarning($"未找到内容容器");
        return;
    }

    // 清空现有内容
    contentContainer.RemoveAllChildren();

    // 根据控件类型创建新内容
    if (control is SdtBlock || contentContainer is SdtContentBlock)
    {
        // 块级控件：创建 Paragraph
        var paragraph = CreateParagraphWithFormattedText(formattedValue);
        contentContainer.AppendChild(paragraph);
    }
    else
    {
        // 行内控件：直接添加 Run
        foreach (var fragment in formattedValue.Fragments)
        {
            var run = CreateFormattedRun(fragment);
            contentContainer.AppendChild(run);
        }
    }
}
```

**Step 5: 运行测试验证通过**

```bash
cd C:\WorkSpace\Go2Hell\src\github.com\allanpk716\docx_replacer
dotnet test Tests/DocuFiller.Tests/Services/DocumentProcessorServiceIntegrationTests.cs -v n
```

预期: PASS

**Step 6: 提交**

```bash
git add DocuFiller/Services/DocumentProcessorService.cs Tests/DocuFiller.Tests/Services/DocumentProcessorServiceIntegrationTests.cs
git commit -m "refactor: DocumentProcessorService 使用安全格式化内容替换服务"
```

---

### Task 6: 注册新服务到 DI 容器

**文件:**
- Modify: `DocuFiller/App.xaml.cs`

**Step 1: 查找服务注册代码**

```bash
cd C:\WorkSpace\Go2Hell\src\github.com\allanpk716\docx_replacer
grep -n "services.AddSingleton" DocuFiller/App.xaml.cs
```

预期输出:
```
行号: services.AddSingleton<IServiceType, ServiceImplementation>();
```

**Step 2: 添加新服务注册**

在 `ConfigureServices` 方法中添加:

```csharp
// 注册安全文本替换服务
services.AddSingleton<ISafeTextReplacer, SafeTextReplacer>();

// 注册安全格式化内容替换服务
services.AddSingleton<ISafeFormattedContentReplacer, SafeFormattedContentReplacer>();
```

**Step 3: 验证编译通过**

```bash
cd C:\WorkSpace\Go2Hell\src\github.com\allanpk716\docx_replacer
dotnet build
```

预期: 编译成功，无错误

**Step 4: 提交**

```bash
git add DocuFiller/App.xaml.cs
git commit -m "feat: 注册安全替换服务到 DI 容器"
```

---

### Task 7: 端到端测试

**文件:**
- Test: `C:\WorkSpace\Go2Hell\src\github.com\allanpk716\docx_replacer\test_data\t1\`

**Step 1: 运行端到端测试**

```bash
cd C:\WorkSpace\Go2Hell\src\github.com\allanpk716\docx_replacer
dotnet run --project DocuFiller -- `
  --template "test_data\t1\IVDR-BH-FD68-CE01 Device Description and Specification including Variants and Accessories.docx" `
  --data "test_data\t1\FD68 IVDR.xlsx" `
  --output "test_data\t1\output"
```

预期输出:
```
✓ 成功处理文档
输出文件: test_data\t1\output\IVDR-BH-FD68-CE01 -- 替换 --YYYY年M月d日HHmmss.docx
```

**Step 2: 验证输出文档**

手动打开输出文档，检查:
1. 章节 1.4.3.2 Instrument 的表格格式是否正常
2. "Brief Product Description" 列中的内容是否正确替换
3. 表格边框、列宽等格式是否保留
4. 是否有内容"跑到下一行"

**Step 3: 如果测试通过，创建回归测试文档**

```markdown
# 表格单元格内容控件回归测试

## 测试场景
- 模板文件: `test_data/t1/IVDR-BH-FD68-CE01 Device Description and Specification including Variants and Accessories.docx`
- 数据文件: `test_data/t1/FD68 IVDR.xlsx`
- 测试章节: 1.4.3.2 Instrument

## 验证点
1. ✓ 表格结构完整（边框、列宽）
2. ✓ 单元格内容正确替换
3. ✓ 没有额外的空段落
4. ✓ 富文本格式保留（上标、下标等）

## 测试日期
[记录测试日期和结果]
```

**Step 4: 提交**

```bash
git add docs/table-cell-regression-test.md
git commit -m "test: 添加表格单元格内容控件回归测试文档"
```

---

## 总结

此实现计划通过以下方式修复表格单元格内容控件格式错乱问题：

1. **检测表格单元格上下文** - 使用 `OpenXmlTableCellHelper` 检测内容控件是否在表格中
2. **安全文本替换** - `SafeTextReplacer` 保留单元格段落结构，只替换文本内容
3. **安全格式化内容替换** - `SafeFormattedContentReplacer` 处理富文本格式同时保留结构
4. **集成到现有服务** - 修改 `ContentControlProcessor` 和 `DocumentProcessorService` 使用新的安全替换服务

**关键原则:**
- 不删除表格单元格的段落结构
- 保留第一个 Run 的格式属性
- 清空内容而不是删除重建
- TDD 方法确保每步都可验证
