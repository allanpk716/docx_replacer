# S01: 修复更新设置 URL 回显 — UAT

**Milestone:** M011-ns0oo0
**Written:** 2026-04-30T06:06:07.624Z

# UAT: S01 修复更新设置 URL 回显

## 前置条件

- 应用已构建完成（`dotnet build` 成功）
- `appsettings.json` 中配置了 `Update:UpdateUrl`（如 `http://172.18.200.47:30001`）和 `Update:Channel`（如 `stable`）

## 测试用例

### TC-01: HTTP 模式 URL 正确回显

1. 在 `appsettings.json` 中设置 `"UpdateUrl": "http://172.18.200.47:30001"`, `"Channel": "stable"`
2. 启动应用
3. 打开更新设置窗口
4. **预期**: URL 输入框显示 `http://172.18.200.47:30001`（无通道路径后缀）
5. **预期**: Channel 下拉框显示 `stable`
6. **预期**: 源类型显示 `HTTP`

### TC-02: GitHub 模式空 URL 回显

1. 在 `appsettings.json` 中设置 `"UpdateUrl": ""`, `"Channel": ""`
2. 启动应用
3. 打开更新设置窗口
4. **预期**: URL 输入框为空
5. **预期**: Channel 下拉框显示 `stable`（fallback 默认值）
6. **预期**: 源类型显示 `GitHub`

### TC-03: Beta 通道回显

1. 在 `appsettings.json` 中设置 `"UpdateUrl": "http://example.com"`, `"Channel": "beta"`
2. 启动应用
3. 打开更新设置窗口
4. **预期**: Channel 下拉框显示 `beta`

### TC-04: URL 含空格 Trim 处理

1. 在 `appsettings.json` 中设置 `"UpdateUrl": "  http://example.com  "`
2. 启动应用
3. 打开更新设置窗口
4. **预期**: URL 输入框显示 `http://example.com`（已 Trim，无前后空格）

### TC-05: 保存功能不受影响

1. 打开更新设置窗口
2. 修改 URL 和 Channel
3. 点击保存
4. **预期**: 设置正常保存，无报错弹窗
5. **预期**: `appsettings.json` 中的值已更新

## 自动化验证（已通过）

```
dotnet build — 0 errors
dotnet test — 203/203 passed
UpdateSettingsViewModelTests — 11/11 passed
```
