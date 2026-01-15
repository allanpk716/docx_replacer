# XLSX 格式化内容填充修复方案

## 问题概述

当使用 XLSX 文件作为数据源时，文档内容替换没有生效，而使用 JSON 文件时工作正常。

## 根本原因分析

### 当前问题

在 `DocumentProcessorService.ProcessDocumentWithFormattedDataAsync()` 方法中：

1. **使用了不同的代码路径**：
   - JSON: `ProcessSingleDocumentAsync()` → `ContentControlProcessor.ProcessContentControlsInDocument()` ✓ 工作正常
   - XLSX: `ProcessDocumentWithFormattedDataAsync()` → `FillContentControlWithFormattedValue()` ✗ 有 bug

2. **`FillContentControlWithFormattedValue` 方法的具体问题** (第 714-754 行)：

   **问题 1**: 查找逻辑不正确
   ```csharp
   // DocumentProcessorService.cs:783-814
   private SdtElement FindContentControlElement(WordprocessingDocument document, string tag)
   {
       // 每次都重新遍历整个文档
       // 而且已经从 templateControls 获取了控件，却不用
   }
   ```

   **问题 2**: 直接操作 `SdtContent` 导致结构损坏
   ```csharp
   // DocumentProcessorService.cs:732-734
   // 清空现有内容
   sdtContent.RemoveAllChildren();
   // 这可能导致控件结构丢失
   ```

   **问题 3**: 没有复用已验证的 `ContentControlProcessor` 逻辑

### 数据流对比

```
JSON 路径 (工作):
┌─────────────────────────────────────────────────────┐
│ DataParser.ParseJsonFileAsync()                     │
│ → List<Dictionary<string, object>>                  │
└─────────────────┬───────────────────────────────────┘
                  │
┌─────────────────▼───────────────────────────────────┐
│ ProcessSingleDocumentAsync()                        │
│ → ContentControlProcessor.ProcessContentControls()  │
│ → ProcessContentReplacement() ✓                    │
└─────────────────────────────────────────────────────┘

XLSX 路径 (不工作):
┌─────────────────────────────────────────────────────┐
│ ExcelDataParser.ParseExcelFileAsync()                │
│ → Dictionary<string, FormattedCellValue>             │
└─────────────────┬───────────────────────────────────┘
                  │
┌─────────────────▼───────────────────────────────────┐
│ ProcessDocumentWithFormattedDataAsync()             │
│ → FillContentControlWithFormattedValue() ✗         │
│ → 自定义逻辑，有 bug                                 │
└─────────────────────────────────────────────────────┘
```

## 修复方案

### 方案 A: 重写 `FillContentControlWithFormattedValue` 方法

采用模块化设计，参考 `ContentControlProcessor` 的成熟模式，同时支持富文本格式。

#### 1. 修复方法签名和逻辑

```csharp
// 位置: DocumentProcessorService.cs
/// <summary>
/// 用格式化值填充内容控件（支持富文本）
/// </summary>
private void FillContentControlWithFormattedValue(
    SdtElement control,              // 直接传入控件元素，而不是查找
    FormattedCellValue formattedValue,
    WordprocessingDocument document)
{
    // 1. 获取控件标签（用于日志）
    string? tag = GetControlTag(control);
    _logger.LogDebug($"开始填充格式化内容控件: {tag}");

    // 2. 查找内容容器
    var contentContainer = FindContentContainer(control);
    if (contentContainer == null)
    {
        _logger.LogWarning($"控件 {tag} 未找到内容容器");
        return;
    }

    // 3. 清空现有内容
    contentContainer.RemoveAllChildren();

    // 4. 根据控件类型创建新内容
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

    _logger.LogInformation($"✓ 成功填充格式化控件 '{tag}'");
}
```

#### 2. 创建支持格式化的辅助方法

```csharp
/// <summary>
/// 创建包含格式化文本的段落
/// </summary>
private Paragraph CreateParagraphWithFormattedText(FormattedCellValue formattedValue)
{
    var paragraph = new Paragraph();

    foreach (var fragment in formattedValue.Fragments)
    {
        var run = CreateFormattedRun(fragment);
        paragraph.AppendChild(run);
    }

    return paragraph;
}

/// <summary>
/// 创建带格式的 Run 元素（支持上标、下标）
/// </summary>
private Run CreateFormattedRun(TextFragment fragment)
{
    var run = new Run();

    // 创建文本元素
    var text = new Text(fragment.Text) { Space = SpaceProcessingModeValues.Preserve };
    run.Append(text);

    // 添加格式属性
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

/// <summary>
/// 查找内容控件的内容容器
/// </summary>
private OpenXmlElement? FindContentContainer(SdtElement control)
{
    return control.Descendants<SdtContentRun>().FirstOrDefault()
        ?? control.Descendants<SdtContentBlock>().FirstOrDefault()
        ?? control.Descendants<SdtContentCell>().FirstOrDefault();
}

/// <summary>
/// 获取内容控件标签
/// </summary>
private string? GetControlTag(SdtElement control)
{
    return control.SdtProperties?.GetFirstChild<Tag>()?.Val?.Value;
}
```

#### 3. 修改 `ProcessDocumentWithFormattedDataAsync` 方法

```csharp
public async Task<ProcessResult> ProcessDocumentWithFormattedDataAsync(
    string templateFilePath,
    Dictionary<string, FormattedCellValue> formattedData,
    string outputFilePath)
{
    var result = new ProcessResult { IsSuccess = false, StartTime = DateTime.Now };

    try
    {
        _logger.LogInformation($"开始处理文档（格式化数据）: {templateFilePath}");

        // 1. 验证输入
        if (!_fileService.FileExists(templateFilePath))
        {
            result.Errors.Add($"模板文件不存在: {templateFilePath}");
            return result;
        }

        // 2. 复制模板文件
        File.Copy(templateFilePath, outputFilePath, true);

        // 3. 打开文档进行编辑
        using var document = WordprocessingDocument.Open(outputFilePath, true);

        // 4. 获取模板中的所有内容控件（包括页眉页脚）
        var allControls = GetAllContentControls(document);
        _logger.LogInformation($"找到 {allControls.Count} 个内容控件");

        // 5. 填充每个匹配的内容控件
        int filledCount = 0;
        foreach (var controlInfo in allControls)
        {
            if (formattedData.TryGetValue(controlInfo.Tag, out var formattedValue))
            {
                FillContentControlWithFormattedValue(
                    controlInfo.Element,
                    formattedValue,
                    document);
                filledCount++;
            }
            else
            {
                _logger.LogDebug($"未找到控件 '{controlInfo.Tag}' 的数据");
            }
        }

        // 6. 保存并关闭
        document.Save();

        result.IsSuccess = true;
        result.SuccessfulRecords = 1;
        result.GeneratedFiles.Add(outputFilePath);
        result.Message = $"成功填充 {filledCount} 个内容控件";

        _logger.LogInformation($"文档处理完成: {outputFilePath}");
    }
    catch (Exception ex)
    {
        result.Errors.Add($"处理文档失败: {ex.Message}");
        _logger.LogError(ex, $"处理文档失败: {templateFilePath}");
    }
    finally
    {
        result.EndTime = DateTime.Now;
    }

    return result;
}

/// <summary>
/// 获取文档中所有内容控件（包括页眉页脚）
/// </summary>
private List<(SdtElement Element, string Tag, ContentControlLocation Location)> GetAllContentControls(
    WordprocessingDocument document)
{
    var result = new List<(SdtElement, string, ContentControlLocation)>();

    if (document.MainDocumentPart == null)
        return result;

    // 1. 文档主体
    foreach (var control in document.MainDocumentPart.Document.Descendants<SdtElement>())
    {
        string? tag = GetControlTag(control);
        if (!string.IsNullOrWhiteSpace(tag))
        {
            result.Add((control, tag, ContentControlLocation.Body));
        }
    }

    // 2. 页眉
    foreach (var headerPart in document.MainDocumentPart.HeaderParts)
    {
        foreach (var control in headerPart.Header?.Descendants<SdtElement>()
            ?? Enumerable.Empty<SdtElement>())
        {
            string? tag = GetControlTag(control);
            if (!string.IsNullOrWhiteSpace(tag))
            {
                result.Add((control, tag, ContentControlLocation.Header));
            }
        }
    }

    // 3. 页脚
    foreach (var footerPart in document.MainDocumentPart.FooterParts)
    {
        foreach (var control in footerPart.Footer?.Descendants<SdtElement>()
            ?? Enumerable.Empty<SdtElement>())
        {
            string? tag = GetControlTag(control);
            if (!string.IsNullOrWhiteSpace(tag))
            {
                result.Add((control, tag, ContentControlLocation.Footer));
            }
        }
    }

    return result;
}
```

### 修复后的数据流

```
XLSX 路径 (修复后):
┌─────────────────────────────────────────────────────┐
│ ExcelDataParser.ParseExcelFileAsync()                │
│ → Dictionary<string, FormattedCellValue>             │
└─────────────────┬───────────────────────────────────┘
                  │
┌─────────────────▼───────────────────────────────────┐
│ ProcessDocumentWithFormattedDataAsync()             │
│ → GetAllContentControls()                            │
│ → FillContentControlWithFormattedValue() ✓         │
│   - 查找内容容器                                      │
│   - 创建格式化 Run (上标/下标)                        │
│   - 正确填充内容                                      │
└─────────────────────────────────────────────────────┘
```

## 测试策略

### 1. 单元测试

创建 `FormattedContentControlTests.cs`：

```csharp
public class FormattedContentControlTests : IClassFixture<WordTemplateFixture>
{
    private readonly WordTemplateFixture _fixture;
    private readonly DocumentProcessorService _processor;

    public FormattedContentControlTests(WordTemplateFixture fixture)
    {
        _fixture = fixture;
        _processor = fixture.CreateProcessor();
    }

    [Fact]
    public async Task ProcessExcelData_ReplacesContentControls()
    {
        // Arrange
        var excelData = new Dictionary<string, FormattedCellValue>
        {
            ["#产品名称#"] = new FormattedCellValue
            {
                Fragments = { new TextFragment { Text = "测试产品" } }
            },
            ["#型号#"] = new FormattedCellValue
            {
                Fragments = { new TextFragment { Text = "Type-A" } }
            }
        };

        // Act
        var result = await _processor.ProcessDocumentWithFormattedDataAsync(
            _fixture.TemplatePath,
            excelData,
            _fixture.OutputPath);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(File.Exists(_fixture.OutputPath));

        // 验证内容已替换
        using var doc = WordprocessingDocument.Open(_fixture.OutputPath, false);
        var text = doc.MainDocumentPart.Document.InnerText;
        Assert.Contains("测试产品", text);
        Assert.Contains("Type-A", text);
    }

    [Fact]
    public async Task ProcessExcelData_WithSuperscript_AppliesFormatting()
    {
        // Arrange
        var excelData = new Dictionary<string, FormattedCellValue>
        {
            ["#规格#"] = new FormattedCellValue
            {
                Fragments =
                {
                    new TextFragment { Text = "2x10" },
                    new TextFragment
                    {
                        Text = "9",
                        IsSuperscript = true
                    }
                }
            }
        };

        // Act
        var result = await _processor.ProcessDocumentWithFormattedDataAsync(
            _fixture.TemplatePath,
            excelData,
            _fixture.OutputPath);

        // Assert
        Assert.True(result.IsSuccess);

        // 验证上标格式
        using var doc = WordprocessingDocument.Open(_fixture.OutputPath, false);
        var superscripts = doc.MainDocumentPart.Document
            .Descendants<VerticalTextAlignment>()
            .Where(v => v.Val == VerticalPositionValues.Superscript);

        Assert.NotEmpty(superscripts);
    }

    [Fact]
    public async Task ProcessExcelData_WithSubscript_AppliesFormatting()
    {
        // 类似上标测试
    }

    [Fact]
    public async Task ProcessExcelData_HeaderAndFooter_ReplacesAll()
    {
        // 测试页眉页脚中的控件
    }
}
```

### 2. 集成测试

创建 `ExcelToWordIntegrationTests.cs`：

```csharp
public class ExcelToWordIntegrationTests
{
    [Fact]
    public async Task EndToEnd_ExcelFile_ReplacesDocumentContent()
    {
        // 1. 创建 Excel 测试文件
        string excelPath = CreateTestExcelFile();

        // 2. 使用真实的 Word 模板
        string templatePath = "test_data/t1/template.docx";

        // 3. 处理文档
        var service = CreateDocumentProcessor();
        var result = await service.ProcessExcelDataAsync(templatePath, excelPath);

        // 4. 验证结果
        Assert.True(result.IsSuccess);
        Assert.True(File.Exists(result.GeneratedFiles.First()));

        // 5. 验证内容
        VerifyDocumentContent(result.GeneratedFiles.First());
    }
}
```

### 3. 边界测试

```csharp
// 测试空值
[Fact]
public async Task ProcessExcelData_EmptyValue_ClearsControl()

// 测试不存在的控件
[Fact]
public async Task ProcessExcelData_NonExistentControl_SkipsGracefully()

// 测试特殊字符
[Fact]
public async Task ProcessExcelData_SpecialCharacters_HandlesCorrectly()

// 测试多格式片段
[Fact]
public async Task ProcessExcelData_MultipleFragments_CombinesCorrectly()
```

### 4. 对比测试

验证 JSON 和 XLSX 路径产生相同结果：

```csharp
[Fact]
public async Task JsonVsXlsx_SameData_ProducesIdenticalOutput()
{
    // 准备相同的数据
    var jsonData = CreateJsonData();
    var excelData = CreateExcelData(jsonData);

    // 分别处理
    var jsonResult = await ProcessWithJson(jsonData);
    var excelResult = await ProcessWithExcel(excelData);

    // 验证输出相同
    var jsonContent = ExtractText(jsonResult.OutputPath);
    var excelContent = ExtractText(excelResult.OutputPath);

    Assert.Equal(jsonContent, excelContent);
}
```

## 验证计划

### 阶段 1: 单元测试验证

1. 运行所有单元测试
   ```bash
   dotnet test --filter "FullyQualifiedName~FormattedContentControlTests"
   ```

2. 确保测试覆盖率
   - 目标: 新代码覆盖率 > 90%
   - 使用 `dotnet test --collect:"XPlat Code Coverage"`

### 阶段 2: 集成测试

1. 使用真实测试文件
   ```bash
   # 使用 test_data/t1 中的文件
   dotnet run --project DocuFiller.CLI -- \
     --template "test_data/t1/1. IVDR-BH-FN68-CE01 Device Description.docx" \
     --data "test_data/t1/FD68 IVDR.xlsx" \
     --output "test_output"
   ```

2. 验证输出文档
   - 打开生成的 Word 文档
   - 检查所有内容控件已填充
   - 验证格式（上标、下标）正确应用

### 阶段 3: 对比测试

1. 相同数据的 JSON vs XLSX 对比
   ```bash
   # JSON 测试
   dotnet run -- ... --data "FD68 IVDR.json" --output "output_json"

   # XLSX 测试
   dotnet run -- ... --data "FD68 IVDR.xlsx" --output "output_xlsx"

   # 对比输出
   diff output_json/*.docx output_xlsx/*.docx
   ```

2. 自动化对比脚本
   ```csharp
   // 创建对比工具
   public class DocumentComparer
   {
       public static bool AreDocumentsEqual(string path1, string path2)
       {
           // 提取文本进行对比
           // 对比内容控件
           // 对比格式
       }
   }
   ```

### 阶段 4: 手动验证检查清单

- [ ] 所有内容控件都被填充
- [ ] 上标格式正确显示
- [ ] 下标格式正确显示
- [ ] 页眉中的控件被填充
- [ ] 页脚中的控件被填充
- [ ] 特殊字符正确处理（换行符、引号等）
- [ ] 空值不会导致错误
- [ ] 长文本正确显示
- [ ] 中文内容正确显示
- [ ] 生成的文档可以正常打开和编辑

### 阶段 5: 性能测试

```csharp
[Fact]
public async Task Performance_LargeExcel_CompletesInReasonableTime()
{
    // 创建包含 1000 个控件的测试
    var largeData = CreateLargeTestData(1000);

    var stopwatch = Stopwatch.StartNew();
    await _processor.ProcessDocumentWithFormattedDataAsync(
        templatePath, largeData, outputPath);
    stopwatch.Stop();

    Assert.True(stopwatch.ElapsedMilliseconds < 5000);
}
```

## 实施步骤

1. **备份当前代码**
   ```bash
   git checkout -b backup/xlsx-fix-before
   git push origin backup/xlsx-fix-before
   ```

2. **创建功能分支**
   ```bash
   git checkout -b feature/xlsx-formatted-fill-fix
   ```

3. **实施修复**
   - 重写 `FillContentControlWithFormattedValue` 方法
   - 添加辅助方法
   - 修改 `ProcessDocumentWithFormattedDataAsync`

4. **编写测试**
   - 创建 `FormattedContentControlTests.cs`
   - 创建集成测试
   - 实现对比测试

5. **验证修复**
   - 运行单元测试
   - 运行集成测试
   - 手动验证
   - 对比 JSON 和 XLSX 输出

6. **代码审查和合并**
   - 提交 PR
   - 代码审查
   - 合并到主分支

## 回滚计划

如果修复引入新问题：

1. 立即回滚到 `backup/xlsx-fix-before` 分支
2. 临时禁用 XLSX 功能，添加警告：
   ```csharp
   if (dataExtension == ".xlsx")
   {
       _logger.LogWarning("XLSX 功能暂时禁用，请使用 JSON 格式");
       result.AddError("XLSX 功能暂时不可用");
       return result;
   }
   ```
3. 重新分析和修复

## 成功标准

1. ✅ 所有单元测试通过
2. ✅ 集成测试通过
3. ✅ JSON 和 XLSX 产生相同的输出内容
4. ✅ 富文本格式（上标、下标）正确应用
5. ✅ 页眉页脚中的控件正确填充
6. ✅ 没有回归（JSON 路径仍然正常工作）
7. ✅ 性能没有明显下降
8. ✅ 手动验证检查清单全部通过
