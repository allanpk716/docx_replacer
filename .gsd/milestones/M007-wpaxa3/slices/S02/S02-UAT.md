# S02: 更新服务 + 状态栏 UI — UAT

**Milestone:** M007-wpaxa3
**Written:** 2026-04-24T06:02:45.259Z

**Milestone:** M007-wpaxa3
**Written:** 2026-04-24

## UAT Type

- UAT mode: artifact-driven
- Why this mode is sufficient: S02 产出的是 UI 代码和服务层代码，Velopack UpdateManager 需要真实安装上下文（应用必须通过 Velopack 安装/打包后运行才能连接更新源）。运行时端到端验证推迟到 S04。当前通过构建验证、代码审查、和测试套件证明正确性。

## Preconditions

- .NET 8 SDK 已安装
- 项目可成功编译（dotnet build）
- appsettings.json 中 Update:UpdateUrl 可配置（默认空字符串）

## Smoke Test

启动应用（WPF GUI 模式），确认主窗口底部状态栏可见，显示版本号文本和"检查更新"按钮。

## Test Cases

### 1. 状态栏 UI 验证

1. 启动应用（无 CLI 参数）
2. 观察主窗口底部
3. **Expected:** 状态栏左侧显示当前版本号（如 "v1.0.0"），右侧显示"检查更新"按钮

### 2. 更新源未配置 — 按钮灰显

1. 确认 appsettings.json 中 Update:UpdateUrl 为空字符串或未配置
2. 启动应用
3. **Expected:** "检查更新"按钮灰显（IsEnabled = false），鼠标悬停无响应

### 3. 更新源已配置 — 按钮可用

1. 设置 appsettings.json 中 Update:UpdateUrl 为有效 URL（如 http://internal-server/updates）
2. 启动应用
3. **Expected:** "检查更新"按钮可点击（IsEnabled = true）

### 4. 检查更新 — 无新版本

1. 配置有效 UpdateUrl，指向与当前版本相同的 releases.win.json
2. 点击"检查更新"按钮
3. **Expected:** 显示提示框"已是最新版本"，按钮恢复可点击状态

### 5. 检查更新 — 有新版本（模拟）

1. 配置 UpdateUrl 指向包含更高版本号的 releases.win.json
2. 点击"检查更新"按钮
3. **Expected:** 弹出确认对话框，显示新旧版本号，提供确认/取消选项

### 6. 检查更新 — 网络异常

1. 配置 UpdateUrl 指向不可达地址
2. 点击"检查更新"按钮
3. **Expected:** 显示错误信息提示框，按钮恢复可点击状态

## Edge Cases

### 快速连续点击

1. 点击"检查更新"按钮
2. 在请求未完成前再次点击
3. **Expected:** 按钮在检查期间自动禁用（IsCheckingUpdate = true），防止重复调用

### 可选注入兼容性

1. 从 DI 容器移除 IUpdateService 注册
2. 启动应用
3. **Expected:** 应用正常启动，状态栏仍显示版本号，检查更新按钮灰显

## Failure Signals

- 窗口底部无状态栏或版本号不显示
- "检查更新"按钮在 UpdateUrl 已配置时仍灰显
- 点击按钮后应用无响应或崩溃
- dotnet build 失败或 dotnet test 有失败测试

## Not Proven By This UAT

- 真实的 Velopack 更新下载和安装流程（需要 Velopack 打包环境，S04 验证）
- 便携版（Portable）模式下更新服务行为
- 更新后用户配置文件保留（S04 验证）

## Notes for Tester

- Velopack UpdateManager 在非 Velopack 安装环境下（如直接 dotnet run）无法真正连接更新源，因此更新检查的网络交互需在 S04 端到端验证中完成
- 当前 UAT 聚焦于 UI 正确性和代码结构完整性
- 版本号来源为 VersionHelper.GetCurrentVersion()，该值来自 Assembly 信息
