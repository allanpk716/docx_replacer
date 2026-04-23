# S03: 功能级文档更新

**Goal:** 更新三份功能级文档，使其与代码实现完全一致：Excel 用户指南增加三列格式说明，页眉页脚文档修正批注行为描述，批注功能说明保持与代码一致。
**Demo:** Excel 用户指南包含三列格式完整说明；页眉页脚和批注功能说明与代码实现一致

## Must-Haves

- docs/excel-data-user-guide.md 包含三列格式（ID|关键词|值）的完整说明、示例和验证规则
- docs/features/header-footer-support.md 中关于批注的描述与代码一致（页眉页脚不添加批注）
- docs/批注功能说明.md 内容与 CommentManager.cs 和 ContentControlProcessor.cs 实现一致
- 三份文档无 TBD/TODO 标记

## Proof Level

- This slice proves: contract

## Integration Closure

- Upstream: reads `ExcelDataParserService.cs`, `ContentControlProcessor.cs`, `CommentManager.cs` for ground truth
- No new wiring — purely documentation updates
- After this slice, all feature-level docs in docs/ are aligned with code

## Verification

- Not provided.

## Tasks

- [x] **T01: 更新 Excel 用户指南增加三列格式说明** `est:30m`
  在 docs/excel-data-user-guide.md 中增加三列 Excel 格式（ID | 关键词 | 值）的完整说明。当前文档仅描述两列格式，但代码 (ExcelDataParserService.cs) 已支持自动检测两列/三列格式。

关键代码行为需反映到文档中：
1. 格式自动检测：读取第一个非空行的第一列，匹配 #xxx# 则两列模式，否则三列模式
2. 三列模式下关键词在 B 列（第2列），值在 C 列（第3列），A 列为 ID
3. 验证规则增加：三列模式下 ID 重复会报错
4. 两列/三列模式的验证规则差异说明
  - Files: `docs/excel-data-user-guide.md`
  - Verify: grep -c '^## ' docs/excel-data-user-guide.md 返回 >= 6（至少6个章节） && grep -qi '三列\|ThreeColumn\|ID.*关键词.*值\|三列格式\|3.*column' docs/excel-data-user-guide.md && ! grep -qi 'TBD\|TODO' docs/excel-data-user-guide.md

- [x] **T02: 修正页眉页脚文档并校验批注功能说明** `est:30m`
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
  - Files: `docs/features/header-footer-support.md`, `docs/批注功能说明.md`
  - Verify: grep -c '^## ' docs/features/header-footer-support.md 返回 >= 4 && grep -qi '页眉页脚.*不支持批注\|header.*footer.*comment.*not supported\|仅正文' docs/features/header-footer-support.md && ! grep -qi 'TBD\|TODO' docs/features/header-footer-support.md && ! grep -qi 'TBD\|TODO' 'docs/批注功能说明.md'

## Files Likely Touched

- docs/excel-data-user-guide.md
- docs/features/header-footer-support.md
- docs/批注功能说明.md
