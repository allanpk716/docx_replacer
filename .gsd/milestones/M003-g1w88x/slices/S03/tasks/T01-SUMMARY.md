---
id: T01
parent: S03
milestone: M003-g1w88x
key_files:
  - docs/excel-data-user-guide.md
key_decisions:
  - 文档中将三列格式的 ID 重复验证规则从通用验证规则中拆分为独立小节，使两列/三列差异更清晰
duration: 
verification_result: passed
completed_at: 2026-04-23T10:41:52.216Z
blocker_discovered: false
---

# T01: 更新 Excel 用户指南，增加三列格式（ID | 关键词 | 值）的完整说明和验证规则差异

**更新 Excel 用户指南，增加三列格式（ID | 关键词 | 值）的完整说明和验证规则差异**

## What Happened

在 docs/excel-data-user-guide.md 中新增了三列 Excel 格式的完整文档。具体改动包括：

1. **新增"格式自动检测"章节**：说明程序如何通过读取第一个非空行 A 列内容自动判断两列/三列模式（匹配 #xxx# 为两列，否则为三列）。

2. **新增"三列格式"说明**：包含表格示例（A=ID, B=关键词, C=值），各列用途说明，以及 ID 不能为 #xxx# 格式以避免误判的注意事项。

3. **拆分验证规则章节**：分为通用规则、三列额外规则（ID 重复报错）、以及两列/三列验证差异对比表。

4. **新增常见问题**：如何选择两列/三列格式、三列 ID 列的作用。

5. **更新概述**：增加支持两种格式和自动检测的说明。

所有代码行为（DetectExcelFormat 检测逻辑、三列下列索引映射、DuplicateRowIds 验证）均准确反映到文档中。

## Verification

运行了三个验证检查：
1. grep -c '^## ' 返回 6（≥6 个章节，符合要求）
2. grep -ci '三列\|ThreeColumn\|ID.*关键词.*值\|三列格式\|3.*column' 返回 11（包含三列相关关键词）
3. grep -ci 'TBD\|TODO' 返回 0（无 TBD/TODO 标记）

所有验证条件均通过。

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `grep -c '^## ' docs/excel-data-user-guide.md` | 0 | ✅ pass | 500ms |
| 2 | `grep -ci '三列\|ThreeColumn\|ID.*关键词.*值\|三列格式\|3.*column' docs/excel-data-user-guide.md` | 0 | ✅ pass (11 matches) | 500ms |
| 3 | `grep -ci 'TBD\|TODO' docs/excel-data-user-guide.md` | 1 | ✅ pass (0 matches) | 500ms |

## Deviations

None.

## Known Issues

None.

## Files Created/Modified

- `docs/excel-data-user-guide.md`
