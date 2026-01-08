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

**架构说明**：
根据 OpenXML SDK 的限制，`WordprocessingCommentsPart` 只能作为 `MainDocumentPart` 的子部分，不能添加到 `HeaderPart` 或 `FooterPart`。因此，所有批注定义（Comment 元素）都存储在主文档的批注部分中，但批注引用（CommentRangeStart、CommentRangeEnd、CommentReference）可以正确添加到页眉页脚的 Run 元素上。这确保了：
- 批注在页眉页脚中仍然可见
- 批注 ID 在整个文档中保持唯一
- 符合 OpenXML 标准规范

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
