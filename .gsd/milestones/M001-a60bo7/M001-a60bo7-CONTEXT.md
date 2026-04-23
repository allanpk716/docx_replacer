# M001-a60bo7: Excel 行 ID 列支持

**Gathered:** 2026-04-23
**Status:** Ready for planning

## Project Description

在现有 Excel 数据模板中新增第一列作为行标签（ID），方便人工维护 Excel 表格时直观识别每行内容。系统自动检测新旧格式，新增 ID 唯一性校验，完全向后兼容。

## Why This Milestone

用户维护大型 Excel 数据表时，只有 `#关键词#` 列不容易直观看出每行是什么内容。加一个人工可读的 ID 标签，降低维护成本和出错概率。

## User-Visible Outcome

### When this milestone is complete, the user can:

- 在 Excel 模板中使用三列格式（ID | #关键词# | 值），ID 列为任意唯一字符串
- 收到 ID 重复的明确错误提示
- 继续使用旧的两列格式，零变化

### Entry point / environment

- Entry point: DocuFiller WPF 应用，选择 Excel 数据文件
- Environment: Windows 桌面

## Completion Class

- Contract complete means: 解析和验证逻辑正确处理两种格式，单元测试覆盖
- Integration complete means: 现有集成测试通过，无回归
- Operational complete means: none（纯解析层改动）

## Final Integrated Acceptance

To call this milestone complete, we must prove:

- 三列 Excel 正确解析，ID 列不参与替换
- ID 重复时验证报错
- 旧两列 Excel 解析结果与改动前完全一致
- `dotnet test` 全部通过

## Architectural Decisions

### 格式自动检测策略

**Decision:** 读取第一个非空行的第一列内容，匹配 `#xxx#` 格式则为两列模式，否则为三列模式

**Rationale:** 最小侵入性检测，利用已有且稳定的关键词格式约定。用户明确认可此方案。

**Alternatives Considered:**
- 表头行标记 — 增加模板复杂度，旧模板无表头
- 配置项切换 — 用户需要额外操作，体验差

### 检测逻辑封装位置

**Decision:** 在 `ExcelDataParserService` 内部新增私有方法 `DetectExcelFormat`，不修改接口签名

**Rationale:** 格式检测是解析内部实现细节，不影响调用方。保持 `IExcelDataParser` 接口稳定。

**Alternatives Considered:**
- 新建独立检测服务 — 过度设计，检测逻辑简单

## Error Handling Strategy

- 三列模式下 ID 重复 → `ExcelValidationResult.Errors` 中添加具体重复 ID 列表
- 三列模式下某行 ID 为空 → 跳过该行（与现有空行逻辑一致，不报错）
- 两列模式 → 行为完全不变，无新增错误路径
- 格式检测遇到空文件/空表 → 走现有错误路径

## Risks and Unknowns

- 边界情况：第一行第一列恰好不是关键词格式但用户意图是两列模式 — 风险低，`#xxx#` 格式约定性强

## Existing Codebase / Prior Art

- `Services/ExcelDataParserService.cs` — 核心改动点：`ParseExcelFileAsync`、`ValidateExcelFileAsync`
- `Models/ExcelFileSummary.cs` — 新增 `DuplicateRowIds` 字段
- `Services/Interfaces/IExcelDataParser.cs` — 不修改
- `Tests/ExcelDataParserServiceTests.cs` — 新增测试用例
- `Tests/ExcelIntegrationTests.cs` — 确保无回归

## Relevant Requirements

- R001 — Excel 三列格式解析
- R002 — 行 ID 唯一性校验
- R003 — 向后兼容两列格式
- R004 — 现有测试全部通过

## Scope

### In Scope

- ExcelDataParserService 解析和验证逻辑
- ExcelFileSummary 模型扩展
- 新增测试覆盖

### Out of Scope / Non-Goals

- UI 展示 ID 信息
- ExcelToWordConverterService（JSON→Excel 转换）不改动
- JSON 数据解析不改动
- GetDataPreviewAsync / GetDataStatisticsAsync 中展示 ID

## Technical Constraints

- 不修改 IExcelDataParser 接口签名
- 不破坏现有两列 Excel 的解析结果
- EPPlus LicenseContext 已设置为 NonCommercial

## Integration Points

- `DocumentProcessorService` 通过 `IExcelDataParser` 消费解析结果 — 接口不变，无影响
- `MainWindowViewModel` 通过 `IExcelDataParser` 获取验证结果 — 验证结果结构扩展但向后兼容

## Testing Requirements

- 现有 `ExcelDataParserServiceTests` 全部通过
- 现有 `ExcelIntegrationTests` 全部通过
- 新增：三列格式解析测试（正常解析、ID 列被跳过）
- 新增：格式自动检测测试（两列 vs 三列）
- 新增：ID 唯一性校验测试（重复报错、无重复通过）

## Acceptance Criteria

### S01
- 三列 Excel 解析结果与等效两列 Excel 完全一致（ID 列不影响输出）
- 格式检测准确区分两列和三列
- ID 重复时 `ExcelValidationResult.IsValid == false` 且 Errors 包含重复 ID
- 旧两列 Excel 解析和验证行为零变化

### S02
- `dotnet test` 全部通过
- 新增测试覆盖三列解析、格式检测、ID 唯一性三个场景

## Open Questions

_None._
