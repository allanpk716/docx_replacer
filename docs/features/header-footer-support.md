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
