# M009-q7p4iu: GitHub CI/CD 发布 + 多源更新提醒

**Vision:** 为 DocuFiller 建立 GitHub CI/CD 发布流水线和多源更新提醒体验。打 v* tag 自动构建发布到 GitHub Release，应用启动时智能选择更新源（内网 Go 服务器优先，GitHub Releases 备选），GUI 和 CLI 都提供对应的更新提示和操作能力。

## Success Criteria

- 打 v* tag 推送后 GitHub Release 自动创建，包含 Setup.exe + Portable.zip + .nupkg + releases.win.json
- UpdateUrl 为空时 UpdateService 使用 GitHubSource，非空时使用 HTTP URL
- GUI 状态栏正确显示三种更新状态（未配置源/有新版本/便携版提示）
- CLI update 命令 JSONL 输出正确，--yes 执行下载应用重启
- CLI 其他命令在 actionable 时追加 update 类型 JSONL 行
- dotnet build 通过，现有测试不被破坏

## Slices

- [x] **S01: S01** `risk:high` `depends:[]`
  > After this: After this: 打 v1.0.0 tag 推送到 GitHub，Actions 自动构建，Release 页面出现 Setup.exe + Portable.zip + .nupkg + releases.win.json

- [x] **S02: S02** `risk:medium` `depends:[]`
  > After this: After this: UpdateUrl 为空时自动走 GitHub Releases 检查更新；UpdateUrl 有值时走内网 Go 服务器；便携版运行时 IsInstalled 返回 false

- [x] **S03: S03** `risk:low` `depends:[]`
  > After this: After this: GUI 启动后状态栏常驻显示更新状态——未配置更新源/有新版本可用/便携版不支持自动更新；点击提示走现有弹窗更新流程

- [x] **S04: S04** `risk:low` `depends:[]`
  > After this: After this: DocuFiller.exe update 输出当前版本和最新版本的 JSONL 信息；--yes 确认后下载应用重启；其他命令在 actionable 时追加 update 行

## Boundary Map

### S01 → S02

Produces:
- .github/workflows/build-release.yml — GitHub Actions workflow 文件
- Velopack 产物上传到 GitHub Release（.nupkg + releases.win.json 供 GitHubSource 使用）

Consumes:
- nothing（独立于代码改动）

### S02 → S03

Produces:
- UpdateService 多源支持（GitHubSource 备选路径）
- IUpdateService 不变签名，IsUpdateUrlConfigured 语义扩展
- UpdateManager.IsInstalled 检测（便携版判断）

Consumes:
- nothing（内部实现变更）

### S02 → S04

Produces:
- IUpdateService.CheckForUpdatesAsync() 可在 CLI 模式调用
- UpdateService 多源支持供 CLI 使用

Consumes:
- nothing（同一服务）

### S03

Produces:
- GUI 状态栏常驻提示组件（三种状态：未配置源/有新版本/便携版）
- 状态栏点击 → 现有 CheckUpdateAsync 流程

Consumes from S02:
- IUpdateService.IsUpdateUrlConfigured — 判断是否有更新源
- IUpdateService.CheckForUpdatesAsync() — 检查新版本
- UpdateManager.IsInstalled — 判断是否安装版

### S04

Produces:
- UpdateCommand 类（ICliCommand 实现）
- JsonlOutput.WriteUpdate() 方法
- CliRunner 注册 update 子命令
- CLI 命令后更新提醒逻辑

Consumes from S02:
- IUpdateService.CheckForUpdatesAsync() — 检查新版本
- IUpdateService.DownloadUpdatesAsync() — 下载更新
- IUpdateService.ApplyUpdatesAndRestart() — 应用更新重启
- IUpdateService.IsUpdateUrlConfigured — 判断更新源状态
