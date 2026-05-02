---
estimated_steps: 12
estimated_files: 10
skills_used: []
---

# T01: 撰写 DocuFiller 产品需求文档并迁移到 docs/

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

## Inputs

- `.trae/documents/DocuFiller产品需求文档.md`
- `Services/Interfaces/IDocumentProcessor.cs`
- `Services/Interfaces/IExcelDataParser.cs`
- `Services/Interfaces/IDocumentCleanupService.cs`
- `Services/Interfaces/IExcelToWordConverter.cs`
- `Services/ContentControlProcessor.cs`
- `Services/CommentManager.cs`
- `ViewModels/MainWindowViewModel.cs`
- `Models/ContentControlData.cs`
- `Models/FormattedCellValue.cs`
- `Models/CleanupFileItem.cs`

## Expected Output

- `docs/DocuFiller产品需求文档.md`

## Verification

test -f docs/DocuFiller产品需求文档.md && grep -c "^## " docs/DocuFiller产品需求文档.md | grep -qE "^[0-9]+$" && grep -q "Excel" docs/DocuFiller产品需求文档.md && grep -q "审核清理" docs/DocuFiller产品需求文档.md && grep -q "页眉" docs/DocuFiller产品需求文档.md && grep -q "批注" docs/DocuFiller产品需求文档.md && grep -q "转换" docs/DocuFiller产品需求文档.md
