---
id: T02
parent: S02
milestone: M003-g1w88x
key_files:
  - CLAUDE.md
key_decisions:
  - 保留原 CLAUDE.md 的表格内容控件处理部分不变（已准确），仅扩展周边内容
  - 数据模型以表格形式呈现而非完整类定义，保持 CLAUDE.md 的简洁性（完整定义在技术架构文档中）
duration: 
verification_result: passed
completed_at: 2026-04-23T10:35:35.531Z
blocker_discovered: false
---

# T02: 更新 CLAUDE.md 服务层架构表至 14+ 接口，补充完整数据模型、Excel 双格式处理路径、审核清理/批注/转换功能说明和 DI 生命周期配置

**更新 CLAUDE.md 服务层架构表至 14+ 接口，补充完整数据模型、Excel 双格式处理路径、审核清理/批注/转换功能说明和 DI 生命周期配置**

## What Happened

在 T01 更新 README.md 的基础上，全面更新 CLAUDE.md 使其反映代码库当前状态。主要变更：

1. **服务层架构表**：从原 6 个接口扩展为 14 个服务接口 + 2 个非接口处理器（ContentControlProcessor, CommentManager）的完整列表，每个标注接口名、实现类和职责。新增了 IExcelDataParser、IExcelToWordConverter、ISafeTextReplacer、ISafeFormattedContentReplacer、ITemplateCacheService、IKeywordValidationService、IDocumentCleanupService、IJsonEditorService、IUpdateService 共 9 个接口。

2. **关键数据模型**：从原 4 个模型扩展为 16 个完整模型列表，涵盖 FormattedCellValue、TextFragment、ExcelFileSummary、ExcelValidationResult、CleanupFileItem、CleanupProgressEventArgs、InputSourceType、FolderProcessRequest、FolderStructure 等。

3. **Excel 双格式处理路径**：新增专门章节说明两列/三列格式自动检测机制（DetectExcelFormat 内部实现）、EPPlus 使用说明、富文本支持、已知问题（空工作表 NullReferenceException）。

4. **新增功能说明**：审核清理（IDocumentCleanupService + CleanupCommentProcessor + CleanupControlProcessor）、批注追踪（CommentManager + ContentControlProcessor 协调逻辑）、富文本格式替换（ISafeFormattedContentReplacer）。

5. **DI 生命周期配置**：补充 Singleton vs Transient 选择原则和具体示例。

6. **文件结构说明**：更新为与 T01 README 一致的完整目录列表，包含 Tools/、Converters/、Exceptions/ 等目录。

7. **配置系统说明**：更新为 appsettings.json + IOptions<T> 模式，保留 App.config 向后兼容说明。

验证结果：I 前缀接口提及 >= 14（实际 17），IDocumentCleanupService/IExcelToWordConverter/ITemplateCacheService 均存在，H2 章节 >= 5（实际 5），所有任务要求的数据模型均已包含。

## Verification

运行 grep 验证：
- `grep -oE "I[A-Z][A-Za-z]+" CLAUDE.md` 返回 17 个唯一 I 前缀标识符（>= 14 ✓）
- `grep -q "IDocumentCleanupService" CLAUDE.md` ✓ 存在
- `grep -q "IExcelToWordConverter" CLAUDE.md` ✓ 存在
- `grep -q "ITemplateCacheService" CLAUDE.md` ✓ 存在
- `grep -c "^## " CLAUDE.md` 返回 5（>= 5 ✓）
- 所有关键数据模型（FormattedCellValue, TextFragment, ExcelFileSummary, CleanupFileItem, FolderProcessRequest, InputSourceType, FolderStructure）均已包含
- DetectExcelFormat、两列/三列格式说明均已包含

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `grep -oE "I[A-Z][A-Za-z]+" CLAUDE.md | sort -u | wc -l` | 0 | ✅ pass (17 unique I-prefixed identifiers, >= 14 required) | 200ms |
| 2 | `grep -q IDocumentCleanupService CLAUDE.md && echo FOUND` | 0 | ✅ pass | 100ms |
| 3 | `grep -q IExcelToWordConverter CLAUDE.md && echo FOUND` | 0 | ✅ pass | 100ms |
| 4 | `grep -q ITemplateCacheService CLAUDE.md && echo FOUND` | 0 | ✅ pass | 100ms |
| 5 | `grep -c "^## " CLAUDE.md` | 0 | ✅ pass (5 H2 sections, >= 5 required) | 100ms |

## Deviations

None.

## Known Issues

None.

## Files Created/Modified

- `CLAUDE.md`
