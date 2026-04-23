# M001-a60bo7: Excel 行 ID 列支持

**Vision:** 在 Excel 数据模板中新增第一列作为行标签（ID），方便人工维护时直观识别每行内容。系统自动检测两列/三列格式，三列模式下校验 ID 唯一性，完全向后兼容旧两列格式。

## Success Criteria

- 三列 Excel（ID | #关键词# | 值）正确解析，ID 列不参与替换
- ID 重复时验证报错，提示具体重复 ID
- 旧两列 Excel 解析和验证行为零变化
- dotnet test 全部通过

## Slices

- [x] **S01: S01** `risk:high` `depends:[]`
  > After this: 三列 Excel 正确解析且 ID 列不影响替换结果；ID 重复时验证报错提示具体重复项；旧两列 Excel 解析和验证行为零变化

- [x] **S02: S02** `risk:low` `depends:[]`
  > After this: dotnet test 全部通过，包含新增的三列解析、格式检测、ID 唯一性校验测试用例

## Boundary Map

### S01 → S02\n\nProduces:\n- ExcelDataParserService 的三列解析能力\n- ExcelFileSummary.DuplicateRowIds 字段\n- DetectExcelFormat 私有方法\n\nConsumes: nothing（第一个切片）
