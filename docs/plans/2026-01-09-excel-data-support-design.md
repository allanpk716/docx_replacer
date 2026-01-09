# Excel 数据支持设计文档

**日期**: 2026-01-09
**状态**: 设计阶段
**作者**: Claude & User

## 背景

当前 DocuFiller 使用 JSON 文件作为数据源进行关键词替换。JSON 格式无法表达富文本格式（如上标、下标），导致数学表达式（如 `2x10^9`）在 Word 文档中无法正确显示为上标格式。

经过验证，Excel 文件可以保留富文本格式，并且复制到 Word 时可以正确传递格式信息。因此需要将 DocuFiller 的数据输入从 JSON 扩展到支持 Excel 格式。

## 目标

1. 支持 Excel (.xlsx) 文件作为数据输入源
2. 保留 Excel 单元格中的上标、下标格式信息
3. 提供 JSON 到 Excel 的转换工具，方便迁移现有数据
4. 严格验证 Excel 文件格式，确保数据质量
5. 替换内容继承 Word 内容控件的字体大小和基础格式，只额外应用上标/下标

## 整体架构

### 新增组件

1. **IExcelDataParser 接口及实现**
   - `ExcelDataParserService`: 使用 EPPlus 读取 Excel 文件
   - 解析两列结构（A列=关键词，B列=值）
   - 提取每个单元格的富文本运行（Runs），识别上标/下标
   - 返回带格式信息的数据结构

2. **格式化数据模型**
   - `FormattedCellValue`: 表示带格式的单元格值
   - `TextFragment`: 表示单个文本片段及格式
   - 支持上标、下标标记

3. **ExcelToWordConverterService**
   - 独立的转换服务，将 JSON 文件转换为 Excel 文件
   - 读取现有的 JSON 格式（包括 keywords 数组结构）
   - 生成标准的两列 Excel 文件

4. **ConverterWindow**
   - 独立的转换工具窗口
   - 支持选择单个或批量转换 JSON 文件
   - 显示转换进度和结果

### 架构影响

- `IDataParser` 接口保持不变（向后兼容 JSON）
- 新增 `IExcelDataParser` 接口专门处理 Excel
- `IDocumentProcessor` 扩展支持接收 `FormattedCellValue` 数据
- 主窗口文件选择器同时支持 .json 和 .xlsx 文件

## 数据模型设计

### FormattedCellValue

```csharp
public class FormattedCellValue
{
    /// <summary>
    /// 纯文本内容（用于验证和显示）
    /// </summary>
    public string PlainText { get; set; } = string.Empty;

    /// <summary>
    /// 文本片段列表，每个片段包含内容和格式信息
    /// </summary>
    public List<TextFragment> Fragments { get; set; } = new();
}

public class TextFragment
{
    /// <summary>
    /// 文本内容
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// 是否为上标
    /// </summary>
    public bool IsSuperscript { get; set; }

    /// <summary>
    /// 是否为下标
    /// </summary>
    public bool IsSubscript { get; set; }
}
```

### 数据流转换

1. **Excel 读取**:
   - EPPlus 读取单元格的 `RichText` 集合
   - 每个 `ExcelRichText` 对应一个 `TextFragment`
   - 提取 `VerticalAlign` 属性判断上标/下标

2. **Word 填充**:
   - `DocumentProcessor` 接收 `Dictionary<string, FormattedCellValue>`
   - 对于每个内容控件，清空现有内容
   - 遍历 `Fragments`，为每个片段创建对应的 `Run` 元素
   - 只设置上标/下标属性，其他格式继承自内容控件样式

3. **兼容性处理**:
   - 如果单元格没有富文本，创建单个 Fragment（纯文本，无格式）
   - JSON 路径：`Dictionary<string, object>` 转换为 `Dictionary<string, FormattedCellValue>`

## Excel 文件格式规范

### 文件结构

| A列 (关键词)   | B列 (值)      |
|---------------|---------------|
| #产品名称#     | D-二聚体...   |
| #结构及组成#   | xxx           |
| #产品型号#     | 产1品2型3号4  |

### 格式要求

1. **基本结构**:
   - 第一列：关键词（符合 `#开头#结尾` 格式）
   - 第二列：对应的值（支持富文本格式）
   - 单个工作表，程序读取第一个工作表
   - 每行一个键值对

2. **关键词格式**:
   - 必须以 `#` 开头和结尾
   - 正则验证：`^#.*#$`
   - 不能为空
   - 不能重复

3. **值列要求**:
   - 可以为空（产生警告）
   - 支持富文本（上标、下标）
   - 富文本通过 Excel 的格式设置功能创建

## Excel 文件验证

### 验证规则（严格模式）

1. **文件级验证**:
   - 文件扩展名必须是 `.xlsx`
   - 文件存在且可读
   - 至少包含一个工作表

2. **结构验证**:
   - 第一列必须存在（A列）
   - 第二列必须存在（B列）
   - 至少有一行数据

3. **关键词格式验证**:
   - 第一列的值必须符合 `#开头#` 结尾格式
   - 收集所有不符合格式的关键词

4. **数据完整性验证**:
   - 检查重复关键词
   - 检查第二列是否为空（警告，非错误）
   - 统计有效行数

### ValidationResult 扩展

```csharp
public class ExcelValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public ExcelFileSummary Summary { get; set; } = new();
}

public class ExcelFileSummary
{
    public int TotalRows { get; set; }
    public int ValidKeywordRows { get; set; }
    public List<string> DuplicateKeywords { get; set; } = new();
    public List<string> InvalidFormatKeywords { get; set; } = new();
}
```

## JSON 转 Excel 转换器

### 转换逻辑

**输入 JSON 结构**:
```json
{
  "project_name": "凝血项目",
  "keywords": [
    { "key": "#产品名称#", "value": "D-二聚体测定试剂盒（胶乳免疫比浊法）" },
    { "key": "#结构及组成#", "value": "adsasdadsa" },
    { "key": "#产品型号#", "value": "产1品2型3号4" }
  ]
}
```

**输出 Excel 结构**:
| A列         | B列                                   |
|-------------|---------------------------------------|
| #产品名称#   | D-二聚体测定试剂盒（胶乳免疫比浊法）   |
| #结构及组成# | adsasdadsa                            |
| #产品型号#   | 产1品2型3号4                          |

### 功能特性

1. **输入支持**:
   - 支持现有的 JSON 格式
   - 单文件转换和批量转换
   - 拖拽支持

2. **输出选项**:
   - 默认保存在原 JSON 文件同目录
   - 文件名：`原文件名.xlsx`
   - 如果文件已存在，提示覆盖

3. **纯文本转换**:
   - JSON 中的值作为纯文本写入 Excel
   - 不尝试自动添加格式
   - 用户可以在生成的 Excel 中手动添加需要的格式

### ConverterWindow UI

```
┌─────────────────────────────────────┐
│ JSON 转 Excel 转换工具              │
├─────────────────────────────────────┤
│                                     │
│  源文件: [________________]  [浏览]  │
│                                     │
│  [拖拽 JSON 文件到这里]             │
│                                     │
│  待转换文件列表:                    │
│  ┌───────────────────────────────┐ │
│  │ ☑ 1.json                      │ │
│  │ ☑ 2.json                      │ │
│  │ ☐ 3.json                      │ │
│  └───────────────────────────────┘ │
│                                     │
│  输出目录: [________________]  [浏览] │
│                                     │
│  [开始转换]  [清空列表]             │
│                                     │
│  进度: 正在转换 1.json...           │
│                                     │
└─────────────────────────────────────┘
```

## 主界面集成

### 文件选择器更新

```csharp
var dialog = new OpenFileDialog
{
    Title = "选择数据文件",
    Filter = "支持的数据文件|*.xlsx;*.json|Excel文件 (*.xlsx)|*.xlsx|JSON文件 (*.json)|*.json"
};
```

### 文件类型自动识别

- 根据 `DataPath` 的扩展名自动选择解析器
- `.xlsx` → `IExcelDataParser`
- `.json` → `IDataParser`

### 数据预览增强

- 如果是 Excel，显示富文本预览
- 上标用 `<sup>` 标记显示
- 下标用 `<sub>` 标记显示
- 示例：`2x10<sup>9</sup>`

### 菜单栏新增

```
文件  工具  帮助
        └─ 转换工具
            └─ JSON 转 Excel 转换器...
```

## 用户操作流程

### 新用户（使用 Excel）

1. 选择 Word 模板
2. 选择 Excel 数据文件
3. 系统自动验证并显示预览
4. 点击"开始处理"
5. 生成带格式的 Word 文档

### 老用户（有旧 JSON）

1. 点击菜单：工具 → 转换工具 → JSON 转 Excel 转换器
2. 选择 JSON 文件 → 转换为 Excel
3. 在 Excel 中手动添加需要的格式（如上标、下标）
4. 返回主界面 → 选择新生成的 Excel 文件
5. 继续正常处理流程

### 错误提示示例

```
❌ Excel 文件验证失败：

• 第 5 行：关键词 "#产品名称" 格式错误（必须以 # 开头和结尾）
• 第 8 行：关键词 "#型号#" 重复（首次出现在第 3 行）
• 第 12 行：值列为空
```

## 技术依赖

- **EPPlus**: 用于读取和写入 Excel 文件
- **OpenXML SDK**: 用于操作 Word 文档（已存在）
- **现有 JSON 处理**: Newtonsoft.Json（已存在）

## 实施计划

### 阶段一：核心数据模型和解析
1. 创建 `FormattedCellValue` 和 `TextFragment` 模型
2. 实现 `IExcelDataParser` 接口和 `ExcelDataParserService`
3. 添加 Excel 文件验证逻辑

### 阶段二：文档处理扩展
1. 扩展 `IDocumentProcessor` 支持格式化值
2. 实现 Word 内容控件的富文本填充
3. 处理上标、下标格式应用

### 阶段三：转换工具
1. 实现 `ExcelToWordConverterService`
2. 创建 `ConverterWindow` UI
3. 添加批量转换支持

### 阶段四：主界面集成
1. 更新文件选择器
2. 添加文件类型自动识别
3. 增强数据预览显示
4. 添加菜单项

### 阶段五：测试和优化
1. 单元测试
2. 集成测试
3. 性能优化
4. 用户文档编写

## 注意事项

1. **向后兼容**: 保持对 JSON 文件的完整支持
2. **格式继承**: 确保替换内容继承内容控件的字体大小、颜色等基础格式
3. **性能考虑**: 大文件的处理性能优化
4. **错误处理**: 提供清晰的错误提示和解决方案
5. **扩展性**: 为将来支持更多格式（粗体、斜体等）预留扩展点

## 参考资料

- 现有 `ExcelToWordVerifier` 项目的实现
- EPPlus 文档
- OpenXML SDK 文档
- JSON 示例文件: `test_data/1.json`
