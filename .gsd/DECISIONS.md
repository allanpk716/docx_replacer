# Decisions Register

<!-- Append-only. Never edit or remove existing rows.
     To reverse a decision, add a new row that supersedes it.
     Read this file at the start of any planning or research phase. -->

| # | When | Scope | Decision | Choice | Rationale | Revisable? | Made By |
|---|------|-------|----------|--------|-----------|------------|---------|
| D003 | M003-g1w88x | convention | 产品需求和技术架构文档归属位置 | 从 .trae/documents/ 迁移到 docs/ | .trae/ 是 Trae IDE 的约定目录，文档应与项目其他文档统一存放在 docs/ 下 | No | collaborative |
| D004 | M003-g1w88x | scope | JSON 编辑器文档处理 | 不迁移，直接删除 | JSON 编辑器功能已从代码中移除，对应文档无保留价值 | No | human |
| D005 | M003-g1w88x | scope | 更新机制文档策略 | 不更新版本管理、外部配置、部署指南相关文档 | 用户明确要求更新机制不写入文档 | No | human |
| D006 | M003-g1w88x | convention | 技术架构文档深度 | 保持详细风格，包含完整 C# 接口定义、数据模型代码和 Mermaid 图 | 现有文档的详细程度被用户认可，开发者文档需要足够的技术细节 | No | collaborative |
| D001 |  | architecture | Excel 格式自动检测策略 | 读取第一个非空行的第一列内容，匹配 #xxx# 格式则为两列模式，否则为三列模式 | 利用已有的关键词格式约定做检测，零配置、最小侵入性。用户明确认可。 | Yes | collaborative |
| D002 |  | architecture | 检测逻辑封装位置 | 在 ExcelDataParserService 内部新增私有方法 DetectExcelFormat，不修改 IExcelDataParser 接口签名 | 格式检测是解析内部实现细节，调用方无感知。保持接口稳定。 | Yes | agent |
| D007 |  | architecture | JSON 数据源全部清理，Excel 成为唯一数据输入方式 | 移除 DataParserService、IDataParser 及所有 JSON 数据解析代码 | 用户确认不再使用 JSON 数据源，只保留 Excel。清理后 DocumentProcessorService 的 JSON 分支整体移除，构造函数去掉 IDataParser 参数。 | No | collaborative |
| D008 |  | scope | 在线更新功能全套移除 | 删除所有更新相关代码、Models、ViewModels、Views、External 文件、csproj PreBuild 门禁 | 在线更新依赖外部 update-client.exe 和更新服务器，不再需要。PreBuild 门禁阻止在缺少外部文件时的构建，是最影响开发的阻碍。 | No | human |
| D009 |  | scope | 转换器窗口、KeywordEditorUrl、Tools 目录一并清理 | 全部删除 | 转换器做 JSON→Excel 转换，JSON 清理后无意义。KeywordEditorUrl 指向内网 Web 服务，JSON 编辑器废弃后无用。Tools 目录 10 个诊断工具是历史遗留。 | No | human |
