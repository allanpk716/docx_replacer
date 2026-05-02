# M010-hpylzg: GUI 更新源配置

**Vision:** 在状态栏提供 GUI 入口编辑更新源配置（UpdateUrl 和 Channel），支持热重载立即生效，状态栏显示当前更新源类型，让用户不需要手动编辑 appsettings.json 就能切换内网/GitHub 更新源。

## Success Criteria

- 状态栏显示更新源类型标识（GitHub 或内网地址）
- 齿轮图标按钮点击弹出设置窗口，可编辑 UpdateUrl 和 Channel
- 保存配置后立即生效（热重载），不需要重启应用
- 配置同时持久化到 appsettings.json
- 现有更新检查流程无回归

## Slices

- [x] **S01: S01** `risk:medium` `depends:[]`
  > After this: 调用 ReloadSource("http://192.168.1.100:8080", "beta") 后 UpdateSourceType 变为 "HTTP"，EffectiveUpdateUrl 变为 "http://192.168.1.100:8080/beta/"，appsettings.json 中 Update 节已更新

- [x] **S02: S02** `risk:low` `depends:[]`
  > After this: 用户点击状态栏齿轮图标→弹出设置窗口显示当前源类型和配置→修改 URL 保存后状态栏立即显示新源类型，检查更新走新源

## Boundary Map

### S01 → S02

Produces:
- `IUpdateService.ReloadSource(string updateUrl, string channel)` — 热重载方法，运行时重建 UpdateManager
- `UpdateService.EffectiveUpdateUrl` — 更新后返回当前生效的 URL（含通道路径）
- `UpdateService.UpdateSourceType` — 更新后返回 "HTTP" 或 "GitHub"
- appsettings.json 中 Update:UpdateUrl 和 Update:Channel 节点已持久化

Consumes:
- nothing (first slice)

### S02 (consumes from S01)

Produces:
- `UpdateSettingsWindow.xaml(.cs)` — 独立弹窗，编辑 UpdateUrl/Channel，调用 ReloadSource
- MainWindow.xaml 状态栏齿轮按钮
- MainWindowViewModel.OpenUpdateSettingsCommand
- UpdateStatusMessage 追加源类型标识（"(GitHub)" 或 "(内网: 地址)"）

Consumes from S01:
- `IUpdateService.ReloadSource` — 保存时调用
- `UpdateService.UpdateSourceType` / `EffectiveUpdateUrl` — 显示当前源信息
