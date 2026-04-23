# S02: README.md + CLAUDE.md 更新 — UAT

**Milestone:** M003-g1w88x
**Written:** 2026-04-23T10:36:50.646Z

# S02: README.md + CLAUDE.md 更新 — UAT

**Milestone:** M003-g1w88x
**Written:** 2026-04-23

## UAT Type

- UAT mode: artifact-driven
- Why this mode is sufficient: 本切片仅涉及文档更新，无运行时代码变更。通过结构化 grep 验证文档内容完整性即可确认交付质量。

## Preconditions

- 代码库处于 M003-g1w88x 工作树中
- S01 已完成（产品需求文档和技术架构文档作为参考源已就绪）
- README.md 和 CLAUDE.md 已由 T01/T02 更新

## Smoke Test

打开 README.md，确认"服务层架构"章节包含 IDocumentProcessor、IDataParser、IExcelDataParser 等 14 个接口的表格。

## Test Cases

### 1. README.md 服务接口完整性

1. 在 README.md 中搜索 14 个核心服务接口名称
2. **Expected:** 全部 14 个接口（IDocumentProcessor, IDataParser, IExcelDataParser, IFileService, IProgressReporter, IFileScanner, IDirectoryManager, IExcelToWordConverter, ISafeTextReplacer, ISafeFormattedContentReplacer, IDocumentCleanupService, IKeywordValidationService, ITemplateCacheService, IJsonEditorService）均至少出现一次

### 2. README.md 功能模块覆盖

1. 检查 README.md "主要功能" 章节
2. **Expected:** 覆盖文件输入与管理、JSON/Excel 双数据源、文档处理、批注追踪、审核清理、转换工具 6 个模块

### 3. README.md Excel 三列格式说明

1. 搜索 README.md 中"三列"相关内容
2. **Expected:** 包含三列格式说明和示例

### 4. CLAUDE.md 服务接口完整性

1. 在 CLAUDE.md 中搜索服务接口标识符
2. **Expected:** 唯一 I 前缀标识符数量 >= 14

### 5. CLAUDE.md 数据模型覆盖

1. 检查 CLAUDE.md "关键数据模型" 章节
2. **Expected:** 包含 FormattedCellValue、TextFragment、ExcelFileSummary、CleanupFileItem、FolderProcessRequest、InputSourceType、FolderStructure 等新增模型

### 6. CLAUDE.md Excel 处理路径

1. 搜索 CLAUDE.md 中 DetectExcelFormat 和格式检测说明
2. **Expected:** 包含两列/三列格式自动检测机制说明

### 7. 文档一致性

1. 对比 README.md 和 CLAUDE.md 的服务接口列表
2. **Expected:** 14 个核心接口在两份文档中均存在，无术语矛盾

## Edge Cases

### IJsonEditorService 处理

1. 搜索 IJsonEditorService 在两份文档中的出现
2. **Expected:** CLAUDE.md 中存在（作为历史记录），README.md 中可能不出现（功能已删除），不构成矛盾

### IUpdateService 处理

1. 搜索 IUpdateService 在两份文档中的出现
2. **Expected:** 可能出现在 CLAUDE.md 但不在 README.md 的核心架构表中（更新机制独立文档跟踪）

## Failure Signals

- 任一核心服务接口在 README.md 中缺失
- CLAUDE.md 接口标识符数量 < 14
- 关键数据模型（FormattedCellValue、TextFragment 等）在 CLAUDE.md 中缺失
- 两份文档对同一功能使用不同术语（如"审核清理" vs "批注清理"）

## Not Proven By This UAT

- 文档内容的准确性（需人工逐行比对代码库验证）
- 项目结构目录列表与实际文件系统的完全一致
- NuGet 包版本号的准确性

## Notes for Tester

- 本切片为纯文档更新，无代码变更
- IJsonEditorService 和 IUpdateService 的处理是预期行为，不视为缺陷
- 文档中的代码示例引用了实际代码路径，如需验证精确性需对照源码
