# S05: R028 启动自动检查更新 + 通知徽章 — UAT

**Milestone:** M021
**Written:** 2026-05-04T11:45:52.501Z

# UAT: S05 R028 启动自动检查更新 + 通知徽章

## UAT Type
Contract-level acceptance — 行为通过单元测试验证，无真实更新服务器交互。

## Preconditions
- DocuFiller 已构建（`dotnet build -c Release`）
- appsettings.json 中 `Update:UpdateUrl` 配置为有效更新源 URL（内网或 GitHub）
- 或者 `Update:UpdateUrl` 为空/无效（测试"无更新源"路径）

## Test Cases

### TC-01: 应用启动 5 秒后自动检查更新（有更新）
1. 启动 DocuFiller GUI
2. 等待 6 秒（5 秒延迟 + 检查耗时）
3. **预期**: 状态栏显示更新状态文本（"有新版本可用，点击更新"或"当前已是最新版本"）
4. **预期**: 无异常弹窗，无启动阻塞

### TC-02: 有新版本时⚙按钮显示红色圆点
1. 确保更新服务器有比当前版本更新的版本
2. 启动 DocuFiller GUI，等待自动检查完成
3. **预期**: ⚙设置按钮右上角出现红色圆点徽章
4. **预期**: 徽章仅在有新版本时显示

### TC-03: 无新版本时无红色圆点
1. 确保当前已是最新版本
2. 启动 DocuFiller GUI，等待自动检查完成
3. **预期**: ⚙设置按钮无红色圆点
4. **预期**: 状态栏显示"当前已是最新版本"

### TC-04: 更新源未配置时状态栏无更新提示
1. 将 appsettings.json 中 `Update:UpdateUrl` 设为空字符串
2. 启动 DocuFiller GUI，等待 6 秒
3. **预期**: 状态栏不显示更新状态文本
4. **预期**: 无异常，无错误弹窗

### TC-05: 手动检查更新仍独立工作
1. 启动 DocuFiller GUI
2. 在 5 秒延迟内点击"检查更新"按钮
3. **预期**: 手动检查立即执行，不等待自动检查
4. **预期**: 结果弹窗正常显示

### TC-06: 自动检查失败静默处理
1. 配置无效的更新源 URL（如 `http://invalid-host:9999`）
2. 启动 DocuFiller GUI，等待自动检查完成
3. **预期**: 状态栏显示"检查更新失败"（红色文本）
4. **预期**: 无异常弹窗阻塞 UI
5. **预期**: Logs/ 目录中有错误日志记录

## Not Proven By This UAT
- 真实 Velopack 更新下载和安装流程（由 M007 E2E 测试覆盖）
- ViewModel 销毁时 CancellationToken 取消的实际 UI 场景
- 高延迟网络环境下的超时行为
