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
| D010 | M005-3te7t8 planning | architecture | CLI 输出使用 AttachConsole(-1) P/Invoke 解决 WinExe stdout 问题 | 使用 AttachConsole(-1) P/Invoke 将 stdout 附加到父控制台，不修改 OutputType=WinExe | 保持 WinExe 可以避免 GUI 模式下控制台窗口闪屏。AttachConsole(-1) 在从 cmd/PowerShell 调用时将 stdout 附加到父控制台，双击启动时无父控制台所以不会弹窗。这是 WPF 应用添加 CLI 支持的标准方案。 | Yes | agent |
| D011 | M005-3te7t8 planning | library | CLI 参数解析使用手写简单解析器，不引入命令行解析库 | 不引入 System.CommandLine 等第三方库，使用手写 args[] 切片解析（3 个子命令、每个 2-5 个选项） | 只有 3 个子命令（fill/cleanup/inspect），每个子命令参数简单（2-5 个 --key value 对），手写解析器代码量约 100 行，不值得引入外部依赖增加包体积和维护成本。 | Yes | agent |
| D012 | M005-3te7t8 planning | architecture | JSONL 输出格式设计：统一 envelope schema | CLI 输出统一使用 System.Text.Json 序列化，每行一个 JSON 对象，envelope 含 type/status/timestamp 字段 | 统一 envelope（type/status/timestamp）让 agent 可以用统一的逻辑解析所有输出行，不需要为不同子命令维护不同的解析逻辑。System.Text.Json 是 .NET 8 内置的，不需要额外依赖。 | Yes | agent |
| D013 |  | architecture | E2E 测试项目的版本兼容策略 | 使用 ServiceCollection DI + 条件类型注册（通过 Type.GetType/AppDomain 检测 IDataParser 是否存在）代替自定义反射工厂 | DI 容器在运行时自动解析 DocumentProcessorService 构造函数参数，自然处理 8 参数（M004 后）和 9 参数（d81cd00 含 IDataParser）的差异。无需手写反射参数匹配逻辑。条件类型注册通过检测已编译类型是否存在来决定是否注册 IDataParser → DataParserService 映射。 | Yes | agent |
| D014 |  | architecture | E2E 测试项目源文件链接策略 | E2E csproj 中对已删除文件（DataParserService.cs、IDataParser.cs）使用 Condition="Exists(...)" 条件编译包含 | 当前代码不包含 DataParserService.cs/IDataParser.cs（M004 已删除），直接链接会导致编译失败。条件包含确保在 d81cd00（文件存在）和当前代码（文件不存在）上都能编译通过。 | Yes | agent |
| D015 |  | architecture | 测试数据路径发现策略 | 从测试程序集位置向上导航查找 test_data/2026年4月23日/ 目录，直到找到包含目标 Excel 文件的目录 | test_data/ 在 .gitignore 中被排除，不在 worktree 中存在。工作树位于 .gsd/worktrees/M006-rj9bue/，主仓库根目录在其上级。向上导航可覆盖 worktree（找到主仓库的 test_data/）和主仓库（直接找到 test_data/）两种场景。 | Yes | agent |
| D016 |  | library | 自动更新框架选择 | Velopack（Squirrel/Clowd.Squirrel 正统继任者） | 内网部署友好（只需 HTTP 静态文件目录），支持单 EXE、增量更新、便携模式自更新。3900+ commits 活跃维护。AutoUpdater.NET 不支持增量更新且有 .NET 8 兼容问题，手写自更新等于重造轮子。 | No | collaborative |
| D017 |  | convention | 更新源配置位置 | appsettings.json 的 Update:UpdateUrl 节点 | 和现有配置风格一致（所有应用配置都在 appsettings.json），不使用 App.config（旧更新残留将被清理）。Velopack UpdateManager 构造函数接受 URL 字符串直接传入。 | Yes — 如果需要支持多环境配置 | agent |
| D018 |  | architecture | 不做 Trimming | PublishSingleFile=true 但 PublishTrimmed=false | EPPlus 和 DocumentFormat.OpenXml 使用反射，trimming 可能导致运行时错误。内网环境下载速度不是瓶颈，大体积（80-120MB）可接受。 | Yes — 如果未来需要减小体积且能验证 trimming 安全性 | agent |
| D019 |  | pattern | 不使用 Velopack 内置更新对话框 | 自定义 WPF 弹窗处理更新确认和进度展示 | Velopack 内置对话框是 WinForms 风格，与应用 WPF 视觉风格不一致。自定义弹窗匹配现有 UI 主题。 | No | agent |
| D020 |  | library | 更新服务器技术选择 | Go 语言，放在项目子目录 update-server/ | Go 编译为单二进制无运行时依赖，内网部署简单。作为子目录先开发，后续好用再分离。只为 DocuFiller 服务，不需要多程序支持。 | Yes | collaborative |
| D021 |  | architecture | 更新服务器认证方式 | 简单 Token（启动参数配置，Bearer Header 传递） | 内网环境够用就行。上传和 promote 接口需要保护，版本列表查询不需要认证。 | Yes | collaborative |
| D022 |  | architecture | 客户端通道切换方式 | appsettings.json Update:Channel 字段，值为 stable 或 beta | 兼顾 GUI 和 CLI 模式（都读同一配置文件）。即时生效，下次检查更新时读取最新配置。向后兼容——Channel 为空默认 stable。 | Yes | collaborative |
| D023 |  | architecture | 版本保留策略 | 每通道自动保留最近 10 个版本，上传/promote 时触发清理 | 10 个版本足够回滚和增量更新，自动化不需要人工干预。 | Yes | collaborative |
| D024 |  | architecture | 更新服务器存储方案 | 文件系统存储，不用数据库 | 够用就行。数据目录下 stable/ 和 beta/ 子目录，每个目录存放 releases.win.json 和 .nupkg 文件。Go 服务器本身就是文件的搬运工。 | Yes | collaborative |
