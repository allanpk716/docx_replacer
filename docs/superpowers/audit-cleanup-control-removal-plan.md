# 审核清理 -- 关键词控件移除改进方案

> 文档版本: 1.0
> 日期: 2026-06-05
> 范围: 仅限审核清理功能 (Audit Cleanup)

---

## 1. 背景

### 1.1 当前功能概述

DocuFiller 的"审核清理"功能用于去除程序在文档中留下的处理痕迹，使文档看起来如同人工填写。当前功能覆盖两个维度:

- **批注清理** (`CleanupCommentProcessor.cs`): 删除批注 XML 部件 (comments.xml / commentsExtended.xml / people.xml)，恢复被批注标记文本的颜色为黑色，移除所有 CommentRangeStart / CommentRangeEnd / CommentReference 元素。
- **内容控件解包** (`CleanupControlProcessor.cs`): 移除所有 `SdtElement` 包装，保留其内部内容。这是一个"全部解包"策略 -- 无论控件 Tag 值是什么，一律去除 SDT 包装。

### 1.2 改进动机

当前 `CleanupControlProcessor.ProcessControls()` 采用**无差别全量解包**策略: 对文档中找到的每一个 `SdtElement`，无论其 Tag 值是什么，均执行 `UnwrapControl()`。这存在以下问题:

1. **过度清理风险**: 文档中可能存在非 DocuFiller 创建的内容控件 (如 Word 内置的文档部件控件、模板保护控件、表单域等)。这些控件如果被无差别解包，可能破坏文档的模板结构或功能。
2. **清理粒度不足**: 审核清理的目标是去除 DocuFiller 填写过程中产生的痕迹。DocuFiller 使用 `#关键词#` 格式的 Tag (如 `#产品名称#`、`#注册证编号#`) 来标识需要填充的字段。理想情况下，审核清理应仅移除这些由程序创建的关键词控件，保留其他所有内容控件不变。
3. **格式丢失风险**: 当前解包策略虽然保留内容，但对于关键词控件，其 `SdtProperties > rPr` 中可能定义了字符样式 (`rStyle`)。解包后这些样式信息可能不被正确应用到内容 Runs 上，导致格式丢失。

### 1.3 改进目标

将 `CleanupControlProcessor` 的控件处理策略从"全量解包"改为"**定向移除**":

- 仅移除 Tag 匹配 `#关键词#` 模式的控件 (正则: `^#.*#$`)
- 将控件内的格式化内容提取为普通文本 (去除 SDT 包装，保留 Run 级别格式: 字体、字号、加粗、斜体、颜色等)
- 对不匹配 `#关键词#` 模式的控件保持原样不动
- 确保清理后的文档在 Word 中打开时无任何可见差异

---

## 2. 现状分析

### 2.1 当前处理流程

```
用户选择文件 -> CleanupViewModel.StartCleanupAsync()
  -> DocumentCleanupService.CleanupAsync()
    -> CleanupCommentProcessor.ProcessComments()  // 批注清理
    -> CleanupControlProcessor.ProcessControls()  // 控件全量解包 (改进目标)
```

### 2.2 关键代码位置

| 文件 | 路径 | 职责 |
|------|------|------|
| `CleanupControlProcessor.cs` | `DocuFiller/Services/CleanupControlProcessor.cs` | 控件解包逻辑 (203 行) -- **主要修改目标** |
| `DocumentCleanupService.cs` | `DocuFiller/Services/DocumentCleanupService.cs` | 编排服务 (384 行) -- 检查 `hasControls`、调用 `ProcessControls` |
| `IDocumentCleanupService.cs` | `Services/Interfaces/IDocumentCleanupService.cs` | 接口 + `CleanupResult` 模型 |
| `OpenXmlHelper.cs` | `Utils/OpenXmlHelper.cs` | 共享工具方法 (210 行) -- 提供 `GetControlTag`、`FindContentContainer`、`FindAllTargetRuns`、`ApplyControlStyleToRuns` |
| `OpenXmlTableCellHelper.cs` | `DocuFiller/Utils/OpenXmlTableCellHelper.cs` | 表格单元格检测工具 |
| `ContentControlProcessor.cs` | `Services/ContentControlProcessor.cs` | 填充功能中的控件处理器 (211 行) -- 参考实现 |
| `CleanupViewModel.cs` | `DocuFiller/ViewModels/CleanupViewModel.cs` | ViewModel (369 行) |
| `CleanupCommentProcessorTests.cs` | `Tests/DocuFiller.Tests/Services/CleanupCommentProcessorTests.cs` | 现有测试 (7 个用例) |

### 2.3 当前 CleanupControlProcessor 的行为

`ProcessControls()` (第 28-61 行):
- 收集 `partRoot.Descendants<SdtElement>()` 的**全部**控件
- 对每个控件调用 `UnwrapControl()`，不做任何过滤

`UnwrapControl()` (第 96-113 行):
- 检测是否在表格单元格内 (`IsInTableCell`)
- 检测是否包装整个表格单元格 (`containsTableCell`)
- 分三种场景解包

`UnwrapStandard()` (第 155-184 行):
- 找到 `SdtContentRun/SdtContentBlock/SdtContentCell` 内容容器
- 将内容容器的所有子元素移到 `SdtElement` 的父元素
- 删除空的 `SdtElement` 包装

**关键问题**: `ProcessControlsInPart()` (第 69-90 行) 的 `allControls` 列表没有按 Tag 值做任何过滤。

### 2.4 填充功能中的处理方式 (参考)

`ContentControlProcessor.ProcessControlsInPart()` (第 124-149 行) 提供了过滤的参考模式:

```csharp
var taggedControls = allControls
    .Select(c => new { Control = c, Tag = OpenXmlHelper.GetControlTag(c) })
    .Where(x => !string.IsNullOrWhiteSpace(x.Tag))
    .ToList();

var contentControls = taggedControls
    .Where(x => !OpenXmlHelper.HasAncestorWithSameTag(x.Control, x.Tag!))
    .Select(x => x.Control)
    .ToList();
```

该模式使用 `OpenXmlHelper.GetControlTag()` 读取 Tag，并跳过嵌套控件中重复 Tag 的项。

### 2.5 关键词模式

DocuFiller 使用 `#关键词#` 格式的 Tag 来标识需要填充的控件。根据 `ExcelDataParserService.cs` 第 36 行的验证正则 `^#.*#$`:

- 匹配示例: `#产品名称#`、`#注册证编号#`、`#型号#`、`#规格#`
- 不匹配: `Title`、`FormControl1`、`Date` (Word 内置控件)

---

## 3. 需求描述

### 3.1 核心需求

在审核清理功能中，将内容控件的处理策略从"全部解包"改为"仅移除 #关键词# 格式的控件":

1. **识别**: 通过 `SdtProperties > Tag > Val` 读取控件 Tag 值，使用正则 `^#.*#$` 匹配关键词控件
2. **提取**: 对于匹配的控件，提取其内容容器中所有子元素 (保留 Run 级别的完整格式: 字体族、字号、加粗、斜体、下划线、颜色、字符样式等)
3. **替换**: 将 `SdtElement` 替换为提取出的内容 (即在文档 XML 中移除 SDT 包装，将内容提升到父级)
4. **保留**: 对 Tag 不匹配 `^#.*#$` 的控件 (含无 Tag 控件)，不做任何修改

### 3.2 功能边界

- **不在本次范围内**: 修改批注清理逻辑 (已完成，无需变更)
- **不在本次范围内**: 修改 UI 层 (无需变更，用户操作流程不变)
- **不在本次范围内**: 修改 CLI 命令 (`CleanupCommand.cs` 无需变更)
- **不在本次范围内**: 修改 `CleanupResult` 模型 (现有 `ControlsUnwrapped` 计数仍然适用)

### 3.3 期望行为对比

| 场景 | 当前行为 | 改进后行为 |
|------|----------|-----------|
| Tag = `#产品名称#` 的控件 | 全部解包 | 解包 (移除 SDT 包装) |
| Tag = `Title` 的 Word 内置控件 | 全部解包 | **保留不动** |
| 无 Tag 的控件 | 全部解包 | **保留不动** |
| 控件内有嵌套控件 (相同 Tag) | 全部解包 | 跳过内层 (避免重复处理) |
| 控件内有嵌套控件 (不同 Tag) | 全部解包 | 分别判断各层 Tag |

---

## 4. 技术方案

### 4.1 识别 #关键词# 控件

在 `CleanupControlProcessor.ProcessControlsInPart()` 中增加过滤逻辑:

```csharp
private static readonly Regex KeywordTagPattern = new(@"^#.*#$", RegexOptions.Compiled);

private int ProcessControlsInPart(OpenXmlPartRootElement partRoot, string location)
{
    int count = 0;
    var allControls = partRoot.Descendants<SdtElement>().ToList();

    // 过滤: 仅处理 Tag 匹配 #关键词# 模式的控件
    var keywordControls = allControls
        .Where(c =>
        {
            string? tag = OpenXmlHelper.GetControlTag(c);
            if (string.IsNullOrWhiteSpace(tag)) return false;
            return KeywordTagPattern.IsMatch(tag.Trim());
        })
        .Where(c => !HasAncestorWithSameTag(c))  // 跳过嵌套重复 Tag
        .ToList();

    _logger.LogDebug($"在 {location} 中找到 {allControls.Count} 个控件，其中 {keywordControls.Count} 个为关键词控件");

    foreach (var control in keywordControls)
    {
        try
        {
            UnwrapControl(control);
            count++;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"解包关键词控件时发生异常 ({location}): {ex.Message}");
        }
    }

    return count;
}
```

**关键决策点**:

- 使用 `OpenXmlHelper.GetControlTag(c)` (位于 `Utils/OpenXmlHelper.cs` 第 22-25 行) 读取 Tag 值
- 对无 Tag 的控件 (Word 内置控件通常没有 Tag 或 Tag 为空) 返回 `null`，被 `string.IsNullOrWhiteSpace` 过滤掉
- 复用 `OpenXmlHelper.HasAncestorWithSameTag()` (第 112-119 行) 避免嵌套控件重复处理

### 4.2 内容提取与格式保留

当前的 `UnwrapStandard()` 和 `UnwrapWrappedTableCell()` 方法已经实现了"将内容容器子元素提升到父级"的逻辑。由于它们直接移动 XML 元素而非重建，**所有 Run 级别的格式 (RunProperties) 会自动保留**:

- `RunProperties` 中的 `Bold`、`Italic`、`Underline`、`FontSize`、`FontFamily`、`Color`、`RunStyle` 等属性随 Run 元素一起移动
- `ParagraphProperties` 中的对齐、缩进、行距等属性随 Paragraph 元素一起移动
- 不需要额外克隆或重建格式

因此，**解包逻辑本身无需修改**。改动仅限于过滤逻辑 -- 决定"哪些控件需要解包"。

### 4.3 SDT 字符样式的处理

部分关键词控件的 `SdtProperties > rPr` 中定义了字符样式引用 (`rStyle`)，例如 `<w:rStyle w:val="样式1 Char"/>`。填充功能通过 `OpenXmlHelper.ApplyControlStyleToRuns()` 将此样式应用到内容 Runs 上。

在审核清理中，有两种处理策略:

**策略 A: 不处理 rStyle (推荐)**
- 如果文档已经过 DocuFiller 填充阶段，`ApplyControlStyleToRuns()` 已经在填充时将 `rStyle` 应用到 Runs 上了
- 此时 SDT 的 `sdtPr/rPr` 中的 rStyle 已经冗余 (Runs 自身已携带该样式)
- 直接解包不影响格式
- 这是**最简单、最安全**的方案

**策略 B: 解包前确认 rStyle 已应用**
- 解包前读取 `SdtProperties > rPr > RunStyle` 的值
- 检查内容容器中的 Runs 是否已有相同的 `RunStyle`
- 如果没有，则调用 `OpenXmlHelper.ApplyControlStyleToRuns()` 补充应用
- 适用于文档未经填充直接提交审核清理的边缘场景

**推荐策略 A**，因为审核清理的正常使用流程是: 填充 -> 审核 -> 清理。如果用户跳过填充直接清理，格式问题应属于用户误操作，不需要在此处理。

### 4.4 边缘情况处理

#### 4.4.1 嵌套 SDT 控件

当关键词控件内部嵌套了其他控件时:

```
外层: SdtBlock (Tag="#产品名称#")
  内层: SdtRun (Tag="SubField")
```

处理策略:
- 外层控件匹配 `^#.*#$`，被选为解包目标
- 使用 `HasAncestorWithSameTag()` 跳过与祖先同 Tag 的内层控件
- 内层控件 Tag 不匹配 `^#.*#$`，不在处理范围内
- 解包外层控件后，内层控件的内容随外层内容一起被提升到外层控件的父级
- 内层控件本身保留在提升后的内容中 (因为内层没有被选中解包)

如果内层控件的 Tag 也匹配 `^#.*#$` 但与外层不同:

```
外层: SdtBlock (Tag="#产品名称#")
  内层: SdtRun (Tag="#子字段#")
```

- `HasAncestorWithSameTag` 对内层返回 `false` (因为 Tag 不同)
- 内层也被选中解包
- 但由于外层先被解包，内层已不存在于外层中 (外层已整体移除)
- 实际效果: 两个控件都被移除，内容被正确提升

**潜在问题**: 如果外层和内层都被选中，遍历顺序可能导致异常。当前代码使用 `ToList()` 快照，外层解包后内层可能已脱离文档树。`UnwrapControl()` 需要增加防御性检查:

```csharp
private void UnwrapControl(SdtElement sdtElement)
{
    // 防御: 如果控件已脱离文档树，跳过
    if (sdtElement.Parent == null)
    {
        _logger.LogDebug("控件已脱离文档树，跳过解包");
        return;
    }

    // ... 原有逻辑
}
```

#### 4.4.2 空内容控件

控件的内容容器可能为空 (无子元素)。此时 `UnwrapStandard()` 中的 `content.ChildElements.ToList()` 为空列表，循环不执行，仅删除空的 SDT 包装。这是正确的行为 -- 空的关键词控件应被移除。

#### 4.4.3 多段落块级控件

`SdtBlock > SdtContentBlock > Paragraph + Paragraph + ...`

解包后多个 Paragraph 被提升到父级。当前 `UnwrapStandard()` 已正确处理此场景 (遍历所有 `content.ChildElements` 并逐个 `InsertBefore`)。

#### 4.4.4 表格单元格中的控件

三种场景已在当前代码中覆盖:
1. **控件在表格单元格内**: `isInTableCell=true, containsTableCell=false` -> `UnwrapStandard`
2. **控件包装整个表格单元格**: `isInTableCell=false, containsTableCell=true` -> `UnwrapWrappedTableCell`
3. **控件在表格单元格内且包含子单元格**: `isInTableCell=true, containsTableCell=true` -> 按当前逻辑进入 `UnwrapWrappedTableCell` (因为 `containsTableCell && !isInTableCell` 为 false，走 else 分支)

**场景 3 需要确认**: 如果一个关键词控件位于表格单元格内部，且该控件内部也包含 TableCell，当前代码会走 `UnwrapStandard()`。这应该是正确的 -- 因为控件本身在单元格内，不应该把内部的子单元格提升到控件外 (否则会破坏外层表格结构)。

#### 4.4.5 SdtContentRun vs SdtContentBlock vs SdtContentCell

当前 `FindContentContainer()` (第 192-202 行) 通过 `Descendants()` 查找内容容器，兼容所有三种类型。无需修改。

---

## 5. 实现计划

### Task 0: 准备工作

**文件**: `DocuFiller/Services/CleanupControlProcessor.cs`
**工作量**: 极小

- 在类顶部添加 `using System.Text.RegularExpressions;`
- 添加静态只读正则字段: `private static readonly Regex KeywordTagPattern = new(@"^#.*#$", RegexOptions.Compiled);`
- 添加 `HasAncestorWithSameTag` 辅助方法 (从 `OpenXmlHelper.HasAncestorWithSameTag` 复制或直接调用静态方法)

**决策**: 优先直接调用 `OpenXmlHelper.HasAncestorWithSameTag(control, tag)`，避免代码重复。需要先通过 `GetControlTag` 获取 tag 值，因此过滤逻辑中自然会拥有 tag。

### Task 1: 修改 ProcessControlsInPart 过滤逻辑

**文件**: `DocuFiller/Services/CleanupControlProcessor.cs`，第 69-90 行
**工作量**: 小

修改 `ProcessControlsInPart` 方法:

1. 将 `var allControls = partRoot.Descendants<SdtElement>().ToList();` 保持不变 (用于日志计数)
2. 新增过滤步骤:
   ```csharp
   var keywordControls = allControls
       .Where(c => IsKeywordControl(c))
       .ToList();
   ```
3. 在日志中分别输出总控件数和关键词控件数
4. 将 `foreach` 循环的目标从 `allControls` 改为 `keywordControls`

新增私有方法 `IsKeywordControl`:

```csharp
/// <summary>
/// 判断控件是否为关键词控件 (Tag 匹配 #关键词# 模式)
/// </summary>
private bool IsKeywordControl(SdtElement control)
{
    string? tag = OpenXmlHelper.GetControlTag(control);
    if (string.IsNullOrWhiteSpace(tag))
        return false;

    if (!KeywordTagPattern.IsMatch(tag.Trim()))
        return false;

    // 跳过嵌套的同 Tag 控件 (避免重复处理)
    if (OpenXmlHelper.HasAncestorWithSameTag(control, tag))
        return false;

    return true;
}
```

### Task 2: 在 UnwrapControl 中增加防御性检查

**文件**: `DocuFiller/Services/CleanupControlProcessor.cs`，第 96 行
**工作量**: 极小

在 `UnwrapControl` 方法开头添加:

```csharp
// 防御: 如果控件已脱离文档树 (可能被祖先控件解包时连带移除)，跳过
if (sdtElement.Parent == null)
{
    _logger.LogDebug("控件已脱离文档树，跳过解包");
    return;
}
```

同时在 `UnwrapStandard()` 第 155 行和 `UnwrapWrappedTableCell()` 第 120 行中也有 `parent == null` 检查，这些已存在无需修改。

### Task 3: 更新日志信息

**文件**: `DocuFiller/Services/CleanupControlProcessor.cs`
**工作量**: 极小

- `ProcessControls()` 第 59 行: 修改日志消息为 `$"共解包 {controlsUnwrapped} 个关键词内容控件"`
- `ProcessControlsInPart()`: 修改日志消息为 `"在 {location} 中找到 {allControls.Count} 个内容控件，其中 {keywordControls.Count} 个为关键词控件"`
- `UnwrapControl()` 第 101 行: 可选添加 Tag 值到日志中 `$"解包关键词控件 [{tag}] - 类型: ..."`

### Task 4: 编写单元测试

**文件**: `Tests/DocuFiller.Tests/Services/CleanupControlProcessorTests.cs` (新建)
**工作量**: 中

测试用例清单:

| # | 测试名 | 验证点 |
|---|--------|--------|
| 1 | `ProcessControls_OnlyUnwrapsKeywordTagControls` | Tag 为 `#产品名称#` 的控件被解包，Tag 为 `Title` 的控件保留 |
| 2 | `ProcessControls_PreservesControlsWithoutTag` | 无 Tag 的控件不被解包 |
| 3 | `ProcessControls_PreservesFormattingAfterUnwrap` | 解包后内容 Run 的 `RunProperties` (Bold/Color/FontSize) 保持不变 |
| 4 | `ProcessControls_HandlesSdtBlock` | 块级控件 (`SdtBlock`) 正确解包 |
| 5 | `ProcessControls_HandlesSdtRun` | 行内控件 (`SdtRun`) 正确解包 |
| 6 | `ProcessControls_HandlesWrappedTableCell` | 包装表格单元格的控件 (`SdtCell`) 正确解包 |
| 7 | `ProcessControls_HandlesNestedControls` | 嵌套控件 (同 Tag 内层被跳过，不同 Tag 内层被单独处理) |
| 8 | `ProcessControls_HandlesEmptyControl` | 空内容控件被安全移除 |
| 9 | `ProcessControls_HandlesMultiParagraphBlock` | 多段落块级控件正确解包 |
| 10 | `ProcessControls_PreservesHeaderControls` | 页眉中不匹配的控件被保留 |
| 11 | `ProcessControls_PreservesFooterControls` | 页脚中不匹配的控件被保留 |
| 12 | `ProcessControls_NoKeywordControls_ReturnsZero` | 文档无关键词控件时返回 0 |
| 13 | `ProcessControls_SkipsDetachedControl` | 已脱离文档树的控件被安全跳过 |

测试辅助方法:

```csharp
// 创建包含指定类型控件的测试文档
private string CreateDocxWithControls(
    Action<WordprocessingDocument>? customize = null)

// 创建包含 #关键词# 控件和普通控件的混合文档
private string CreateDocxWithMixedControls()

// 在指定位置添加 SdtBlock
private SdtBlock CreateSdtBlockWithTag(string tag, params OpenXmlElement[] contentChildren)

// 在指定位置添加 SdtRun
private SdtRun CreateSdtRunWithTag(string tag, params OpenXmlElement[] contentChildren)
```

### Task 5: 验证 DocumentCleanupService 的行为

**文件**: `DocuFiller/Services/DocumentCleanupService.cs`，第 72-73 行
**工作量**: 极小 (可能无需修改)

当前代码:
```csharp
bool hasComments = document.MainDocumentPart.WordprocessingCommentsPart != null;
bool hasControls = document.MainDocumentPart.Document.Descendants<SdtElement>().Any();
```

**分析**:
- `hasControls` 检查所有控件 (不过滤)。如果文档中只有非关键词控件 (无 `#xxx#` Tag)，`hasControls` 为 `true`，会调用 `ProcessControls()`。
- `ProcessControls()` 在改进后如果找不到任何关键词控件，会返回 0。
- `CleanupResult.ControlsUnwrapped` 为 0，消息中显示"解包 0 个控件"。
- `CleanupResult.Success` 仍为 `true` (因为有 `hasControls` 分支执行)。

**决策**: 这是可接受的行为。文档可能同时含有批注和非关键词控件，即使控件解包数为 0，批注仍然被正确清理。无需修改 `DocumentCleanupService`。

如果希望更精确 (完全无关键词控件时跳过控件处理分支)，可以将 `hasControls` 改为检查是否存在关键词控件。但这会增加一次遍历开销，且当前行为已经正确，因此**不推荐修改**。

### Task 6: 集成测试验证

**手动验证步骤**:

1. 使用 DocuFiller 填充一份包含 `#关键词#` 控件的模板
2. 对填充后的文档执行审核清理
3. 用 Word 打开清理后的文档，验证:
   - 原本 `#产品名称#` 位置显示普通文本 (无控件边框/底纹)
   - 文本格式与填充后一致 (字体、字号、颜色无变化)
   - 文档中没有残留的批注标记
4. 对仅含非关键词控件的文档执行审核清理，验证控件未被移除

---

## 6. 测试方案

### 6.1 单元测试

参见 Task 4，共 13 个测试用例，覆盖:
- Tag 过滤 (匹配/不匹配/空 Tag)
- 控件类型 (SdtBlock/SdtRun/SdtCell)
- 表格单元格场景 (在单元格内/包装单元格)
- 嵌套控件
- 空控件
- 多段落控件
- 页眉/页脚
- 边缘情况 (脱离文档树、无关键词控件)

### 6.2 集成验证

使用实际的 docx 测试文件 (填充功能生成的文档):

| # | 测试场景 | 预期结果 |
|---|---------|---------|
| 1 | 仅含 `#关键词#` 控件的文档 | 所有控件被移除，内容保留，格式不变 |
| 2 | 含 `#关键词#` 控件 + Word 内置控件 | 仅关键词控件被移除，内置控件保留 |
| 3 | 含批注 + `#关键词#` 控件 | 批注被清除，关键词控件被移除 |
| 4 | 纯批注文档 (无控件) | 批注被清除，控件处理数为 0 |
| 5 | 纯内置控件文档 (无关键词 Tag) | 控件不被移除，结果消息"解包 0 个控件" |
| 6 | 嵌套关键词控件的文档 | 外层被移除，内容提升；内层按 Tag 判断 |
| 7 | 表格中的关键词控件 | 控件被正确解包，表格结构完整 |

### 6.3 回归验证

确保改动不影响现有的批注清理功能:
- 运行现有 `CleanupCommentProcessorTests` 的全部 7 个测试用例，确保全部通过
- 使用 2026-06-03 修复周期中的 44 个测试文件验证批注清理功能不受影响

---

## 7. 风险评估

### 7.1 技术风险

| 风险 | 等级 | 说明 | 缓解措施 |
|------|------|------|---------|
| 过滤正则过于严格 | 低 | 正则 `^#.*#$` 与 `ExcelDataParserService.cs` 中使用的完全一致，且填充功能已验证此模式能正确匹配所有关键词控件 | 复用已验证的正则表达式 |
| 嵌套控件遍历顺序问题 | 中 | 外层控件解包后，内层控件可能脱离文档树，访问其 `Parent` 会返回 null | Task 2 中增加的防御性检查 (Parent == null 时跳过) |
| 非关键词控件被误判 | 低 | `GetControlTag()` 读取 `SdtProperties > Tag > Val`，对于无 Tag 属性的控件返回 null，被 `IsNullOrWhiteSpace` 正确过滤 | 充分的单元测试 (Task 4) |
| 格式丢失 | 低 | 当前解包逻辑直接移动 XML 元素，RunProperties 随 Run 一起移动，格式自动保留 | 格式保留测试 (Task 4 测试 #3) |

### 7.2 业务风险

| 风险 | 等级 | 说明 | 缓解措施 |
|------|------|------|---------|
| 用户体验变化 | 低 | 之前审核清理会移除所有控件，现在只移除关键词控件。如果用户期望全部移除，可能觉得清理"不彻底" | 考虑在 UI 中增加选项 (可选: 未来增加"移除所有控件"的开关) |
| 已有清理行为被改变 | 中 | 已有的 44 个测试文件如果包含非关键词控件，之前会被移除，改进后将保留 | 需要确认测试文件的控件类型。如果有非关键词控件，清理结果会有预期差异 |

### 7.3 兼容性风险

| 风险 | 等级 | 说明 | 缓解措施 |
|------|------|------|---------|
| DocuFiller 生成的文档中含非关键词控件 | 低 | DocuFiller 的 `ContentControlProcessor` 在填充时只处理有 Tag 的控件，不会创建无 Tag 控件。因此 DocuFiller 生成的文档中所有控件都应有 Tag | 无需特殊处理 |
| 第三方工具创建的控件 | 低 | 这些控件的 Tag 通常不是 `#关键词#` 格式，会被正确保留 | 正则过滤确保只有特定格式被处理 |

---

## 附录: 修改文件清单

| 文件 | 操作 | 改动规模 |
|------|------|---------|
| `DocuFiller/Services/CleanupControlProcessor.cs` | 修改 | 约 30 行新增，10 行修改 |
| `Tests/DocuFiller.Tests/Services/CleanupControlProcessorTests.cs` | 新建 | 约 350 行 |
| `DocuFiller/Services/DocumentCleanupService.cs` | 不修改 | -- |
| `Utils/OpenXmlHelper.cs` | 不修改 | -- |
| `MainWindow.xaml` | 不修改 | -- |
| `MainWindow.xaml.cs` | 不修改 | -- |
| `CleanupViewModel.cs` | 不修改 | -- |

总改动量预估: **约 380 行** (其中 350 行为测试代码)。
