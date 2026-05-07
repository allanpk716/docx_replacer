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
| D025 | M009-q7p4iu | architecture | UpdateService 多源切换策略 | UpdateUrl 非空 → HTTP URL 走内网 Go 服务器；UpdateUrl 为空 → GitHubSource 走 GitHub Releases。GitHub 只走 stable 通道。 | 公司用户访问 GitHub 不顺畅，内网 Go 服务器是首选更新源。GitHub Releases 作为外网用户的备选通道。不需要同时检查多个源，按配置选一个即可。 | Yes | collaborative |
| D026 | M009-q7p4iu | convention | CLI update 命令交互方式 | 纯 JSONL 输出 + --yes 参数确认执行，无交互式 Y/N | CLI 场景可能用于批处理脚本，交互式确认会打断工作流。JSONL 保持输出格式一致性。 | Yes | collaborative |
| D027 | M009-q7p4iu | architecture | IUpdateService 接口兼容策略 | 不改接口签名，IsUpdateUrlConfigured 语义扩展为"有任一更新源可用"（内网 Go 或 GitHub Releases 都算） | 内网 Go 服务器和 GitHub Releases 都算"有更新源"，调用方不需要知道具体走哪个源。最小化改动范围，避免连锁修改。 | Yes | agent |
| D028 | M009-q7p4iu | scope | GitHub Release 通道策略 | GitHub Release 只分发 stable 版本（Velopack 默认 win 通道），beta 继续走内网 Go 服务器 | 公司大部分用户访问 GitHub 不顺畅，beta 测试在内网进行。GitHub 对外只发稳定版，减少维护复杂度。 | Yes | collaborative |
| D029 | M009-q7p4iu | scope | 只支持安装版自动更新 | 只提供安装版（Setup.exe）的自动更新支持，便携版有明确提示告知用户使用安装版以获得自动更新能力 | 统一更新体验，避免便携版无法自更新导致的用户困惑。Velopack 自更新机制依赖安装版。 | Yes | collaborative |
| D030 | M010-hpylzg | architecture | UpdateService 热重载方案 | 在 IUpdateService 接口新增 ReloadSource(string updateUrl, string channel) 方法，运行时重建 IUpdateSource，Singleton 生命周期不变 | UpdateService 构造函数一次性决定 SimpleWebSource/GithubSource。不改 DI 生命周期最简单，ReloadSource 接收新参数重建 IUpdateSource，后续 CheckForUpdatesAsync 自动用新源。改为 Transient 过于激进。 | No | collaborative |
| D031 | M010-hpylzg | pattern | 更新设置弹窗形态 | 独立 WPF Window（UpdateSettingsWindow），状态栏齿轮图标按钮触发 | 独立窗口简单清晰，与 CleanupWindow 模式一致。下拉面板在 WPF 中实现复杂（Popup 定位问题）。 | No | collaborative |
| D032 | M010-hpylzg | pattern | 状态栏更新源类型显示方式 | 在 UpdateStatusMessage 后追加源类型标识，如"当前已是最新版本 (GitHub)"或"当前已是最新版本 (内网: 192.168.1.100:8080)" | 最小改动，复用现有 TextBlock，不增加 UI 元素。UpdateService 已有 UpdateSourceType 属性。新增独立 TextBlock 会增加状态栏复杂度。 | Yes | collaborative |
| D033 | M011-ns0oo0/S01 | architecture | URL 回显数据源选择 | 直接从 IConfiguration 读取 Update:UpdateUrl 原始值，不从 EffectiveUpdateUrl 剥离 | EffectiveUpdateUrl 是拼接了通道路径后缀的完整 URL（如 http://host/stable/），剥离逻辑容易出边界问题（尾部斜杠、大小写等）。IConfiguration 中的 Update:UpdateUrl 就是用户输入的原始值，直接读取更可靠、更简单。 | No | agent |
| D034 | M011-ns0oo0/S02 | architecture | 下载进度弹窗形态 | 独立模态 WPF Window（DownloadProgressWindow），阻塞主窗口 | 用户明确要求"类似安装进度"的独立弹窗，阻塞式等待。非模态可能让用户误操作主窗口。进度数据来自 Velopack Action<int> 回调 + VelopackAsset.Size 计算速度和剩余时间。 | No | agent |
| D035 |  | layout | 拖放区域从独立 Border 改为路径 TextBox 内支持 | 去掉独立拖放 Border，路径 TextBox 设置 AllowDrop=True | 两个拖放 Border 共占约 150px 垂直空间，改为单行路径栏可大幅节省空间。TextBox AllowDrop 是 WPF 标准做法。 | Yes | collaborative |
| D036 |  | layout | GroupBox 替换方案 | 去掉 GroupBox，用 TextBlock 标签 + Separator 分隔线 | 三个 GroupBox 的 header 行和内边距共约 120px，替换为标签+分隔线后只需约 30px，节省 90px。 | Yes | collaborative |
| D037 |  | layout | 窗口默认尺寸 | Width=900 Height=550，MinWidth=800 MinHeight=500 | 紧凑布局后内容不需要大窗口。900x550 在 1366x768（减任务栏 ~728px 可用）下充裕显示。 | Yes | collaborative |
| D038 |  | layout | 全局字号调整范围 | TabControl 标题 14px、标签 13px、正文 12px | 当前 FontSize=16 是空间浪费主因之一。降至 12-14px 后内容密度显著提升且仍清晰可读。 | Yes | collaborative |
| D039 |  | architecture | update-config.json 存储路径 | %USERPROFILE%\.docx_replacer\update-config.json | 用户明确要求 ~/.docx_replacer/ 目录。完全独立于 Velopack 安装目录，安装/更新/卸载都不会触及。 | Yes | human |
| D040 |  | scope | 旧路径 update-config.json 不做自动迁移 | 不迁移旧路径配置，用户重新配置 | 用户明确表示不需要迁移，重新配置即可。迁移逻辑增加复杂度且只需执行一次。 | No | human |
| D041 |  | architecture | 标题栏自定义按钮实现方式 | 使用 WindowChrome 将标题栏扩展到客户区，在 DockPanel 顶部放自定义标题栏含图钉按钮 | 标准 WPF 标题栏无法添加自定义按钮。WindowChrome 是最轻量方案，保留系统窗口行为（拖动、缩放、Aero Snap），不需要完全自绘窗口 | Yes | collaborative |
| D042 |  | pattern | TextBox 拖放事件路由策略 | 将 Drop/DragOver/DragEnter/DragLeave 改为 PreviewDrop/PreviewDragOver/PreviewDragEnter/PreviewDragLeave 隧道事件 | WPF TextBox 内置拖放处理在冒泡阶段拦截外部文件拖放。隧道事件先于内置处理触发，e.Handled=true 阻止拦截。Border（清理区域）无内置处理，保持冒泡事件不变。 | No | agent |
| D043 | M018 | scope | 便携版自动更新支持策略 | 移除所有便携版更新阻断，便携版和安装版走完全相同的更新代码路径，推翻 D029 | Velopack 设计上支持便携版自更新（IsPortable 属性、Portable.zip 包含 Update.exe），当前的阻断是应用层面不必要的守卫。用户明确要求便携版享有同等更新能力。 | No | collaborative |
| D044 | M018 | scope | 推翻 D029（只支持安装版自动更新） | 推翻 D029，便携版与安装版享有完全相同的自动更新能力 | 用户确认便携版也应该支持自动更新。Velopack 技术上支持，无需限制。 | No | collaborative |
| D045 |  | architecture | 推翻 D029（"只提供安装版的自动更新支持"） | 便携版享有与安装版完全一致的自动更新能力（检查→下载→应用→重启），不再有任何基于 IsInstalled 的流程阻断 | Velopack SDK 原生支持便携版自更新（IsPortable 属性、Portable.zip 包含 Update.exe）。D029 的原始假设（便携版不支持自动更新）已被证实不成立。移除所有阻断逻辑后，便携版走相同的更新代码路径，无需额外维护。 | Yes | agent |
| D046 |  | pattern | ProgressBar 模板修复方式 | 在 ModernProgressBarStyle 模板中添加 PART_Indicator（WPF 标准命名），保持圆角视觉风格 | WPF ProgressBar 通过 PART_Indicator 显示填充进度。当前模板只有 PART_Track 导致填充不可见。标准修复方式，无需自定义动画。 | No | collaborative |
| D047 |  | pattern | 图标生成方式 | Python + Pillow 程序化绘制（文档页面 + 填充箭头意象），导出多尺寸 .ico | 不依赖外部设计工具或在线服务，可重复生成。DocuFiller 定位（文档填充）有明确视觉隐喻。 | No | collaborative |
| D048 | M021 | architecture | MainWindowViewModel 拆分策略 | FillViewModel（CT.Mvvm）+ UpdateStatusViewModel（CT.Mvvm）+ 协调器 MainWindowVM（手写 INPC） | FillViewModel 承载关键词替换 Tab 全部业务逻辑（~700行），UpdateStatusViewModel 承载更新状态管理（~200行），MainWindowVM 降为纯协调器（~400行）。CT.Mvvm 减少样板代码，与 M020 迁移的子 VM 一致。MainWindowVM 保留手写 INPC 遵循渐进迁移策略。 | Yes | collaborative |
| D049 | M021 | architecture | 清理 Tab 复用 CleanupViewModel | Tab 2 DataContext 改为绑定 CleanupViewModel，删除 MainWindowVM 清理代码 | MainWindowVM 和 CleanupViewModel 有两套几乎相同的清理逻辑（文件列表、处理、进度），复用消除 ~200 行重复代码。CleanupViewModel 需扩展输出目录属性。 | Yes | collaborative |
| D050 | M021 | convention | 不再维护 CLAUDE.md | 删除 CLAUDE.md，产品需求文档和 README.md 作为唯一项目文档 | CLAUDE.md 维护成本高且容易与代码脱节，用户决定不再投入。 | No | human |
| D051 | M021 | architecture | 拖放逻辑实现方式 | DragDropBehavior AttachedProperty 统一处理文件拖放，15 个事件处理器消除 | 4 组 Preview 事件 × 3 目标 = 12 个方法逻辑几乎相同。Behavior 模式是 WPF AttachedProperty 标准做法，可复用于未来窗口。使用 Preview 隧道事件绕过 TextBox 内置拖放拦截（D042）。 | Yes | collaborative |
| D052 | M023 | architecture | 更新协议选择 | 所有接入的应用统一使用 Velopack 打包和更新协议 | Velopack 已解决 delta 更新、签名验证、安装/便携模式自更新等难题，DocuFiller 已深度集成，Go/Python 可用 vpk pack 打包。避免双协议或自定义协议的巨大工作量。 | No | collaborative |
| D053 | M023 | architecture | 元数据存储方案 | SQLite 存元数据，文件系统存 artifacts | Web UI 需要结构化查询（应用列表、版本排序、备注搜索）。SQLite 单文件、零运维、Go 标准库自带驱动，Windows Server 不需要额外安装。 | Yes — 如果以后多实例部署需要共享数据库 | collaborative |
| D054 | M023 | architecture | 前端技术栈和部署方式 | Vue 3 + Vite，编译产物通过 Go embed 内嵌到二进制 | 用户选择 Vue。Go embed 实现单文件部署，符合 Go 单二进制部署哲学。 | No | collaborative |
| D055 | M023 | architecture | 认证方案 | 启动参数配置管理密码，Web UI 登录发 JWT session cookie，API 同时支持 Bearer token | 内网单用户场景不需要多用户系统。兼容 Bearer token 确保现有 build-internal.bat 不需要改认证方式。 | Yes — 如果以后需要多用户/角色 | collaborative |
| D056 | M023 | architecture | 多应用 URL 结构 | /{appId}/{channel}/releases.{os}.json | URL 层级清晰，按应用隔离，方便以后加权限控制。用户明确选择此方案。 | No | collaborative |
| D057 | M023 | scope | DocuFiller 客户端迁移策略 | 直接在 appsettings.json 改 URL 指向新路径，不兼容旧 URL 格式 | 用户明确表示直接改配置即可，不需要旧 URL 兼容过渡期。URL 是从 appsettings.json 读取的，客户端代码不用改。 | No | collaborative |
| D058 | M023 | pattern | 应用注册方式 | 第一次上传时自动注册，从 releases feed 的 PackageId 提取应用标识 | 降低接入成本，新应用不需要管理操作就能开始使用更新服务器。用户明确选择此方案。 | Yes — 如果以后需要应用审核/审批流程 | collaborative |
| D059 | M023 | architecture | 通道模型 | 不硬编码 stable/beta，通道名由上传路径动态决定 | 不同应用可能需要不同的通道策略（如 nightly、rc），不应由服务器硬编码限制。 | No | collaborative |
| D060 |  | architecture | 更新检查进度动画样式 | 旋转圆圈动画（Spinner） | 用户选择。旋转动画过程感更强，用户更熟悉。脉冲圆点过于含蓄。 | Yes | human |
| D061 |  | architecture | 启动时更新检查动画时序 | 立刻显示动画（不等 5 秒延迟） | 用户选择。当前 5 秒延迟期间完全无反馈是主要痛点，立刻显示动画让用户知道程序在准备。 | Yes | human |
