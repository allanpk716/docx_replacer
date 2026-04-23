# 页眉页脚内容控件支持

## 功能概述

DocuFiller 现在支持替换 Word 文档中页眉和页脚里的内容控件。

## 使用方法

1. 在 Word 模板的页眉或页脚中插入内容控件
2. 为控件设置标记（Tag）属性
3. 在 JSON 数据文件中提供对应的字段值
4. 运行 DocuFiller 进行批量替换

## 支持的位置

- **文档主体（Body）**: 正文中的内容控件
- **页眉（Header）**: 包括首页、奇数页、偶数页的所有页眉
- **页脚（Footer）**: 包括首页、奇数页、偶数页的所有页脚

## 技术实现

### 核心变更

1. **ContentControlLocation 枚举**
   ```csharp
   public enum ContentControlLocation
   {
       Body,   // 文档主体
       Header, // 页眉
       Footer  // 页脚
   }
   ```

2. **ContentControlData.Location 属性**
   - 标识内容控件所在位置
   - 默认值为 `Body`

3. **ContentControlProcessor.ProcessContentControlsInDocument**
   - 统一处理文档的所有部分
   - 遍历所有 HeaderParts 和 FooterParts
   - 支持取消令牌

### 批注支持

**仅正文区域支持批注，页眉页脚不支持批注功能。**

在 `ContentControlProcessor.ProcessContentControl` 方法中，批注添加逻辑根据控件位置进行区分：

- **正文（Body）**：替换内容后自动添加批注，记录旧值、新值、修改时间和位置信息
- **页眉（Header）/ 页脚（Footer）**：跳过批注添加，仅记录调试日志

批注内容格式：
```
此字段（正文）已于 {时间} 更新。标签：{tag}，旧值：[{oldValue}]，新值：{newValue}
```

**技术原因**：

根据 OOXML 规范（ISO/IEC 29500），Comments 部分只能与 Main Document Part 建立关系，Header 和 Footer 部分无法建立与 Comments 部分的关系。Microsoft Word 在页眉页脚编辑模式下也会禁用批注功能。

**相关代码**（`ContentControlProcessor.cs`）：

```csharp
// 添加批注（仅正文区域支持，页眉页脚不支持批注）
if (location == ContentControlLocation.Body)
{
    AddProcessingComment(document, control, tag, value, oldValue, location);
}
else
{
    _logger.LogDebug($"跳过批注添加（页眉页脚不支持批注功能），标签: '{tag}', 位置: {location}");
}
```

详细的批注功能说明请参阅 [批注功能说明](../批注功能说明.md)。

## 代码示例

### 处理页眉页脚控件

```csharp
// 自动处理文档中的所有内容控件（包括页眉页脚）
_contentControlProcessor.ProcessContentControlsInDocument(
    document,
    data,
    cancellationToken);
```

### 获取所有位置的控件信息

```csharp
var controls = await _documentProcessor.GetContentControlsAsync(templatePath);

foreach (var control in controls)
{
    Console.WriteLine($"{control.Tag} - {control.Location}");
}
// 输出示例:
// HeaderField1 - Header
// BodyField1 - Body
// FooterField1 - Footer
```

## 测试

测试模板和说明位于 `Tests/Templates/` 目录。

运行验证脚本检查测试文件：
```bash
cd Tests
verify-templates.bat
```

## 版本历史

- **2026-01-08**: 初始版本，支持页眉页脚内容控件替换
