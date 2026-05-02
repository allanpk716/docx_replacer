# S01: 去掉 ExplicitChannel，修复更新检测逻辑 — UAT

**Milestone:** M014-jkpyu6
**Written:** 2026-05-02T15:56:22.144Z

# UAT: S01 去掉 ExplicitChannel，修复更新检测逻辑

## 前置条件
- 已安装 v1.3.4（含旧 ExplicitChannel="stable" 代码）的安装版
- GitHub Releases 上存在 v1.4.0 发布版本，附带 releases.win.json
- 内网 HTTP 更新服务器 stable 和 beta 通道分别部署了对应 releases.win.json

## 测试用例

### TC1: GitHub 源更新检测
1. 启动已安装的 v1.3.4 应用
2. 点击状态栏"检查更新"按钮
3. **预期**：检测到 v1.4.0 更新可用，显示更新确认对话框
4. **验证**：查看 velopack.log，确认查找的是 releases.win.json 而非 releases.stable.json

### TC2: 内网 HTTP stable 通道更新检测
1. 在更新设置中选择内网 HTTP 模式，URL 指向 `http://server/stable/`
2. 确保 stable 通道的 releases.win.json 包含比当前版本更高的版本
3. 点击"检查更新"
4. **预期**：检测到新版本可用

### TC3: 内网 HTTP beta→stable 回退
1. 在更新设置中选择内网 HTTP 模式，URL 指向 `http://server/beta/`，通道设为 beta
2. 确保 beta 通道无可用更新（releases.win.json 无更高版本）
3. 点击"检查更新"
4. **预期**：自动回退检查 `http://server/stable/` 通道
5. **验证**：日志中显示回退到 stable 通道的检查记录

### TC4: GitHub 模式跳过回退
1. 在更新设置中选择 GitHub 模式
2. 确保 GitHub Releases 无可用更新（当前已是最新）
3. 点击"检查更新"
4. **预期**：显示"已是最新版本"，不触发通道回退逻辑

### TC5: 编译和回归安全
1. 运行 `dotnet build`
2. **预期**：0 错误
3. 运行 `dotnet test`
4. **预期**：全部通过，0 失败

### TC6: 热重载后 _baseUrl 同步
1. 在更新设置中将 URL 从 `http://server/beta/` 修改为 `http://newserver/stable/`
2. 保存设置
3. 点击"检查更新"
4. **预期**：使用新 URL 检查更新，_baseUrl 已正确更新为 `http://newserver/`

