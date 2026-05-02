---
estimated_steps: 6
estimated_files: 1
skills_used: []
---

# T01: 更新 Excel 用户指南增加三列格式说明

在 docs/excel-data-user-guide.md 中增加三列 Excel 格式（ID | 关键词 | 值）的完整说明。当前文档仅描述两列格式，但代码 (ExcelDataParserService.cs) 已支持自动检测两列/三列格式。

关键代码行为需反映到文档中：
1. 格式自动检测：读取第一个非空行的第一列，匹配 #xxx# 则两列模式，否则三列模式
2. 三列模式下关键词在 B 列（第2列），值在 C 列（第3列），A 列为 ID
3. 验证规则增加：三列模式下 ID 重复会报错
4. 两列/三列模式的验证规则差异说明

## Inputs

- `Services/ExcelDataParserService.cs`
- `Models/ExcelFileSummary.cs`
- `Models/ExcelValidationResult.cs`
- `docs/excel-data-user-guide.md`

## Expected Output

- `docs/excel-data-user-guide.md`

## Verification

grep -c '^## ' docs/excel-data-user-guide.md 返回 >= 6（至少6个章节） && grep -qi '三列\|ThreeColumn\|ID.*关键词.*值\|三列格式\|3.*column' docs/excel-data-user-guide.md && ! grep -qi 'TBD\|TODO' docs/excel-data-user-guide.md
