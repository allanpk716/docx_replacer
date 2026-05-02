# S02: 客户端通道支持 — UAT

**Milestone:** M008-4uyz6m
**Written:** 2026-04-24T23:55:42.154Z

# S02 UAT: 客户端通道支持

## 前置条件
- DocuFiller 已编译（dotnet build 成功）
- Go 更新服务器可用（S01 已完成）

## 测试用例

### TC-01: 默认通道（Channel 为空）
1. 确保 appsettings.json 中 `"Channel": ""`
2. 启动应用
3. **预期**: UpdateService Channel 属性为 "stable"
4. **预期**: UpdateManager 请求 URL 为 `{UpdateUrl}/stable/releases.win.json`

### TC-02: 显式 beta 通道
1. 修改 appsettings.json: `"Channel": "beta"`
2. 启动应用
3. **预期**: UpdateService Channel 属性为 "beta"
4. **预期**: UpdateManager 请求 URL 为 `{UpdateUrl}/beta/releases.win.json`

### TC-03: 显式 stable 通道
1. 修改 appsettings.json: `"Channel": "stable"`
2. 启动应用
3. **预期**: UpdateService Channel 属性为 "stable"
4. **预期**: UpdateManager 请求 URL 为 `{UpdateUrl}/stable/releases.win.json`

### TC-04: Channel 键缺失（向后兼容）
1. 从 appsettings.json Update 节中删除 Channel 键
2. 启动应用
3. **预期**: 不报错，Channel 默认为 "stable"
4. **预期**: 行为与 TC-01 一致

### TC-05: UpdateUrl 为空
1. 设置 appsettings.json `"UpdateUrl": ""`
2. 启动应用
3. **预期**: IsUpdateUrlConfigured 返回 false
4. **预期**: "检查更新" 按钮灰显
5. **预期**: 日志输出"更新源 URL 未配置"警告

### TC-06: URL 斜杠处理
1. 设置 `"UpdateUrl": "http://server:8080"`（无末尾斜杠）
2. **预期**: 构造的 URL 为 `http://server:8080/stable/`
3. 设置 `"UpdateUrl": "http://server:8080/"`（有末尾斜杠）
4. **预期**: 构造的 URL 为 `http://server:8080/stable/`（无双斜杠）

### TC-07: 全量回归测试
1. 运行 `dotnet test`
2. **预期**: 168 tests passed, 0 failed
