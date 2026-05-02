---
estimated_steps: 14
estimated_files: 2
skills_used: []
---

# T02: 修正页眉页脚文档并校验批注功能说明

校验并更新两份功能文档，确保与代码实现一致。

**docs/features/header-footer-support.md 需修正：**
当前文档中 '批注支持' 部分声称页眉页脚控件替换会添加批注，但实际代码 (ContentControlProcessor.cs ProcessContentControl 方法) 中仅 Body 位置添加批注，Header/Footer 位置跳过批注（_logger.LogDebug "跳过批注添加"）。需要修正此部分描述。

具体代码行为：
- `if (location == ContentControlLocation.Body)` → 添加批注
- else → 跳过批注，仅记录调试日志
- 批注格式：`此字段（{locationText}）已于 {currentTime} 更新。标签：{tag}，旧值：[{oldValue}]，新值：{newValue}`
- locationText: Body=正文, Header=页眉, Footer=页脚

**docs/批注功能说明.md 需校验：**
当前文档已正确说明页眉页脚不支持批注（与代码一致），需核实其余描述是否准确：
- CommentManager 的 AddCommentToElement 和 AddCommentToRunRange 方法行为
- 批注 ID 全局唯一性管理
- 批注存储在 MainDocumentPart.WordprocessingCommentsPart
- 批注内容格式与代码一致

## Inputs

- `Services/ContentControlProcessor.cs`
- `Services/CommentManager.cs`
- `Models/ContentControlData.cs`
- `docs/features/header-footer-support.md`
- `docs/批注功能说明.md`

## Expected Output

- `docs/features/header-footer-support.md`
- `docs/批注功能说明.md`

## Verification

grep -c '^## ' docs/features/header-footer-support.md 返回 >= 4 && grep -qi '页眉页脚.*不支持批注\|header.*footer.*comment.*not supported\|仅正文' docs/features/header-footer-support.md && ! grep -qi 'TBD\|TODO' docs/features/header-footer-support.md && ! grep -qi 'TBD\|TODO' 'docs/批注功能说明.md'
