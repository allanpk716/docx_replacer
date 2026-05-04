# M020: 代码质量与技术债清理

**Gathered:** 2025-07-09
**Status:** Ready for planning

## Project Description

DocuFiller 是一个 .NET 8 + WPF 桌面应用，提供 Word 文档批量填充、富文本替换、批注追踪和审核清理功能。支持 GUI（WPF）和 CLI 双模式运行。当前版本 v1.8.0。

## Why This Milestone

项目经过 19 个里程碑的快速迭代，积累了显著技术债：异常被静默吞没（FileService 4 处裸 catch）、取消功能是空操作（CancellationTokenSource 从未实例化）、两个核心服务间存在 7 个重复方法、大量死代码和幽灵配置值、测试覆盖不足、文档与实际代码不一致。这些问题导致错误难以诊断、新人难以理解代码、维护成本持续上升。

## User-Visible Outcome

### When this milestone is complete, the user can:

- 在 FileService 操作失败时看到有意义的日志输出而非静默失败
- CLAUDE.md 准确描述 update 子命令、完整错误码表、所有服务接口
- 代码库无死代码、无重复方法，新人可以快速理解架构
- 构建和测试全部通过，无回归

### Entry point / environment

- Entry point: 开发者阅读 CLAUDE.md 并运行 `dotnet build` / `dotnet test`
- Environment: 本地开发环境（Windows + .NET 8 SDK）
- Live dependencies involved: 无外部依赖变化，纯内部重构

## Completion Class

- Contract complete means: `dotnet build` 0 错误，`dotnet test` 全部通过，无回归
- Integration complete means: CLI 和 GUI 模式功能不受影响，CLAUDE.md 与代码一致
- Operational complete means: 不涉及运行时环境变化，仅需代码和文档一致性

## Final Integrated Acceptance

To call this milestone complete, we must prove:

- `dotnet build` 0 错误 + `dotnet test` 全部通过（包括新增测试）
- FileService 异常路径日志可验证（单元测试断言日志输出）
- CLAUDE.md 包含 update 子命令文档、完整错误码表、准确的服务列表和文件结构
- grep 确认死代码已删除、重复方法已提取到 OpenXmlHelper
- 无需模拟——所有改动在真实构建和测试环境中验证

## Architectural Decisions

### 取消功能处理

**Decision:** 删除 CancelProcessing() 方法及相关 CancellationTokenSource 字段

**Rationale:** CancelProcessing() 是空操作——_cancellationTokenSource 声明了但从未 new，Cancel() 永远打到 null。UI 中没有取消按钮调用它。做成真正的取消需要大改处理循环（ProcessExcelDataAsync 中 200+ 行的 foreach），远超本里程碑范围。

**Alternatives Considered:**
- 让取消真正生效 — 工作量过大，需要重写处理循环并添加 UI 取消按钮
- 保留接口但标注 TODO — 最小改动但留了技术债，与里程碑目标矛盾

### 重复代码提取策略

**Decision:** 提取到 Utils/ 下的 OpenXmlHelper 静态工具类，两个服务类调用工具类

**Rationale:** 7 个重复方法（GetControlTag、ExtractExistingText、FindContentContainer、AddProcessingComment、FindAllTargetRuns、CreateParagraphWithFormattedText、CreateFormattedRuns）属于 OpenXML 操作工具方法，不依赖服务状态。提取为静态方法最清晰。CreateParagraphWithFormattedText 签名需合并——CCP 接受 string，DPS 接受 FormattedCellValue+RunProperties，合并为接受 FormattedCellValue 的统一签名（CCP 调用时构造简单的 FormattedCellValue）。

**Alternatives Considered:**
- DPS 调用 CCP — 改动最小但制造了服务间依赖，违反职责分离
- 只加 TODO 不提取 — 技术债未清理，与里程碑目标矛盾

### 未使用配置值清理

**Decision:** 从 appsettings.json 和 Configuration/ 类中删除所有未被引用的配置项

**Rationale:** 确认只有 EnableTemplateCache 和 CacheExpirationMinutes（PerformanceSettings）被 TemplateCacheService 实际引用。以下配置项完全无代码引用：MaxFileSize、EnableFileBackup、MaxConcurrentProcessing、ProcessingTimeout、AutoSaveSettings、ShowProgressDetails、ConfirmBeforeExit、WindowWidth、WindowHeight、DefaultOutputDirectory、SupportedExtensions。UpdateUrl 和 Channel 已迁移到持久化配置，appsettings.json 中保留为空字符串作为占位。

**Alternatives Considered:**
- 保留类但清理 JSON — Configuration 类中的死属性仍然误导开发者
- 标注注释但全部保留 — 配置文件仍然臃肿，未真正清理

### 死方法处理策略

**Decision:** ProcessSingleDocumentAsync 从 IDocumentProcessor 接口和实现中直接删除；执行前对 ContentControlProcessor 的"5个死方法"做全面确认

**Rationale:** ProcessSingleDocumentAsync 是接口上的公开方法但无调用者（FillCommand 用 ProcessDocumentsAsync，ViewModel 也是）。删除接口方法会破坏兼容性，但此项目无外部消费者，所有 mock 实现也在测试代码中可控。ContentControlProcessor 的方法需要实际扫描确认哪些真正无引用——初步检查发现所有方法都有内部引用，Roadmap 中的"5个死方法"可能需要重新评估。

**Alternatives Considered:**
- 标记 [Obsolete] 不删除 — 留了半个里程碑的过渡期债务
- 按 Roadmap 列表直接删 — 有错删风险，CCP 方法都有内部调用

### 测试覆盖深度

**Decision:** 核心路径测试 + 日志验证

**Rationale:** FileService 的 catch 块改为日志输出后，测试验证日志确实被写入。TemplateCacheService 测试缓存过期、清除逻辑。不追求高覆盖率数字，聚焦于"改动不破坏现有功能"和"新行为可验证"。

**Alternatives Considered:**
- 全面测试覆盖 — 工作量过大，偏离技术债清理主题
- 只验证现有测试通过 — 无法验证 FileService 异常日志改进是否生效

## Error Handling Strategy

**当前问题：** FileService 有 4 处裸 `catch { return false; }`，异常被完全吞没——调用方无法知道失败原因，日志中无任何记录。

**修复策略：**
1. 为 FileService 注入 ILogger（当前无 logger）
2. 每个 catch 块中记录异常（`_logger.LogError(ex, "操作描述")`）
3. 保持返回 false 的 API 不变（不破坏调用方），但确保异常被记录
4. 单元测试验证：mock ILogger，断言 LogError 被调用

## Risks and Unknowns

- **CCP 死方法范围不确定** — Roadmap 提到 5 个死方法，但初步扫描显示所有方法都有内部引用。需要在执行时用 call graph 分析确认，可能实际死方法数量少于预期
- **CreateParagraphWithFormattedText 签名合并** — DPS 版本接受 FormattedCellValue + RunProperties，CCP 版本接受 string。合并后 CCP 调用点需要构造 FormattedCellValue，需确保格式不丢失
- **接口删除的测试影响** — 删除 IDocumentProcessor.ProcessSingleDocumentAsync 会影响所有 mock/stub 实现（CommandValidationTests 等），需逐一更新
- **重复方法行为差异** — 两个服务中的"相同"方法可能在细节行为上有差异（空值处理、日志格式等），提取时需仔细对比

## Existing Codebase / Prior Art

- `Services/FileService.cs` — 137 行，4 处裸 catch，无 logger 注入
- `Services/DocumentProcessorService.cs` — 1131 行，包含重复方法和 _cancellationTokenSource 空操作
- `Services/ContentControlProcessor.cs` — 493 行，包含重复方法
- `Utils/OpenXmlTableCellHelper.cs` — 已有的 OpenXML 工具类，新增 OpenXmlHelper 可参考其风格
- `Configuration/AppSettings.cs` — 包含大量未使用配置属性的配置类定义
- `appsettings.json` — 包含未使用配置值的配置文件
- `CLAUDE.md` — 缺少 update 子命令、错误码不完整、服务列表缺少 IUpdateService

## Relevant Requirements

- R004 — 所有现有单元测试和集成测试在改动后继续通过（硬性约束）

## Scope

### In Scope

- FileService 异常处理修复（注入 logger，catch 块记录日志）
- 删除 CancelProcessing() 及 CancellationTokenSource 相关代码
- 删除 ProcessSingleDocumentAsync（接口 + 实现 + 测试 mock）
- 删除 GenerateOutputFileNameWithTimestamp（DPS private 方法）
- 确认并删除 ContentControlProcessor 中实际无引用的方法
- 7 个重复方法提取到 OpenXmlHelper 静态工具类
- 清理 appsettings.json 和 Configuration/ 类中未使用的配置值
- CLAUDE.md 补充：update 子命令文档、完整错误码表、IUpdateService 服务描述、准确文件结构
- FileService 和 TemplateCacheService 核心路径单元测试

### Out of Scope / Non-Goals

- 不实现真正的取消功能（需大改处理循环 + UI 改动，超出范围）
- 不重构 DI 注册或服务生命周期
- 不添加新功能
- 不改变 Velopack 更新机制
- 不重写测试框架或引入新测试库

## Technical Constraints

- .NET 8 + WPF 项目，Windows 平台
- 所有改动必须通过 `dotnet build` 和 `dotnet test`
- 不能破坏 CLI 和 GUI 两种运行模式
- 配置删除需确保 TemplateCacheService 的 EnableTemplateCache 和 CacheExpirationMinutes 不受影响

## Integration Points

- `IFileService` — 被 DocumentProcessorService、ExcelDataParserService、MainWindowViewModel 注入
- `IDocumentProcessor` — 被 FillCommand、MainWindowViewModel 使用；接口签名变更影响所有实现和 mock
- `ContentControlProcessor` — 被 DocumentProcessorService 内部创建和使用
- `TemplateCacheService` — 使用 PerformanceSettings 的 EnableTemplateCache 和 CacheExpirationMinutes
- `IUpdateService` / `UpdateService` — CLAUDE.md 需要补充描述
- CLI 命令 — UpdateCommand 需要在 CLAUDE.md 的子命令表中补充

## Testing Requirements

- 现有 249 个测试全部通过（回归底线）
- FileService 新增测试：验证 catch 块记录日志（mock ILogger 断言）
- TemplateCacheService 新增测试：缓存过期、清除逻辑
- OpenXmlHelper 提取后：现有 CCP/DPS 集成测试覆盖重复方法的行为
- 不要求特定覆盖率数字

## Acceptance Criteria

### S01: FileService 异常处理 + 取消功能清理
- FileService 所有 catch 块记录日志
- CancelProcessing() 及 CancellationTokenSource 字段已删除
- IDocumentProcessor 接口不再包含 CancelProcessing
- FileService 单元测试验证日志输出

### S02: 死代码清理
- ProcessSingleDocumentAsync 从接口和实现中删除
- GenerateOutputFileNameWithTimestamp 删除
- CCP 死方法经全面确认后删除
- 所有测试 mock 同步更新

### S03: 重复代码消除
- OpenXmlHelper 静态工具类创建，包含 7 个提取的方法
- DPS 和 CCP 调用 OpenXmlHelper
- CreateParagraphWithFormattedText 签名统一为接受 FormattedCellValue
- 现有集成测试验证行为不变

### S04: CLAUDE.md 文档更新
- 补充 update 子命令（参数、输出格式、使用示例）
- 完整错误码表（包括 UPDATE_NOT_AVAILABLE 等）
- 服务表补充 IUpdateService
- 文件结构更新（Cli/Commands/ 下补充 UpdateCommand.cs）

### S05: 配置清理和测试补充
- appsettings.json 移除未使用配置值
- Configuration/ 类移除对应属性
- TemplateCacheService 单元测试覆盖缓存核心逻辑

## Open Questions

- CCP 死方法实际范围 — 需要执行时用工具分析调用图确认。Roadmap 说 5 个，初步扫描显示所有方法都有引用，可能需要重新定义"死方法"（如仅被其他死方法调用的方法）
