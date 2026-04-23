# S01: 产品需求 + 技术架构文档重写与迁移

**Goal:** 在 docs/ 下创建完整的产品需求文档和技术架构文档，涵盖所有现有功能（JSON/Excel 双数据源、两列/三列 Excel、富文本格式保留、页眉页脚替换、批注追踪、审核清理、JSON↔Excel 转换工具），删除 .trae/documents/ 下的 4 份旧文件
**Demo:** docs/ 下有完整的产品需求文档和技术架构文档，涵盖所有现有功能（关键词替换、审核清理、工具、Excel 双格式、富文本、页眉页脚、批注）；.trae/documents/ 下 4 份旧文件已删除

## Must-Haves

- docs/DocuFiller产品需求文档.md 涵盖所有现有功能（关键词替换、审核清理、转换工具、Excel 双格式、富文本、页眉页脚、批注）\n- docs/DocuFiller技术架构文档.md 包含完整 15 个服务接口定义、数据模型代码、Mermaid 图\n- .trae/documents/ 目录已删除（4 份旧文件全部清理）\n- 两份文档代码示例与实际代码精确匹配

## Proof Level

- This slice proves: contract

## Integration Closure

- Upstream surfaces consumed: .trae/documents/ 下的 2 份旧文档（作为重写参考），所有 Services/Interfaces/*.cs 和 Models/*.cs 文件（提取接口和模型定义）\n- New wiring introduced in this slice: 无（纯文档工作）\n- What remains before the milestone is truly usable end-to-end: S02（README.md + CLAUDE.md 更新）和 S03（功能级文档更新）

## Verification

- Not provided.

## Tasks

- [x] **T01: 撰写 DocuFiller 产品需求文档并迁移到 docs/** `est:1.5h`
  全面重写 DocuFiller 产品需求文档，从仅覆盖 JSON-only 基础版本扩展到覆盖所有现有功能：

1. **文件输入模块**：单文件选择和文件夹拖拽输入，自动扫描 docx 文件
2. **数据配置模块**：JSON 和 Excel 双数据源，Excel 支持两列（关键词|值）和三列（ID|关键词|值）格式
3. **文档处理模块**：关键词替换（通过内容控件 Tag）、富文本格式保留（上标/下标）、页眉页脚内容控件支持
4. **批注追踪**：正文区域自动添加变更批注（旧值、新值、时间），页眉页脚不添加批注
5. **审核清理模块**：去除批注痕迹（颜色恢复黑色、删除批注标记），解除内容控件包装
6. **转换工具**：JSON↔Excel 转换功能
7. **输出管理**：时间戳子文件夹，保持目录结构
8. **更新检查**：版本检查和更新提示

文档需包含：产品概述、用户角色、功能模块详细描述（含页面表格）、核心流程（含 Mermaid 流程图）、用户界面设计、各模块的业务规则。

语言：中文为主。面向开发者。

注意：JSON 编辑器功能已移除，不写入文档。更新机制按用户要求不写入文档。
  - Files: `Services/Interfaces/IDocumentProcessor.cs`, `Services/Interfaces/IExcelDataParser.cs`, `Services/Interfaces/IDocumentCleanupService.cs`, `Services/Interfaces/IExcelToWordConverter.cs`, `Services/ContentControlProcessor.cs`, `Services/CommentManager.cs`, `ViewModels/MainWindowViewModel.cs`, `Models/ContentControlData.cs`, `Models/FormattedCellValue.cs`, `Models/CleanupFileItem.cs`
  - Verify: test -f docs/DocuFiller产品需求文档.md && grep -c "^## " docs/DocuFiller产品需求文档.md | grep -qE "^[0-9]+$" && grep -q "Excel" docs/DocuFiller产品需求文档.md && grep -q "审核清理" docs/DocuFiller产品需求文档.md && grep -q "页眉" docs/DocuFiller产品需求文档.md && grep -q "批注" docs/DocuFiller产品需求文档.md && grep -q "转换" docs/DocuFiller产品需求文档.md

- [x] **T02: 撰写 DocuFiller 技术架构文档并迁移到 docs/** `est:2h`
  全面重写 DocuFiller 技术架构文档，从 4 个基础服务接口扩展到完整的 15 个服务接口定义（C# 代码）。

文档结构：
1. **架构设计**：Mermaid 分层架构图（表示层/业务层/服务层/外部资源）
2. **技术栈说明**：.NET 8, WPF, OpenXML, EPPlus, Newtonsoft.Json
3. **服务层架构**：完整表格列出所有 15 个服务接口（名称、接口、职责、注册方式）
4. **API 定义**：每个接口的完整 C# 接口定义（从实际代码文件中提取），包括：
   - IDocumentProcessor（文档处理）
   - IDataParser（JSON 数据解析）
   - IExcelDataParser（Excel 数据解析）
   - IFileService（文件操作）
   - IProgressReporter（进度报告）
   - IFileScanner（文件扫描）
   - IDirectoryManager（目录管理）
   - IExcelToWordConverter（JSON↔Excel 转换）
   - ISafeTextReplacer（安全文本替换）
   - ISafeFormattedContentReplacer（安全格式化内容替换）
   - ITemplateCacheService（模板缓存）
   - IKeywordValidationService（关键词验证）
   - IJsonEditorService（JSON 编辑器 — 注明已废弃但接口保留）
   - IDocumentCleanupService（文档清理）
   - ContentControlProcessor（内容控件处理器）
   - CommentManager（批注管理器）
5. **数据模型**：所有数据模型的 C# 类定义和 ER 图（Mermaid）
6. **处理管道**：文档处理流程的 Mermaid 序列图
7. **表格内容控件处理**：SafeTextReplacer 三种替换策略的详细说明
8. **依赖注入配置**：App.xaml.cs 中的服务注册方式

语言：中文为主。包含完整 C# 接口定义和 Mermaid 图。

注意：保持详细风格（含代码示例、Mermaid 图），与现有技术架构文档风格一致。代码示例必须与实际代码精确匹配。
  - Files: `Services/Interfaces/IDocumentProcessor.cs`, `Services/Interfaces/IDataParser.cs`, `Services/Interfaces/IExcelDataParser.cs`, `Services/Interfaces/IFileService.cs`, `Services/Interfaces/IProgressReporter.cs`, `Services/Interfaces/IFileScanner.cs`, `Services/Interfaces/IDirectoryManager.cs`, `Services/Interfaces/IExcelToWordConverter.cs`, `Services/Interfaces/ISafeTextReplacer.cs`, `Services/Interfaces/ISafeFormattedContentReplacer.cs`, `Services/Interfaces/ITemplateCacheService.cs`, `Services/Interfaces/IKeywordValidationService.cs`, `Services/Interfaces/IDocumentCleanupService.cs`, `Services/ContentControlProcessor.cs`, `Services/CommentManager.cs`, `Services/SafeTextReplacer.cs`, `Services/SafeFormattedContentReplacer.cs`, `Models/ContentControlData.cs`, `Models/FormattedCellValue.cs`, `Models/TextFragment.cs`, `Models/ProcessRequest.cs`, `Models/ProcessResult.cs`, `Models/ProgressEventArgs.cs`, `Models/CleanupFileItem.cs`, `Models/CleanupProgressEventArgs.cs`, `Models/ExcelFileSummary.cs`, `Models/ExcelValidationResult.cs`, `App.xaml.cs`, `Utils/OpenXmlTableCellHelper.cs`
  - Verify: test -f docs/DocuFiller技术架构文档.md && grep -c "^## " docs/DocuFiller技术架构文档.md | grep -qE "^[0-9]+$" && grep -c "public interface" docs/DocuFiller技术架构文档.md | grep -qE "^[0-9]+$" && grep -q "mermaid" docs/DocuFiller技术架构文档.md && grep -q "SafeTextReplacer" docs/DocuFiller技术架构文档.md && grep -q "IExcelDataParser" docs/DocuFiller技术架构文档.md && grep -q "IDocumentCleanupService" docs/DocuFiller技术架构文档.md

- [x] **T03: 删除 .trae/documents/ 下的 4 份旧文档** `est:5m`
  删除 .trae/documents/ 目录下的全部 4 个文件：

1. `DocuFiller产品需求文档.md` — 已迁移到 docs/DocuFiller产品需求文档.md（T01 产出）
2. `DocuFiller技术架构文档.md` — 已迁移到 docs/DocuFiller技术架构文档.md（T02 产出）
3. `JSON关键词编辑器产品需求文档.md` — 不迁移，直接删除（D004：JSON 编辑器功能已移除）
4. `JSON关键词编辑器技术架构文档.md` — 不迁移，直接删除（D004）

删除后检查 .trae/ 目录是否为空，如果为空则删除 .trae/ 目录本身。
  - Files: `.trae/documents/DocuFiller产品需求文档.md`, `.trae/documents/DocuFiller技术架构文档.md`, `.trae/documents/JSON关键词编辑器产品需求文档.md`, `.trae/documents/JSON关键词编辑器技术架构文档.md`
  - Verify: ! test -d .trae/documents/ && test -f docs/DocuFiller产品需求文档.md && test -f docs/DocuFiller技术架构文档.md

## Files Likely Touched

- Services/Interfaces/IDocumentProcessor.cs
- Services/Interfaces/IExcelDataParser.cs
- Services/Interfaces/IDocumentCleanupService.cs
- Services/Interfaces/IExcelToWordConverter.cs
- Services/ContentControlProcessor.cs
- Services/CommentManager.cs
- ViewModels/MainWindowViewModel.cs
- Models/ContentControlData.cs
- Models/FormattedCellValue.cs
- Models/CleanupFileItem.cs
- Services/Interfaces/IDataParser.cs
- Services/Interfaces/IFileService.cs
- Services/Interfaces/IProgressReporter.cs
- Services/Interfaces/IFileScanner.cs
- Services/Interfaces/IDirectoryManager.cs
- Services/Interfaces/ISafeTextReplacer.cs
- Services/Interfaces/ISafeFormattedContentReplacer.cs
- Services/Interfaces/ITemplateCacheService.cs
- Services/Interfaces/IKeywordValidationService.cs
- Services/SafeTextReplacer.cs
- Services/SafeFormattedContentReplacer.cs
- Models/TextFragment.cs
- Models/ProcessRequest.cs
- Models/ProcessResult.cs
- Models/ProgressEventArgs.cs
- Models/CleanupProgressEventArgs.cs
- Models/ExcelFileSummary.cs
- Models/ExcelValidationResult.cs
- App.xaml.cs
- Utils/OpenXmlTableCellHelper.cs
- .trae/documents/DocuFiller产品需求文档.md
- .trae/documents/DocuFiller技术架构文档.md
- .trae/documents/JSON关键词编辑器产品需求文档.md
- .trae/documents/JSON关键词编辑器技术架构文档.md
