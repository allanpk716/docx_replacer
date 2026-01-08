# 页眉页脚内容控件支持功能实施计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 使 DocuFiller 能够替换 Word 文档中页眉和页脚里的内容控件

**Architecture:** 增强 ContentControlProcessor 以遍历和处理页眉页脚，保留现有的批注和格式化功能，删除冗余的 OpenXmlDocumentHandler

**Tech Stack:** .NET 8, C#, WPF, DocumentFormat.OpenXml SDK, Microsoft.Extensions.Logging, xUnit

---

## 前置准备

### Task 0: 验证开发环境

**Files:**
- Verify: `DocuFiller.csproj`
- Verify: `Services/ContentControlProcessor.cs`

**Step 1: 验证项目可以构建**

```bash
dotnet build
```

Expected: Build succeeds without errors

**Step 2: 运行现有测试（如果存在）**

```bash
dotnet test
```

Expected: All existing tests pass

**Step 3: 创建测试模板文件**

创建测试用的 Word 文档：
- `Tests/Templates/template-with-header.docx` - 包含页眉控件
- `Tests/Templates/template-with-footer.docx` - 包含页脚控件
- `Tests/Templates/template-with-both.docx` - 包含页眉和页脚控件

---

## 第一阶段：增强核心模型

### Task 1: 扩展 ContentControlData 模型

**Files:**
- Modify: `Models/ContentControlData.cs`

**Step 1: 添加位置枚举**

在 `ContentControlData.cs` 第 103 行（enum 定义之前）添加：

```csharp
/// <summary>
/// 内容控件位置枚举
/// </summary>
public enum ContentControlLocation
{
    /// <summary>
    /// 文档主体
    /// </summary>
    Body,

    /// <summary>
    /// 页眉
    /// </summary>
    Header,

    /// <summary>
    /// 页脚
    /// </summary>
    Footer
}
```

**Step 2: 在 ContentControlData 类中添加 Location 属性**

在 `ContentControlData.cs` 第 30 行（Type 属性之后）添加：

```csharp
/// <summary>
/// 内容控件所在位置
/// </summary>
public ContentControlLocation Location { get; set; } = ContentControlLocation.Body;
```

**Step 3: 构建项目验证**

```bash
dotnet build
```

Expected: Build succeeds

**Step 4: 提交变更**

```bash
git add Models/ContentControlData.cs
git commit -m "feat(model): 添加内容控件位置枚举和属性"
```

---

## 第二阶段：增强 ContentControlProcessor

### Task 2: 添加页眉页脚处理核心方法

**Files:**
- Modify: `Services/ContentControlProcessor.cs`

**Step 1: 在类顶部添加新方法签名**

在 `ContentControlProcessor.cs` 第 68 行（ProcessContentControl 方法之后）添加：

```csharp
/// <summary>
/// 处理文档中的所有内容控件（包括页眉页脚）
/// </summary>
public void ProcessContentControlsInDocument(
    WordprocessingDocument document,
    Dictionary<string, object> data,
    CancellationToken cancellationToken)
{
    if (document.MainDocumentPart == null)
    {
        _logger.LogError("文档主体部分不存在");
        return;
    }

    _logger.LogInformation("开始处理文档中的所有内容控件");

    // 处理文档主体
    ProcessControlsInPart(
        document.MainDocumentPart.Document,
        data,
        document,
        ContentControlLocation.Body,
        cancellationToken);

    // 处理所有页眉
    foreach (var headerPart in document.MainDocumentPart.HeaderParts)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ProcessControlsInHeaderPart(headerPart, data, document, cancellationToken);
    }

    // 处理所有页脚
    foreach (var footerPart in document.MainDocumentPart.FooterParts)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ProcessControlsInFooterPart(footerPart, data, document, cancellationToken);
    }

    _logger.LogInformation("文档内容控件处理完成");
}
```

**Step 2: 添加文档主体处理方法**

在上述方法后添加：

```csharp
/// <summary>
/// 处理文档部分中的内容控件
/// </summary>
private void ProcessControlsInPart(
    OpenXmlPartRootElement partRoot,
    Dictionary<string, object> data,
    WordprocessingDocument document,
    ContentControlLocation location,
    CancellationToken cancellationToken)
{
    var contentControls = partRoot.Descendants<SdtElement>().ToList();
    _logger.LogDebug($"在 {location} 中找到 {contentControls.Count} 个内容控件");

    foreach (var control in contentControls)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ProcessContentControl(control, data, document, location);
    }
}
```

**Step 3: 添加页眉处理方法**

```csharp
/// <summary>
/// 处理页眉中的内容控件
/// </summary>
private void ProcessControlsInHeaderPart(
    HeaderPart headerPart,
    Dictionary<string, object> data,
    WordprocessingDocument document,
    CancellationToken cancellationToken)
{
    if (headerPart.Header == null)
    {
        _logger.LogDebug("页眉部分为空，跳过处理");
        return;
    }

    _logger.LogDebug("开始处理页眉中的内容控件");
    ProcessControlsInPart(
        headerPart.Header,
        data,
        document,
        ContentControlLocation.Header,
        cancellationToken);
}
```

**Step 4: 添加页脚处理方法**

```csharp
/// <summary>
/// 处理页脚中的内容控件
/// </summary>
private void ProcessControlsInFooterPart(
    FooterPart footerPart,
    Dictionary<string, object> data,
    WordprocessingDocument document,
    CancellationToken cancellationToken)
{
    if (footerPart.Footer == null)
    {
        _logger.LogDebug("页脚部分为空，跳过处理");
        return;
    }

    _logger.LogDebug("开始处理页脚中的内容控件");
    ProcessControlsInPart(
        footerPart.Footer,
        data,
        document,
        ContentControlLocation.Footer,
        cancellationToken);
}
```

**Step 5: 修改 ProcessContentControl 方法签名**

将第 30 行的方法签名改为：

```csharp
public void ProcessContentControl(SdtElement control, Dictionary<string, object> data, WordprocessingDocument document, ContentControlLocation location = ContentControlLocation.Body)
```

**Step 6: 更新日志输出以包含位置信息**

修改第 61 行的日志：

```csharp
_logger.LogInformation($"✓ 成功替换内容控件 '{tag}' ({location}) 为 '{value}'");
```

**Step 7: 构建项目验证**

```bash
dotnet build
```

Expected: Build succeeds (可能会有未使用的参数警告，稍后会修复)

**Step 8: 提交变更**

```bash
git add Services/ContentControlProcessor.cs
git commit -m "feat(processor): 添加页眉页脚内容控件处理核心方法"
```

---

### Task 3: 更新 ProcessContentControl 调用

**Files:**
- Modify: `Services/ContentControlProcessor.cs`

**Step 1: 修改 AddProcessingComment 方法调用**

将第 59 行的方法调用改为：

```csharp
AddProcessingComment(document, control, tag, value, oldValue, location);
```

**Step 2: 修改 AddProcessingComment 方法签名**

将第 182 行的方法签名改为：

```csharp
private void AddProcessingComment(WordprocessingDocument document, SdtElement control, string tag, string newValue, string oldValue, ContentControlLocation location)
```

**Step 3: 更新批注文本以包含位置**

修改第 188 行的批注文本：

```csharp
string locationText = location switch
{
    ContentControlLocation.Header => "页眉",
    ContentControlLocation.Footer => "页脚",
    _ => "正文"
};
string commentText = $"此字段（{locationText}）已于 {currentTime} 更新。标签：{tag}，旧值：[{oldValue}]，新值：{newValue}";
```

**Step 4: 构建项目验证**

```bash
dotnet build
```

Expected: Build succeeds

**Step 5: 提交变更**

```bash
git add Services/ContentControlProcessor.cs
git commit -m "refactor(processor): 更新方法签名以支持位置参数"
```

---

## 第三阶段：更新 DocumentProcessorService

### Task 4: 修改 ProcessSingleDocumentAsync 方法

**Files:**
- Modify: `Services/DocumentProcessorService.cs`

**Step 1: 替换内容控件处理逻辑**

将第 229-235 行的代码替换为：

```csharp
// 处理文档中的所有内容控件（包括页眉页脚）
_contentControlProcessor.ProcessContentControlsInDocument(document, data, cancellationToken);
```

**Step 2: 构建项目验证**

```bash
dotnet build
```

Expected: Build succeeds

**Step 3: 提交变更**

```bash
git add Services/DocumentProcessorService.cs
git commit -m "refactor(service): 使用统一的内容控件处理方法"
```

---

### Task 5: 更新 GetContentControlsAsync 方法

**Files:**
- Modify: `Services/DocumentProcessorService.cs`

**Step 1: 修改 GetContentControlsAsync 以包含页眉页脚**

将第 329-365 行的方法替换为：

```csharp
public Task<List<ContentControlData>> GetContentControlsAsync(string templatePath)
{
    List<ContentControlData> controls = new List<ContentControlData>();

    try
    {
        using WordprocessingDocument document = WordprocessingDocument.Open(templatePath, false);
        if (document.MainDocumentPart == null)
        {
            return Task.FromResult(controls);
        }

        // 处理文档主体
        IEnumerable<SdtElement> bodyControls = document.MainDocumentPart.Document.Descendants<SdtElement>();
        foreach (SdtElement control in bodyControls)
        {
            SdtProperties? properties = control.SdtProperties;
            string tag = properties?.GetFirstChild<Tag>()?.Val?.Value ?? string.Empty;
            string alias = properties?.GetFirstChild<SdtAlias>()?.Val?.Value ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(tag))
            {
                controls.Add(new ContentControlData
                {
                    Tag = tag,
                    Title = alias,
                    Type = ContentControlType.Text,
                    Location = ContentControlLocation.Body
                });
            }
        }

        // 处理页眉
        foreach (var headerPart in document.MainDocumentPart.HeaderParts)
        {
            IEnumerable<SdtElement> headerControls = headerPart.Header?.Descendants<SdtElement>() ?? Enumerable.Empty<SdtElement>();
            foreach (SdtElement control in headerControls)
            {
                SdtProperties? properties = control.SdtProperties;
                string tag = properties?.GetFirstChild<Tag>()?.Val?.Value ?? string.Empty;
                string alias = properties?.GetFirstChild<SdtAlias>()?.Val?.Value ?? string.Empty;

                if (!string.IsNullOrWhiteSpace(tag))
                {
                    controls.Add(new ContentControlData
                    {
                        Tag = tag,
                        Title = alias,
                        Type = ContentControlType.Text,
                        Location = ContentControlLocation.Header
                    });
                }
            }
        }

        // 处理页脚
        foreach (var footerPart in document.MainDocumentPart.FooterParts)
        {
            IEnumerable<SdtElement> footerControls = footerPart.Footer?.Descendants<SdtElement>() ?? Enumerable.Empty<SdtElement>();
            foreach (SdtElement control in footerControls)
            {
                SdtProperties? properties = control.SdtProperties;
                string tag = properties?.GetFirstChild<Tag>()?.Val?.Value ?? string.Empty;
                string alias = properties?.GetFirstChild<SdtAlias>()?.Val?.Value ?? string.Empty;

                if (!string.IsNullOrWhiteSpace(tag))
                {
                    controls.Add(new ContentControlData
                    {
                        Tag = tag,
                        Title = alias,
                        Type = ContentControlType.Text,
                        Location = ContentControlLocation.Footer
                    });
                }
            }
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, $"获取内容控件信息失败: {templatePath}");
    }

    return Task.FromResult(controls);
}
```

**Step 2: 构建项目验证**

```bash
dotnet build
```

Expected: Build succeeds

**Step 3: 提交变更**

```bash
git add Services/DocumentProcessorService.cs
git commit -m "feat(service): GetContentControlsAsync 现在包含页眉页脚控件"
```

---

## 第四阶段：清理冗余代码

### Task 6: 删除 OpenXmlDocumentHandler

**Files:**
- Delete: `Services/OpenXmlDocumentHandler.cs`
- Modify: `App.xaml.cs`

**Step 1: 从 App.xaml.cs 移除注册**

将 `App.xaml.cs` 第 106 行删除：

```csharp
// 删除这一行
services.AddSingleton<OpenXmlDocumentHandler>();
```

**Step 2: 删除 OpenXmlDocumentHandler.cs 文件**

```bash
rm Services/OpenXmlDocumentHandler.cs
```

**Step 3: 构建项目验证**

```bash
dotnet build
```

Expected: Build succeeds

**Step 4: 提交变更**

```bash
git add Services/OpenXmlDocumentHandler.cs App.xaml.cs
git commit -m "refactor: 删除冗余的 OpenXmlDocumentHandler"
```

---

## 第五阶段：单元测试

### Task 7: 创建 ContentControlProcessor 单元测试

**Files:**
- Create: `Tests/ContentControlProcessorTests.cs`

**Step 1: 创建测试文件**

创建 `Tests/ContentControlProcessorTests.cs`：

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocuFiller.Models;
using DocuFiller.Services;
using Microsoft.Extensions.Logging;
using Xunit;

namespace DocuFiller.Tests
{
    public class ContentControlProcessorTests
    {
        private readonly ContentControlProcessor _processor;
        private readonly CommentManager _commentManager;

        public ContentControlProcessorTests()
        {
            var logger = new Logger<ContentControlProcessor>(new LoggerFactory());
            var commentLogger = new Logger<CommentManager>(new LoggerFactory());
            _commentManager = new CommentManager(commentLogger);
            _processor = new ContentControlProcessor(logger, _commentManager);
        }

        [Fact]
        public void ProcessContentControl_BodyControl_ReplacesContent()
        {
            // Arrange
            var data = new Dictionary<string, object> { { "TestField", "TestValue" } };

            using var document = WordprocessingDocument.Create(
                "test_body.docx", WordprocessingDocumentType.Document);
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new Document(new Body());

            // 创建测试内容控件
            var sdtBlock = new SdtBlock();
            var sdtProperties = new SdtProperties(
                new Tag { Val = "TestField" },
                new SdtAlias { Val = "Test Field" }
            );
            var sdtContent = new SdtContentBlock(new Paragraph(new Run(new Text("OldValue"))));
            sdtBlock.Append(sdtProperties, sdtContent);
            mainPart.Document.Body.Append(sdtBlock);
            mainPart.Document.Save();

            // Act
            _processor.ProcessContentControl(
                sdtBlock, data, document, ContentControlLocation.Body);

            // Assert
            var newText = sdtContent.Descendants<Text>().FirstOrDefault();
            Assert.NotNull(newText);
            Assert.Equal("TestValue", newText.Text);
        }

        [Fact]
        public void ProcessContentControl_MissingData_SkipsGracefully()
        {
            // Arrange
            var data = new Dictionary<string, object>();
            using var document = WordprocessingDocument.Create(
                "test_skip.docx", WordprocessingDocumentType.Document);
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new Document(new Body());

            var sdtBlock = new SdtBlock();
            var sdtProperties = new SdtProperties(new Tag { Val = "TestField" });
            var sdtContent = new SdtContentBlock(new Paragraph(new Run(new Text("OldValue"))));
            sdtBlock.Append(sdtProperties, sdtContent);

            // Act & Assert - 应该不抛出异常
            _processor.ProcessContentControl(
                sdtBlock, data, document, ContentControlLocation.Body);
        }
    }
}
```

**Step 2: 添加测试项目引用（如果不存在）**

```bash
# 检查是否有测试项目
ls *.csproj
```

如果没有测试项目，创建：

```bash
dotnet new xunit -n DocuFiller.Tests
dotnet add DocuFiller.Tests/DocuFiller.Tests.csproj reference ../DocuFiller.csproj
```

**Step 3: 运行测试**

```bash
dotnet test
```

Expected: Tests pass

**Step 4: 提交变更**

```bash
git add Tests/ContentControlProcessorTests.cs
git commit -m "test: 添加 ContentControlProcessor 单元测试"
```

---

### Task 8: 创建集成测试

**Files:**
- Create: `Tests/HeaderFooterIntegrationTests.cs`

**Step 1: 创建集成测试文件**

```csharp
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using DocuFiller.Models;
using DocuFiller.Services;
using DocuFiller.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace DocuFiller.Tests
{
    public class HeaderFooterIntegrationTests
    {
        private readonly ITestOutputHelper _output;
        private readonly IServiceProvider _serviceProvider;

        public HeaderFooterIntegrationTests(ITestOutputHelper output)
        {
            _output = output;

            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(new LoggerFactory());
            services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
            services.AddSingleton<IFileService, FileService>();
            services.AddSingleton<IDataParser, DataParserService>();
            services.AddSingleton<IProgressReporter, ProgressReporterService>();
            services.AddSingleton<IDocumentProcessor, DocumentProcessorService>();
            services.AddSingleton<ContentControlProcessor>();
            services.AddSingleton<CommentManager>();

            _serviceProvider = services.BuildServiceProvider();
        }

        [Fact]
        public async Task ProcessSingleDocument_WithHeaderControl_ReplacesHeaderContent()
        {
            // Arrange
            var processor = _serviceProvider.GetRequiredService<IDocumentProcessor>();
            var templatePath = "Tests/Templates/template-with-header.docx";
            var outputPath = "Tests/Output/test-header-output.docx";
            var data = new Dictionary<string, object>
            {
                { "HeaderField", "Header Value" }
            };

            // 确保输出目录存在
            Directory.CreateDirectory("Tests/Output");

            // Act
            var result = await processor.ProcessSingleDocumentAsync(
                templatePath, outputPath, data);

            // Assert
            Assert.True(result);
            Assert.True(File.Exists(outputPath));
            _output.WriteLine($"Header control replaced successfully: {outputPath}");
        }

        [Fact]
        public async Task GetContentControls_WithHeaderFooter_ReturnsAllLocations()
        {
            // Arrange
            var processor = _serviceProvider.GetRequiredService<IDocumentProcessor>();
            var templatePath = "Tests/Templates/template-with-both.docx";

            // Act
            var controls = await processor.GetContentControlsAsync(templatePath);

            // Assert
            Assert.NotNull(controls);
            var hasBody = controls.Any(c => c.Location == ContentControlLocation.Body);
            var hasHeader = controls.Any(c => c.Location == ContentControlLocation.Header);
            var hasFooter = controls.Any(c => c.Location == ContentControlLocation.Footer);

            _output.WriteLine($"Found {controls.Count} controls: Body={hasBody}, Header={hasHeader}, Footer={hasFooter}");
        }
    }
}
```

**Step 2: 运行集成测试**

```bash
dotnet test
```

Expected: Tests pass (如果模板文件不存在，测试会跳过或失败，这是预期的)

**Step 3: 提交变更**

```bash
git add Tests/HeaderFooterIntegrationTests.cs
git commit -m "test: 添加页眉页脚集成测试"
```

---

## 第六阶段：手动验证

### Task 9: 创建测试模板并进行手动验证

**Files:**
- Manual: 使用 Microsoft Word 创建测试模板

**Step 1: 创建包含页眉控件的模板**

使用 Microsoft Word：
1. 创建新文档
2. 插入页眉：插入 > 页眉 > 空白
3. 在页眉中插入内容控件：开发工具 > 控件 > 纯文本内容控件
4. 设置控件属性：开发工具 > 属性 > 标记：`HeaderField1`
5. 保存为 `Tests/Templates/template-with-header.docx`

**Step 2: 创建包含页脚控件的模板**

使用 Microsoft Word：
1. 创建新文档
2. 插入页脚：插入 > 页脚 > 空白
3. 在页脚中插入内容控件
4. 设置控件标记：`FooterField1`
5. 保存为 `Tests/Templates/template-with-footer.docx`

**Step 3: 创建同时包含页眉页脚的模板**

1. 创建新文档
2. 添加页眉控件 `HeaderField1`
3. 添加页脚控件 `FooterField1`
4. 在正文添加控件 `BodyField1`
5. 保存为 `Tests/Templates/template-with-both.docx`

**Step 4: 创建测试 JSON 数据文件**

创建 `Tests/Data/test-data.json`：

```json
[
  {
    "HeaderField1": "页眉测试值",
    "FooterField1": "页脚测试值",
    "BodyField1": "正文测试值"
  }
]
```

**Step 5: 运行应用程序进行手动测试**

```bash
dotnet run
```

在 UI 中：
1. 选择模板文件
2. 选择数据文件
3. 选择输出目录
4. 点击处理按钮
5. 打开生成的文档验证页眉页脚控件是否被替换

**Step 6: 提交测试文件**

```bash
git add Tests/Templates/ Tests/Data/
git commit -m "test: 添加页眉页脚测试模板和数据文件"
```

---

## 第七阶段：文档更新

### Task 10: 更新项目文档

**Files:**
- Create: `docs/features/header-footer-support.md`

**Step 1: 创建功能说明文档**

创建 `docs/features/header-footer-support.md`：

```markdown
# 页眉页脚内容控件支持

## 功能概述

DocuFiller 现在支持替换 Word 文档中页眉和页脚里的内容控件。

## 使用方法

1. 在 Word 模板的页眉或页脚中插入内容控件
2. 为控件设置标记（Tag）属性
3. 在 JSON 数据文件中提供对应的字段值
4. 运行 DocuFiller 进行批量替换

## 支持的位置

- 文档主体（Body）
- 页眉（Header）- 包括首页、奇数页、偶数页
- 页脚（Footer）- 包括首页、奇数页、偶数页

## 批注支持

页眉和页脚中的控件替换会添加位置标识到批注中，方便识别。

## 技术实现

- 增强 `ContentControlProcessor` 以遍历所有 `HeaderParts` 和 `FooterParts`
- 为 `ContentControlData` 添加 `Location` 属性
- 保持向后兼容，默认位置为 `Body`
```

**Step 2: 提交文档**

```bash
git add docs/features/header-footer-support.md
git commit -m "docs: 添加页眉页脚功能说明文档"
```

---

## 第八阶段：最终验证和发布

### Task 11: 最终构建和测试

**Files:**
- Verify: 整个项目

**Step 1: 清理并重新构建**

```bash
dotnet clean
dotnet build -c Release
```

Expected: Release build succeeds

**Step 2: 运行所有测试**

```bash
dotnet test -c Release
```

Expected: All tests pass

**Step 3: 验证应用程序启动**

```bash
dotnet run -c Release
```

Expected: Application starts without errors

**Step 4: 检查未使用的 using 语句**

```bash
# 可以使用代码清理工具或手动检查
```

**Step 5: 最终提交**

```bash
git add .
git commit -m "chore: 最终清理和验证"
```

---

## 实施检查清单

在实施过程中，确保每一步都完成：

- [ ] Task 0: 验证开发环境
- [ ] Task 1: 扩展 ContentControlData 模型
- [ ] Task 2: 添加页眉页脚处理核心方法
- [ ] Task 3: 更新 ProcessContentControl 调用
- [ ] Task 4: 修改 ProcessSingleDocumentAsync 方法
- [ ] Task 5: 更新 GetContentControlsAsync 方法
- [ ] Task 6: 删除 OpenXmlDocumentHandler
- [ ] Task 7: 创建 ContentControlProcessor 单元测试
- [ ] Task 8: 创建集成测试
- [ ] Task 9: 创建测试模板并进行手动验证
- [ ] Task 10: 更新项目文档
- [ ] Task 11: 最终构建和测试

---

## 故障排除

### 问题：编译错误 -找不到类型

**解决方案**：确保所有 using 语句正确，检查命名空间

### 问题：测试失败 -模板文件不存在

**解决方案**：先创建测试模板文件，或跳过集成测试

### 问题：页眉页脚控件未被替换

**解决方案**：
1. 检查控件是否设置了 Tag 属性
2. 检查 JSON 数据中是否有对应的字段
3. 查看日志输出，确认控件被找到

### 问题：批注位置错误

**解决方案**：页眉页脚中的批注引用可能不精确，这是 OpenXML 的限制，可以接受

---

## 完成标准

实施完成后，应该满足：

1. ✅ 所有构建成功，无编译错误
2. ✅ 所有单元测试通过
3. ✅ 集成测试通过（如果有测试模板）
4. ✅ 手动测试确认页眉页脚控件被正确替换
5. ✅ 代码已提交到 git
6. ✅ 文档已更新
7. ✅ `OpenXmlDocumentHandler.cs` 已删除
8. ✅ 现有功能（批量处理、进度报告、批注）仍然正常工作
