# S02: README.md + CLAUDE.md 更新

**Goal:** 更新 README.md 和 CLAUDE.md，使其反映代码库当前状态：完整功能列表（6 个功能模块）、14 个服务接口的服务层架构表、准确的项目结构（含所有目录和关键文件）、Excel 双格式处理路径、审核清理/批注/转换工具等功能说明。
**Demo:** README.md 反映完整功能列表、15 个服务接口的项目结构、准确的使用方法；CLAUDE.md 的服务层架构表完整且准确

## Must-Haves

- README.md 的服务层架构表包含全部 14 个服务接口（IDocumentProcessor, IDataParser, IExcelDataParser, IFileService, IProgressReporter, IFileScanner, IDirectoryManager, IExcelToWordConverter, ISafeTextReplacer, ISafeFormattedContentReplacer, IDocumentCleanupService, IKeywordValidationService, ITemplateCacheService, IJsonEditorService）\n- README.md 的主要功能列表覆盖 6 个功能模块（文件输入、JSON/Excel 双数据源、文档处理、批注追踪、审核清理、转换工具）\n- README.md 的 Excel 说明包含两列和三列格式\n- README.md 项目结构反映所有实际目录\n- CLAUDE.md 服务层架构包含全部 14 个接口\n- CLAUDE.md 数据模型列表包含所有关键模型\n- CLAUDE.md 包含 Excel 处理路径说明（两列/三列格式检测）\n- 两份文档之间术语一致，无矛盾

## Proof Level

- This slice proves: contract

## Integration Closure

- Upstream surfaces consumed: docs/DocuFiller产品需求文档.md and docs/DocuFiller技术架构文档.md (from S01)\n- New wiring introduced: none — documentation only\n- What remains: S03 (feature-level docs) can cross-reference with updated README/CLAUDE

## Verification

- Not provided.

## Tasks

- [x] **T01: 重写 README.md 反映完整功能列表、14 个服务接口、准确项目结构和使用方法** `est:45m`
  读取 S01 产出的产品需求文档和技术架构文档作为权威参考，结合代码库实际状态，全面重写 README.md。需要更新以下部分：

1. **主要功能**：从 7 项扩展到完整 6 个功能模块（文件输入与管理、JSON/Excel 双数据源含两列/三列格式、文档处理含富文本和页眉页脚、批注追踪、审核清理、转换工具）
2. **服务层架构表**：从 6 个接口扩展到 14 个，每个接口标注职责和实现类
3. **项目结构**：反映所有实际目录（含 DocuFiller/ 子目录、External/、Configuration/、Models/Update/、Services/Update/、ViewModels/Update/、Views/Update/）
4. **使用方法**：Excel 部分补充三列格式说明
5. **数据模型**：补充关键模型列表
6. **核心数据模型表**：补充新增模型
7. **技术架构/核心框架**：确认 NuGet 包版本号准确
8. **文档处理管道**：保持不变（已准确）

注意事项：
- 不涉及更新机制文档（D005）
- 不涉及 JSON 编辑器（D004 已删除）
- 表格内容控件处理部分保留不变（已准确且完整）
- 确保所有代码引用与实际代码匹配
  - Files: `README.md`, `docs/DocuFiller产品需求文档.md`, `docs/DocuFiller技术架构文档.md`, `Services/Interfaces/`, `App.xaml.cs`, `Models/`
  - Verify: grep -c "^## " README.md` returns >= 8; `grep -c "I[A-Z]" README.md` returns >= 14 interface mentions; `grep -q "IDocumentCleanupService" README.md` && `grep -q "IExcelToWordConverter" README.md` && `grep -q "三列" README.md`

- [x] **T02: 更新 CLAUDE.md 的服务层架构、数据模型、Excel 处理路径和开发指南** `est:30m`
  在 T01 的基础上更新 CLAUDE.md，这是 AI 编码助手的上下文文件。需要更新以下部分：

1. **服务层架构**：从 6 个接口扩展到 14 个完整列表，每个标注职责
2. **关键数据模型**：补充 FormattedCellValue、TextFragment、ExcelFileSummary、ExcelValidationResult、CleanupFileItem、CleanupProgressEventArgs、InputSourceType、FolderProcessRequest、FolderStructure 等模型
3. **Excel 处理路径**：补充两列/三列格式自动检测机制（DetectExcelFormat 内部实现）、EPPlus 使用说明
4. **新增服务说明**：
   - IDocumentCleanupService + CleanupCommentProcessor + CleanupControlProcessor（审核清理）
   - CommentManager（批注追踪）
   - IExcelToWordConverter（转换工具）
   - IKeywordValidationService（关键词验证）
   - ITemplateCacheService（模板缓存）
   - ISafeFormattedContentReplacer（富文本替换）
5. **DI 配置**：补充完整的 DI 注册说明，包括 Singleton vs Transient 的选择
6. **文件结构说明**：更新目录列表
7. **开发规范**：保留现有开发规范不变

注意事项：
- 保持中文语言规范
- 保留 BAT 脚本规范和图片处理规范
- 表格内容控件处理部分保留不变（已准确）
- 不涉及更新机制和 JSON 编辑器
  - Files: `CLAUDE.md`, `docs/DocuFiller技术架构文档.md`, `Services/Interfaces/`, `App.xaml.cs`
  - Verify: grep -c "I[A-Z]" CLAUDE.md` returns >= 14 interface mentions; `grep -q "IDocumentCleanupService" CLAUDE.md` && `grep -q "IExcelToWordConverter" CLAUDE.md` && `grep -q "ITemplateCacheService" CLAUDE.md`; `grep -c "^## " CLAUDE.md` returns >= 5

## Files Likely Touched

- README.md
- docs/DocuFiller产品需求文档.md
- docs/DocuFiller技术架构文档.md
- Services/Interfaces/
- App.xaml.cs
- Models/
- CLAUDE.md
