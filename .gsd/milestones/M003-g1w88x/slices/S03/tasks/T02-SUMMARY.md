---
id: T02
parent: S03
milestone: M003-g1w88x
key_files:
  - docs/features/header-footer-support.md
  - docs/批注功能说明.md
key_decisions:
  - (none)
duration: 
verification_result: passed
completed_at: 2026-04-23T10:44:18.424Z
blocker_discovered: false
---

# T02: 修正页眉页脚文档中批注行为描述，仅正文区域支持批注；确认批注功能说明文档与代码一致

**修正页眉页脚文档中批注行为描述，仅正文区域支持批注；确认批注功能说明文档与代码一致**

## What Happened

修正了 `docs/features/header-footer-support.md` 中"批注支持"章节的错误描述。原文档声称页眉页脚控件替换会添加批注（带有位置标识），并详细描述了批注存储架构。实际代码（ContentControlProcessor.cs ProcessContentControl 方法）中，仅 `location == ContentControlLocation.Body` 时添加批注，Header/Footer 位置直接跳过并记录调试日志。

修正内容：
- 删除了错误的"所有位置都添加批注"描述
- 删除了错误的"批注在页眉页脚中仍然可见"声明
- 删除了错误的"批注 ID 全局管理"描述（实际页眉页脚根本不产生批注）
- 新增准确的"仅正文区域支持批注"说明，与代码行为完全一致
- 添加了关键代码片段和技术原因（OOXML 规范限制）
- 添加了到批注功能说明文档的交叉引用

同时校验了 `docs/批注功能说明.md`，确认其所有描述与代码一致：
- CommentManager 的 AddCommentToElement/AddCommentToRunRange 方法描述准确
- 批注 ID 全局唯一性（GenerateCommentId 扫描现有批注取最大值+1）描述准确
- 批注存储在 WordprocessingCommentsPart 描述准确
- 批注格式与代码一致
- 页眉页脚不支持批注的说明准确
- 无需修改

## Verification

运行四项验证检查，全部通过：
1. header-footer-support.md 包含 7 个章节（≥4 要求） ✅
2. header-footer-support.md 包含"仅正文"关键词（批注行为修正） ✅
3. header-footer-support.md 无 TBD/TODO 标记 ✅
4. 批注功能说明.md 无 TBD/TODO 标记 ✅

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `powershell -Command "(Select-String -Path 'docs/features/header-footer-support.md' -Pattern '^## ' | Measure-Object).Count"` | 0 | ✅ pass (7 sections >= 4) | 800ms |
| 2 | `powershell -Command "Select-String -Path 'docs/features/header-footer-support.md' -Pattern '页眉页脚.*不支持批注|仅正文' -Quiet"` | 0 | ✅ pass (True) | 600ms |
| 3 | `powershell -Command "Select-String -Path 'docs/features/header-footer-support.md' -Pattern 'TBD|TODO' -Quiet"` | 0 | ✅ pass (False, no TBD/TODO) | 600ms |
| 4 | `powershell -Command "Select-String -Path 'docs/批注功能说明.md' -Pattern 'TBD|TODO' -Quiet"` | 0 | ✅ pass (False, no TBD/TODO) | 600ms |

## Deviations

None.

## Known Issues

None.

## Files Created/Modified

- `docs/features/header-footer-support.md`
- `docs/批注功能说明.md`
