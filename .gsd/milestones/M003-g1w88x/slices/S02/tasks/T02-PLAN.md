---
estimated_steps: 19
estimated_files: 4
skills_used: []
---

# T02: 更新 CLAUDE.md 的服务层架构、数据模型、Excel 处理路径和开发指南

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

## Inputs

- `CLAUDE.md — 当前需要更新的文件`
- `docs/DocuFiller技术架构文档.md — S01 产出的权威技术架构文档`
- `README.md — T01 刚更新的 README，可交叉验证`
- `App.xaml.cs — DI 注册信息来源`
- `Services/Interfaces/ — 所有接口定义`

## Expected Output

- `CLAUDE.md — 更新后的完整 CLAUDE.md`

## Verification

grep -c "I[A-Z]" CLAUDE.md` returns >= 14 interface mentions; `grep -q "IDocumentCleanupService" CLAUDE.md` && `grep -q "IExcelToWordConverter" CLAUDE.md` && `grep -q "ITemplateCacheService" CLAUDE.md`; `grep -c "^## " CLAUDE.md` returns >= 5
