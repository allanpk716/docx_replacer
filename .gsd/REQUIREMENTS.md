# Requirements

This file is the explicit capability and coverage contract for the project.

## Validated

### R001 — Excel 解析服务自动检测两列（关键词|值）或三列（ID|关键词|值）格式，三列模式下跳过第1列，读取第2列为关键词、第3列为值
- Class: core-capability
- Status: validated
- Description: Excel 解析服务自动检测两列（关键词|值）或三列（ID|关键词|值）格式，三列模式下跳过第1列，读取第2列为关键词、第3列为值
- Why it matters: 允许用户在 Excel 中为每行添加人类可读的标签，方便维护大型数据表
- Source: user
- Primary owning slice: M001-a60bo7/S01
- Supporting slices: none
- Validation: 3-column Excel (ID|#keyword#|value) correctly parsed via DetectExcelFormat heuristic. 6 new xunit tests prove: 3-col parsing returns correct keyword-value pairs, ID column excluded from results, and format detection distinguishes 2-col vs 3-col. All 61 tests pass.
- Notes: 检测依据为第一行第一列内容是否匹配 #xxx# 格式

### R002 — 三列模式下验证第1列 ID 的唯一性，重复 ID 报错并提示具体重复项
- Class: core-capability
- Status: validated
- Description: 三列模式下验证第1列 ID 的唯一性，重复 ID 报错并提示具体重复项
- Why it matters: 防止数据表维护错误导致混淆
- Source: user
- Primary owning slice: M001-a60bo7/S01
- Supporting slices: none
- Validation: ID uniqueness validation implemented in ValidateExcelFileAsync using HashSet tracking. Duplicate IDs populate ExcelFileSummary.DuplicateRowIds and add errors to ExcelValidationResult.Errors with specific duplicate ID names. Test ValidateExcelFileAsync_ThreeColumnFormat_DetectsDuplicateIds confirms behavior.
- Notes: 两列模式不触发此校验

### R003 — 现有两列 Excel 模板（关键词|值）解析行为完全不变，所有现有功能正确性不受影响
- Class: constraint
- Status: validated
- Description: 现有两列 Excel 模板（关键词|值）解析行为完全不变，所有现有功能正确性不受影响
- Why it matters: 已有用户和数据模板不能因为新功能被破坏
- Source: inferred
- Primary owning slice: M001-a60bo7/S01
- Supporting slices: M001-a60bo7/S02
- Validation: All 3 pre-existing ExcelDataParserServiceTests pass unchanged (ParseExcelFileAsync_ValidFile_ReturnsData, ValidateExcelFileAsync_ValidFile_PassesValidation, ValidateExcelFileAsync_InvalidKeywordFormat_FailsValidation). Full 61-test suite passes with zero regressions. 2-column parsing and validation behavior is completely unchanged.
- Notes: 硬性约束，零回归

### R004 — 所有现有单元测试和集成测试在改动后继续通过
- Class: quality-attribute
- Status: validated
- Description: 所有现有单元测试和集成测试在改动后继续通过
- Why it matters: 回归安全的底线
- Source: inferred
- Primary owning slice: M001-a60bo7/S02
- Supporting slices: none
- Validation: Full test suite passes with 71 tests (0 failures, 0 skipped). Includes 12 new edge case unit tests for 3-column format (empty file, blank first rows, empty ID, single row, ID trim, multi-duplicate) and 1 new end-to-end integration test proving 3-column Excel→Word pipeline works correctly. All pre-existing tests remain green with zero regressions.
- Notes: 包含新增的三列解析和唯一性校验测试

### R005 — 将 DocuFiller 产品需求文档从 .trae/documents/ 迁移到 docs/ 并全面重写，覆盖所有现有功能：JSON/Excel 双数据源、三列 Excel 格式、富文本格式保留、页眉页脚替换、批注追踪、审核清理、JSON↔Excel 转换工具
- Class: core-capability
- Status: validated
- Description: 将 DocuFiller 产品需求文档从 .trae/documents/ 迁移到 docs/ 并全面重写，覆盖所有现有功能：JSON/Excel 双数据源、三列 Excel 格式、富文本格式保留、页眉页脚替换、批注追踪、审核清理、JSON↔Excel 转换工具
- Why it matters: 产品需求文档严重过时（2025-09），只描述了 JSON-only 的基础版本，新开发者无法通过文档了解完整功能
- Source: user
- Primary owning slice: M003-g1w88x/S01
- Supporting slices: none
- Validation: docs/DocuFiller产品需求文档.md 已创建，覆盖所有 6 个功能模块（文件输入、JSON/Excel 双数据源、文档处理、批注追踪、审核清理、转换工具），包含 Mermaid 流程图和用户界面设计
- Notes: 保持"需求+架构"两份文档拆分；中文为主；面向开发者

### R006 — 将技术架构文档从 .trae/documents/ 迁移到 docs/ 并全面重写，包含完整的 15 个服务接口定义（C# 代码）、数据模型、Mermaid 架构图、处理管道
- Class: core-capability
- Status: validated
- Description: 将技术架构文档从 .trae/documents/ 迁移到 docs/ 并全面重写，包含完整的 15 个服务接口定义（C# 代码）、数据模型、Mermaid 架构图、处理管道
- Why it matters: 架构文档只有 4 个基础服务的接口定义，缺少 Excel 解析、富文本替换、清理等 11 个新增服务
- Source: user
- Primary owning slice: M003-g1w88x/S01
- Supporting slices: none
- Validation: docs/DocuFiller技术架构文档.md 已创建，包含 14 个 public interface 定义、9 个二级章节、5 个 Mermaid 图（架构图、ER 图、3 个序列图），覆盖全部 15 个服务组件
- Notes: 保持详细风格（含代码示例、Mermaid 图）

### R007 — 更新 README.md 的功能列表、服务层架构表、项目结构、使用方法，反映所有现有功能和 15 个服务接口
- Class: core-capability
- Status: validated
- Description: 更新 README.md 的功能列表、服务层架构表、项目结构、使用方法，反映所有现有功能和 15 个服务接口
- Why it matters: README 是项目入口，当前缺少审核清理、三列 Excel、转换工具等新功能的说明
- Source: user
- Primary owning slice: M003-g1w88x/S02
- Supporting slices: none
- Validation: README.md 包含 14 个服务接口架构表（grep 验证全部 14 个 I 前缀接口存在）、6 个功能模块完整覆盖、Excel 两列/三列格式说明、准确项目结构。验证命令全部通过。
- Notes: 无

### R008 — 更新 CLAUDE.md 的服务层架构、数据模型、开发指南，补充新增服务的说明和 Excel 处理路径
- Class: core-capability
- Status: validated
- Description: 更新 CLAUDE.md 的服务层架构、数据模型、开发指南，补充新增服务的说明和 Excel 处理路径
- Why it matters: CLAUDE.md 是 AI 编码助手的上下文文件，过时信息会导致错误的代码建议
- Source: user
- Primary owning slice: M003-g1w88x/S02
- Supporting slices: none
- Validation: CLAUDE.md 包含 17 个唯一 I 前缀标识符（>=14）、16 个关键数据模型、DetectExcelFormat 处理路径说明、DI 生命周期配置。grep 验证所有关键接口和数据模型均存在。
- Notes: 无

### R009 — 在 docs/excel-data-user-guide.md 中增加三列 Excel 格式（ID|关键词|值）的使用说明、示例和验证规则
- Class: core-capability
- Status: validated
- Description: 在 docs/excel-data-user-guide.md 中增加三列 Excel 格式（ID|关键词|值）的使用说明、示例和验证规则
- Why it matters: 三列格式是 M001 新增的核心功能，但用户指南仍然只描述两列格式
- Source: user
- Primary owning slice: M003-g1w88x/S03
- Supporting slices: none
- Validation: docs/excel-data-user-guide.md 包含 6 个章节（含三列格式独立章节）、三列格式关键词匹配 11 处、无 TBD/TODO 标记
- Notes: 无

### R010 — 校验并更新 docs/features/header-footer-support.md 和 docs/批注功能说明.md，确保与实际代码实现一致
- Class: core-capability
- Status: validated
- Description: 校验并更新 docs/features/header-footer-support.md 和 docs/批注功能说明.md，确保与实际代码实现一致
- Why it matters: 功能说明文档可能存在与代码实现的偏差（页眉页脚批注的实际行为）
- Source: user
- Primary owning slice: M003-g1w88x/S03
- Supporting slices: none
- Validation: header-footer-support.md 修正为"仅正文区域支持批注"（7 个章节，含准确描述）；批注功能说明.md 与代码一致；两份文档均无 TBD/TODO
- Notes: 无

### R011 — 将 .trae/documents/ 下的 DocuFiller 产品需求文档和技术架构文档迁移到 docs/；删除 .trae/documents/ 下的 4 份文件（含 2 份 JSON 编辑器文档）
- Class: operability
- Status: validated
- Description: 将 .trae/documents/ 下的 DocuFiller 产品需求文档和技术架构文档迁移到 docs/；删除 .trae/documents/ 下的 4 份文件（含 2 份 JSON 编辑器文档）
- Why it matters: .trae/ 是 Trae IDE 的目录约定，文档应统一存放在 docs/ 下
- Source: user
- Primary owning slice: M003-g1w88x/S01
- Supporting slices: none
- Validation: .trae/documents/ 目录已删除（4 份旧文件全部清理），DocuFiller 两份文档已迁移到 docs/，JSON 编辑器两份文档已按 D004 直接删除
- Notes: JSON 编辑器文档不迁移，直接删除

## Out of Scope

### R012 — 不更新 docs/VERSION_MANAGEMENT.md、docs/EXTERNAL_SETUP.md、docs/deployment-guide.md
- Class: operability
- Status: out-of-scope
- Description: 不更新 docs/VERSION_MANAGEMENT.md、docs/EXTERNAL_SETUP.md、docs/deployment-guide.md
- Why it matters: 用户明确要求更新机制不写入文档
- Source: user
- Primary owning slice: none
- Supporting slices: none
- Validation: n/a
- Notes: 这些文档保持现状不动

### R013 — JSON 编辑器功能已移除，相关文档不迁移，直接删除
- Class: core-capability
- Status: out-of-scope
- Description: JSON 编辑器功能已移除，相关文档不迁移，直接删除
- Why it matters: 功能不存在，文档无意义
- Source: user
- Primary owning slice: none
- Supporting slices: none
- Validation: n/a
- Notes: .trae/documents/ 下两份 JSON 编辑器文档将被删除

## Traceability

| ID | Class | Status | Primary owner | Supporting | Proof |
|---|---|---|---|---|---|
| R001 | core-capability | validated | M001-a60bo7/S01 | none | 3-column Excel (ID|#keyword#|value) correctly parsed via DetectExcelFormat heuristic. 6 new xunit tests prove: 3-col parsing returns correct keyword-value pairs, ID column excluded from results, and format detection distinguishes 2-col vs 3-col. All 61 tests pass. |
| R002 | core-capability | validated | M001-a60bo7/S01 | none | ID uniqueness validation implemented in ValidateExcelFileAsync using HashSet tracking. Duplicate IDs populate ExcelFileSummary.DuplicateRowIds and add errors to ExcelValidationResult.Errors with specific duplicate ID names. Test ValidateExcelFileAsync_ThreeColumnFormat_DetectsDuplicateIds confirms behavior. |
| R003 | constraint | validated | M001-a60bo7/S01 | M001-a60bo7/S02 | All 3 pre-existing ExcelDataParserServiceTests pass unchanged (ParseExcelFileAsync_ValidFile_ReturnsData, ValidateExcelFileAsync_ValidFile_PassesValidation, ValidateExcelFileAsync_InvalidKeywordFormat_FailsValidation). Full 61-test suite passes with zero regressions. 2-column parsing and validation behavior is completely unchanged. |
| R004 | quality-attribute | validated | M001-a60bo7/S02 | none | Full test suite passes with 71 tests (0 failures, 0 skipped). Includes 12 new edge case unit tests for 3-column format (empty file, blank first rows, empty ID, single row, ID trim, multi-duplicate) and 1 new end-to-end integration test proving 3-column Excel→Word pipeline works correctly. All pre-existing tests remain green with zero regressions. |
| R005 | core-capability | validated | M003-g1w88x/S01 | none | docs/DocuFiller产品需求文档.md 已创建，覆盖所有 6 个功能模块（文件输入、JSON/Excel 双数据源、文档处理、批注追踪、审核清理、转换工具），包含 Mermaid 流程图和用户界面设计 |
| R006 | core-capability | validated | M003-g1w88x/S01 | none | docs/DocuFiller技术架构文档.md 已创建，包含 14 个 public interface 定义、9 个二级章节、5 个 Mermaid 图（架构图、ER 图、3 个序列图），覆盖全部 15 个服务组件 |
| R007 | core-capability | validated | M003-g1w88x/S02 | none | README.md 包含 14 个服务接口架构表（grep 验证全部 14 个 I 前缀接口存在）、6 个功能模块完整覆盖、Excel 两列/三列格式说明、准确项目结构。验证命令全部通过。 |
| R008 | core-capability | validated | M003-g1w88x/S02 | none | CLAUDE.md 包含 17 个唯一 I 前缀标识符（>=14）、16 个关键数据模型、DetectExcelFormat 处理路径说明、DI 生命周期配置。grep 验证所有关键接口和数据模型均存在。 |
| R009 | core-capability | validated | M003-g1w88x/S03 | none | docs/excel-data-user-guide.md 包含 6 个章节（含三列格式独立章节）、三列格式关键词匹配 11 处、无 TBD/TODO 标记 |
| R010 | core-capability | validated | M003-g1w88x/S03 | none | header-footer-support.md 修正为"仅正文区域支持批注"（7 个章节，含准确描述）；批注功能说明.md 与代码一致；两份文档均无 TBD/TODO |
| R011 | operability | validated | M003-g1w88x/S01 | none | .trae/documents/ 目录已删除（4 份旧文件全部清理），DocuFiller 两份文档已迁移到 docs/，JSON 编辑器两份文档已按 D004 直接删除 |
| R012 | operability | out-of-scope | none | none | n/a |
| R013 | core-capability | out-of-scope | none | none | n/a |

## Coverage Summary

- Active requirements: 0
- Mapped to slices: 0
- Validated: 11 (R001, R002, R003, R004, R005, R006, R007, R008, R009, R010, R011)
- Unmapped active requirements: 0
