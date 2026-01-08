# Excel 到 Word 格式保留验证报告

## 验证日期
2025-01-08

## 验证目标
验证从 Excel 读取带格式（特别是上标/下标）的文本并写入 Word 文档时，格式是否能正确保留。

## 测试场景

### 场景 1：Unicode 上标/下标字符
**测试文件**: `FormattedTextTest.xlsx`

**测试内容**:
- 2x10⁹ (使用 Unicode 上标字符 U+2079)
- H₂O (使用 Unicode 下标字符 U+2082)
- 粗体、斜体、下划线等基本格式

**验证结果**: ✅ 成功
- Unicode 字符被正确读取和保留
- 不需要特殊的格式处理
- 生成的 Word 文档: `FormattedTextOutput_20250108_184102.docx`

### 场景 2：Excel 格式化上标/下标
**测试文件**: `FormattedSuperscriptTest.xlsx`

**测试内容**:
1. 2x10⁹ - 使用 Excel 上标格式
2. H₂O - 使用 Excel 下标格式
3. x² - 平方（上标）
4. y³ - 立方（上标）
5. E = mc² - 粗体 + 上标
6. H₂SO₄ - 多个下标

**验证结果**: ✅ 成功
- 成功识别 Excel 的上标/下标格式
- 正确转换为 Word 的垂直对齐格式
- 粗体等其他格式也正确保留
- 生成的 Word 文档: `FormattedSuperscriptOutput_20250108_195516.docx`

## 技术实现

### Excel 读取
使用 **EPPlus 7.5.2** 库：
- 通过 `cell.IsRichText` 检测富文本
- 通过 `ExcelRichText.VerticalAlign` 识别上标/下标
- 上标: `ExcelVerticalAlignmentFont.Superscript`
- 下标: `ExcelVerticalAlignmentFont.Subscript`

### Word 写入
使用 **DocumentFormat.OpenXml 3.0.1** 库：
- 通过 `RunProperties` 设置文本运行属性
- 上标: `VerticalTextAlignment` = `VerticalPositionValues.Superscript`
- 下标: `VerticalTextAlignment` = `VerticalPositionValues.Subscript`
- 粗体: `Bold` 元素
- 斜体: `Italic` 元素
- 下划线: `Underline` 元素

## 关键发现

### 1. 两种上标表示方式的区别

**Unicode 字符方式**（如 ⁹、²、³）:
- 本质是特殊字符，不是格式
- 在 Excel 中显示为上标，但实际是字符本身
- 读取时不需要特殊处理
- 优点：简单，兼容性好
- 缺点：只支持有限的字符（0-9 的部分上标）

**Excel 格式化方式**（设置字符为上标）:
- 使用 Excel 的格式化功能
- 字符本身是普通数字（如 "9"），通过格式显示为上标
- 读取时需要检测 `VerticalAlign` 属性
- 写入 Word 时需要设置 `VerticalTextAlignment`
- 优点：灵活，可以设置任何字符为上标/下标
- 缺点：需要特殊的格式处理代码

### 2. EPPlus API 变化
EPPlus 7.x 版本中，`ExcelRichText` 的 API 发生了变化：
- 不再直接提供 `Underline` 属性
- 需要通过其他方式获取某些格式信息
- 富文本格式需要逐个 `ExcelRichText` 项处理

### 3. 文本运行（Run）的概念
Excel 富文本和 Word 文档都使用"文本运行"的概念：
- 一个文本运行包含一段具有相同格式的文本
- "2x10⁹" 在格式化方式下包含 2 个运行：
  - Run 1: "2x10" (普通文本)
  - Run 2: "9" (上标格式)
- 读取和写入时需要保持运行的结构

## 验证结论

✅ **功能验证成功**

验证程序成功实现了：
1. 从 Excel 读取格式化上标/下标
2. 正确识别各种文本格式（粗体、斜体、下划线、上标、下标）
3. 将格式正确应用到 Word 文档
4. 处理 Unicode 字符和 Excel 格式化两种情况

## 项目结构

```
ExcelToWordVerifier/
├── Models/
│   ├── FormattedText.cs       # 带格式文本模型
│   └── TextRun.cs             # 单个文本运行模型
├── Services/
│   ├── IExcelReader.cs        # Excel 读取接口
│   ├── ExcelReaderService.cs  # EPPlus 实现
│   ├── IWordWriter.cs         # Word 写入接口
│   └── WordWriterService.cs   # OpenXML 实现
├── TestFiles/
│   ├── FormattedTextTest.xlsx              # Unicode 字符测试
│   └── FormattedSuperscriptTest.xlsx       # 格式化上标测试
├── Output/
│   ├── FormattedTextOutput_*.docx          # Unicode 字符输出
│   └── FormattedSuperscriptOutput_*.docx   # 格式化上标输出
├── Program.cs                        # 主验证程序
├── TestFormattedSuperscript.cs       # 格式化上标验证程序
└── ExcelToWordVerifier.csproj        # 项目文件

Tools/
└── ExcelFormattedTestGenerator/      # 测试文件生成工具
    ├── Program.cs
    └── ExcelFormattedTestGenerator.csproj
```

## 使用说明

### 运行主验证程序（Unicode 字符）
```bash
cd ExcelToWordVerifier
dotnet run
```

### 运行格式化上标验证程序
```bash
cd ExcelToWordVerifier
dotnet run --project TestFormattedSuperscript.csproj
```

### 生成新的格式化测试文件
```bash
cd Tools/ExcelFormattedTestGenerator
dotnet run
```

## 后续建议

1. **集成到 DocuFiller 项目**：可以将此功能集成到主项目中，作为 Excel 数据源的支持

2. **扩展格式支持**：
   - 颜色格式
   - 字体大小
   - 字体类型

3. **错误处理优化**：
   - 处理不支持的上标/下标格式
   - 提供更友好的错误信息

4. **性能优化**：
   - 批量读取时的性能优化
   - 大文件处理优化

## 总结

本次验证成功证明了从 Excel 读取格式化文本（特别是上标/下标）并写入 Word 文档的可行性。验证程序展示了完整的技术实现方案，包括：
- 使用 EPPlus 读取 Excel 格式化文本
- 使用 OpenXML SDK 写入 Word 文档
- 正确处理文本运行和格式属性

这为后续在 DocuFiller 项目中集成 Excel 数据源提供了技术基础。
