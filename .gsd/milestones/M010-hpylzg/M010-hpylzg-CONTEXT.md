# M010-hpylzg: GUI 更新源配置

**Gathered:** 2026-04-29
**Status:** Ready for planning

## Project Description

在 DocuFiller 的状态栏区域提供 GUI 入口，让用户编辑更新源配置（UpdateUrl 和 Channel），支持热重载（改完立即生效，不需重启），同时在状态栏显示当前更新源类型，帮助用户区分问题出在 GitHub 还是内网服务器。

## Why This Milestone

当前切换更新源（内网 Go 服务器 vs GitHub Releases）需要用户手动编辑 appsettings.json，然后重启应用。用户是小白，不应该去翻配置文件。状态栏也没有更新源信息，出问题时无法快速判断是哪个源的问题。

## User-Visible Outcome

### When this milestone is complete, the user can:

- 点击状态栏"检查更新"按钮旁边的齿轮图标，弹出设置窗口
- 在设置窗口中看到当前更新源类型（GitHub / 内网 HTTP 地址）和通道（stable/beta）
- 编辑 UpdateUrl 和 Channel，保存后立即用新配置检查更新
- 在状态栏的更新提示中看到源类型标识

### Entry point / environment

- Entry point: 状态栏齿轮图标按钮
- Environment: WPF GUI 桌面应用
- Live dependencies involved: appsettings.json 文件读写、UpdateService 热重载

## Completion Class

- Contract complete means: IUpdateService.ReloadSource 可被调用并立即生效，appsettings.json 正确写回
- Integration complete means: 齿轮按钮→弹窗→编辑→保存→热重载→状态栏更新，全流程贯通
- Operational complete means: dotnet build 通过，现有更新流程不受影响

## Final Integrated Acceptance

To call this milestone complete, we must prove:

- 用户点击齿轮→弹窗→修改 URL→保存→状态栏立即显示新源类型，appsettings.json 已更新
- 修改为空 URL 后保存→热重载切换到 GitHub 模式→检查更新走 GitHub
- 现有"检查更新"按钮、启动时自动检查、弹窗确认流程无回归

## Architectural Decisions

### UpdateService 热重载方案

**Decision:** 在 IUpdateService 接口新增 `ReloadSource(string updateUrl, string channel)` 方法，运行时重建 UpdateManager 的 IUpdateSource，Singleton 生命周期不变。

**Rationale:** UpdateService 构造函数一次性决定 SimpleWebSource/GithubSource，不改 DI 生命周期最简单。ReloadSource 接收新参数，重建 IUpdateSource，后续 CheckForUpdatesAsync 自动用新源。

**Alternatives Considered:**
- 改为 Transient + 每次从 IConfiguration 重新构建 — 过于激进，所有依赖方都要改
- 只写文件不改运行时状态，要求重启 — 用户明确要求热重载

### appsettings.json 写回方案

**Decision:** 使用 System.Text.Json.Nodes.JsonNode 解析 → 修改 Update:UpdateUrl 和 Update:Channel 节点 → 写回文件。

**Rationale:** JSON 无注释（JSONC 不是标准），System.Text.Json 已在项目中，不需要额外依赖。保留原文件格式（缩进、属性顺序）。

**Alternatives Considered:**
- 手写字符串替换 — 脆弱，JSON 格式变化就出错
- Newtonsoft.Json — 额外依赖，现有项目已移除

### 设置弹窗形态

**Decision:** 独立 WPF Window（UpdateSettingsWindow），类似 CleanupWindow 的模式。状态栏加齿轮图标按钮触发。

**Rationale:** 独立窗口简单清晰，与现有 CleanupWindow 模式一致，用户可拖动。

**Alternatives Considered:**
- 下拉弹出面板 — WPF 实现复杂（Popup 定位问题），交互体验不如独立窗口

### 状态栏源类型显示

**Decision:** 在 UpdateStatusMessage 后追加源类型标识，如"当前已是最新版本 (GitHub)"或"当前已是最新版本 (内网: 192.168.1.100:8080)"。

**Rationale:** 最小改动，复用现有 TextBlock，不增加 UI 元素。UpdateService 已有 UpdateSourceType 属性。

**Alternatives Considered:**
- 状态栏新增独立的源类型 TextBlock — 增加状态栏复杂度，视觉噪音

---

> See `.gsd/DECISIONS.md` for the full append-only register of all project decisions.

## Error Handling Strategy

- **appsettings.json 写入失败**（只读、被占用、权限不足）→ 弹窗提示"保存配置失败：{原因}"，不关闭设置窗口，用户可重试或取消
- **输入验证** — UpdateUrl 非空但不是合法 HTTP/HTTPS URL → 提示"请输入有效的 HTTP 地址"；Channel 只允许 stable/beta/空值
- **热重载后检查更新失败**（新 URL 不可达）→ 状态栏正常显示"检查更新失败"，不影响应用其他功能
- **并发保护** — 检查更新进行中时禁止打开设置弹窗（CanCheckUpdate 互斥）

## Risks and Unknowns

- 无重大技术风险。UpdateService 热重载路径明确，appsettings.json 写回是成熟操作。

## Existing Codebase / Prior Art

- `Services/UpdateService.cs` — 现有 UpdateService 实现，构造函数决定源，Singleton
- `Services/Interfaces/IUpdateService.cs` — 现有接口，有 UpdateSourceType 属性
- `ViewModels/MainWindowViewModel.cs` — 现有更新状态管理、CheckUpdateAsync、InitializeUpdateStatusAsync
- `MainWindow.xaml` — 现有状态栏布局（版本号、状态消息、检查更新按钮）
- `DocuFiller/Views/CleanupWindow.xaml(.cs)` — 现有弹窗参考模式
- `appsettings.json` — 当前 Update 配置节

## Relevant Requirements

- R029 — UI 切换更新源和通道（从 deferred 激活）
- R044 — UpdateService 热重载
- R045 — 状态栏更新源类型显示
- R046 — 现有更新流程不受影响

## Scope

### In Scope

- IUpdateService 新增 ReloadSource 方法
- UpdateService 实现热重载 + appsettings.json 写回
- 状态栏齿轮图标按钮
- UpdateSettingsWindow 独立弹窗（编辑 UpdateUrl/Channel）
- 状态栏 UpdateStatusMessage 追加源类型标识

### Out of Scope / Non-Goals

- CLI 的更新配置修改（CLI 用户直接改 appsettings.json）
- appsettings.json 中其他配置项的 GUI 编辑
- 自动检测内网更新服务器可用性

## Technical Constraints

- UpdateService 保持 Singleton 生命周期
- 使用 System.Text.Json.Nodes（已在项目依赖中）写回 appsettings.json
- 弹窗风格与 CleanupWindow 一致

## Integration Points

- `UpdateService` ← `UpdateSettingsWindow` 通过 `IUpdateService.ReloadSource` 调用
- `MainWindowViewModel` — 新增 OpenUpdateSettingsCommand，连接齿轮按钮到弹窗
- `MainWindow.xaml` — 状态栏新增齿轮按钮

## Testing Requirements

- dotnet build 编译通过
- 手动验证：修改配置→热重载→状态栏更新→检查更新用新源
- 手动验证：现有"检查更新"按钮流程无回归

## Acceptance Criteria

### S01: UpdateService 热重载 + 配置写回
- ReloadSource 调用后 CheckForUpdatesAsync 使用新源
- appsettings.json 中 Update:UpdateUrl 和 Update:Channel 正确更新
- 现有 CheckUpdateAsync / InitializeUpdateStatusAsync 无回归

### S02: 更新设置弹窗 + 状态栏源类型显示
- 齿轮按钮可见且可点击
- 弹窗显示当前 UpdateUrl、Channel、源类型
- 保存后立即生效，状态栏显示新源类型
- 输入验证提示正常工作
- 检查更新中时齿轮按钮禁用

## Open Questions

- None
