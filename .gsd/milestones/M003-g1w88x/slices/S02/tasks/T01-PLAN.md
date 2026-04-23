---
estimated_steps: 14
estimated_files: 6
skills_used: []
---

# T01: 重写 README.md 反映完整功能列表、14 个服务接口、准确项目结构和使用方法

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

## Inputs

- `README.md — 当前需要更新的文件`
- `docs/DocuFiller产品需求文档.md — S01 产出的权威产品需求文档`
- `docs/DocuFiller技术架构文档.md — S01 产出的权威技术架构文档`
- `App.xaml.cs — DI 注册信息来源`
- `Services/Interfaces/ — 所有接口定义`

## Expected Output

- `README.md — 更新后的完整 README`

## Verification

grep -c "^## " README.md` returns >= 8; `grep -c "I[A-Z]" README.md` returns >= 14 interface mentions; `grep -q "IDocumentCleanupService" README.md` && `grep -q "IExcelToWordConverter" README.md` && `grep -q "三列" README.md`
