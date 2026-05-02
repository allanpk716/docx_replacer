# M008-4uyz6m: 双通道更新系统 — Go 更新服务器 + stable/beta 通道 + 自动化发布

**Gathered:** 2026-04-24
**Status:** Ready for planning

## Project Description

为 DocuFiller 的 Velopack 自动更新系统引入 stable/beta 双通道机制。开发一个 Go 语言轻量更新服务器（放在项目子目录 update-server/），提供 REST API 管理双通道版本发布，客户端通过 appsettings.json 的 Channel 字段选择通道。

## Why This Milestone

当前 M007 实现的单通道更新只能给所有用户推同一个版本。实际发布中需要：先让少数测试用户验证 beta 版本，确认稳定后再推给所有 stable 用户。这需要一个独立的更新服务器来管理多通道版本，以及客户端的通道选择能力。

## User-Visible Outcome

### When this milestone is complete, the user can:

- 在 appsettings.json 中设置 `"Channel": "beta"` 让 DocuFiller 从 beta 通道检查更新
- 运维人员通过 API 上传新版本到 beta 通道，beta 用户立即收到更新
- 运维人员通过 promote API 将 beta 验证通过的版本一键推到 stable 通道
- 使用 `build-internal.bat stable` 或 `build-internal.bat beta` 一条命令完成编译+打包+发布

### Entry point / environment

- Entry point: appsettings.json Channel 字段 + build-internal.bat channel 参数
- Environment: 内网 Windows 桌面 + Go HTTP 服务器
- Live dependencies involved: Go 更新服务器（HTTP 静态文件 + REST API）

## Completion Class

- Contract complete means: Go 服务器编译通过，API 接口可通过 curl 验证，客户端 UpdateService 正确拼接通道 URL
- Integration complete means: 完整流程跑通——上传 beta → 客户端检测到更新 → promote stable → stable 客户端检测到更新
- Operational complete means: build-internal.bat 一条命令完成构建+上传，服务器自动清理旧版本

## Final Integrated Acceptance

To call this milestone complete, we must prove:

- Go 服务器启动后，curl 能上传到 beta 通道、promote 到 stable、查询版本列表
- DocuFiller 设置 Channel=beta 后能从 beta 通道检测到更新
- promote 后 stable 用户能检测到新版本
- build-internal.bat beta 完成构建并自动上传
- 每个通道超过 10 个版本时自动清理

## Architectural Decisions

### 更新服务器技术选择：Go

**Decision:** 使用 Go 开发独立更新服务器，放在项目子目录 update-server/

**Rationale:** Go 编译为单二进制，无运行时依赖，内网部署简单。作为子目录先开发，后续好用再考虑分离为独立仓库。当前只为 DocuFiller 服务，不需要多程序支持。

**Alternatives Considered:**
- Python — 也够用，但需要 Python 运行时，不如 Go 单二进制方便
- Node.js — 需要运行时，部署不如 Go 简洁
- 继续用 Python e2e-serve.py — 没有上传/promote API，无法管理版本

### 认证方式：简单 Token

**Decision:** 服务器启动时配置 API Token，上传/promote 接口通过 Authorization: Bearer {token} 认证

**Rationale:** 内网环境，够用就行。不需要复杂的用户管理系统。

**Alternatives Considered:**
- 无认证 — 内网环境理论上可以，但上传接口暴露风险太大
- JWT/用户系统 — 过度设计

### 通道切换方式：appsettings.json Channel 字段

**Decision:** 在 appsettings.json Update 节点下增加 Channel 字段，值为 stable 或 beta

**Rationale:** 兼顾 GUI 和 CLI 模式——两种模式都读取同一个配置文件。即时生效，下次检查更新时读取最新配置。

**Alternatives Considered:**
- UI 界面切换 — 不方便 CLI 模式
- 命令行参数 — 每次 CLI 调用都要传，不如配置文件持久

### 版本保留策略：每通道最近 10 个

**Decision:** 上传和 promote 时自动检查，超过 10 个版本删除最老的

**Rationale:** 10 个版本足够回滚和增量更新，防止磁盘无限增长。自动化做到，不需要人工干预。

**Alternatives Considered:**
- 永久保留 — 简单但磁盘会增长
- 手动清理 — 额外运维负担

## Scope

### In Scope

- Go 更新服务器（update-server/ 子目录）
  - 静态文件服务（/stable/、/beta/ 目录）
  - 上传 API（POST /api/channels/{channel}/releases）
  - Promote API（POST /api/channels/stable/promote）
  - 版本列表 API（GET /api/channels/{channel}/releases）
  - 自动版本清理（每通道保留 10 个）
  - Token 认证
- 客户端改造
  - appsettings.json 增加 Channel 字段
  - UpdateService 根据通道拼接 URL
  - 向后兼容（Channel 为空默认 stable）
- 发布脚本改造
  - build-internal.bat 支持 channel 参数
  - 构建后自动调用上传 API
- 端到端验证

### Out of Scope / Non-Goals

- 多程序支持（只为 DocuFiller 服务）
- Web 管理界面
- 用户管理系统（简单 Token 够用）
- 后台自动检查更新通知（R028 仍 deferred）
- 代码签名
- Docker 部署
- CI/CD pipeline 集成（手动脚本够用）

## Technical Constraints

- Go 服务器：单二进制，无外部依赖，文件系统存储（不用数据库）
- 客户端：不修改 IUpdateService 接口签名（向后兼容），只修改 UpdateService 内部实现
- 发布脚本：BAT 脚本不含中文字符
- 现有 162 个测试必须全部通过

## Integration Points

- **Go 更新服务器** — 静态文件服务供 Velopack UpdateManager 消费，REST API 供构建脚本和运维使用
- **appsettings.json** — Update:UpdateUrl + Update:Channel 两个字段
- **UpdateService** — CreateUpdateManager() 根据通道拼接 URL：`{UpdateUrl}/{Channel}/`
- **build-internal.bat** — vpk pack 后调用 curl POST 上传到指定通道
- **Velopack UpdateManager** — 客户端仍然使用标准 Velopack API，只是 URL 指向不同通道目录

## Error Handling Strategy

- **上传失败**（网络/认证/服务器）：curl 返回非 0 退出码，build-internal.bat 报错并停止
- **通道不存在**：服务器返回 404，客户端 Velopack 会报告"无法连接更新源"
- **promote 版本不存在**：服务器返回 404，提示版本在源通道中不存在
- **Token 无效**：服务器返回 401
- **Channel 配置为空**：客户端默认使用 stable
- **服务器未启动**：客户端检查更新时报"无法连接到更新服务器"（已有行为不变）

## Risks and Unknowns

- **Velopack releases.win.json 格式**：promote 时需要正确合并/覆盖 releases.win.json，格式必须与 vpk pack 产出的一致
- **Go 服务器在 Windows 上的表现**：需要验证文件路径和文件锁在 Windows 上正常工作
- **现有 UpdateService 改造影响**：修改 CreateUpdateManager() 的 URL 拼接逻辑，必须确保单通道场景（旧配置）不受影响

## Relevant Requirements

- R029 — 客户端双通道支持
- R030 — Go 更新服务器静态文件服务
- R031 — 上传 API
- R032 — Promote API
- R033 — 版本列表查询 API
- R034 — 自动版本清理
- R035 — build-internal.bat 通道参数
- R036 — 端到端验证

## Existing Codebase / Prior Art

- `Services/UpdateService.cs` — 现有更新服务，CreateUpdateManager() 接受 URL 字符串，改造点
- `Services/Interfaces/IUpdateService.cs` — 接口不变，只改内部实现
- `appsettings.json` — 现有 Update:UpdateUrl，增加 Update:Channel
- `scripts/build-internal.bat` — 现有发布脚本，增加 channel 参数和上传逻辑
- `scripts/e2e-serve.py` — 可参考其 HTTP 服务逻辑，但 Go 服务器独立实现
- `ViewModels/MainWindowViewModel.cs` — CheckUpdateAsync 调用 UpdateService，无需修改
- `App.xaml.cs` — DI 注册 UpdateService 为 Singleton，无需修改

## Acceptance Criteria

### S01: Go 更新服务器
- `go build` 编译通过，产出单二进制
- curl 上传 .nupkg 到 beta 通道成功
- curl 从 /beta/releases.win.json 下载到正确的文件
- curl promote beta 版本到 stable 成功
- curl 查询版本列表返回正确 JSON
- 上传第 11 个版本时，最老版本被自动清理

### S02: 客户端通道支持
- appsettings.json Channel=beta 时，UpdateService 请求 {UpdateUrl}/beta/releases.win.json
- Channel 为空或缺失时，默认使用 stable
- 修改 Channel 后无需重启，下次检查更新即时生效
- 现有 162 个测试全部通过

### S03: 发布脚本改造
- `build-internal.bat standalone beta` 完成编译+打包+上传到 beta 通道
- `build-internal.bat standalone stable` 完成编译+打包+上传到 stable 通道
- 缺少 UPDATE_SERVER_URL 或 UPDATE_SERVER_TOKEN 时给出清晰提示

### S04: 端到端验证
- 启动 Go 服务器 → 上传到 beta → beta 客户端检测到更新
- promote 到 stable → stable 客户端检测到更新
- build-internal.bat 端到端流程验证
- 现有测试全部通过

## Testing Requirements

- Go 服务器：go test 覆盖上传、promote、列表、清理、认证等核心逻辑
- C# 客户端：现有 162 个测试全部通过（不修改接口签名，不改 UI 逻辑）
- 集成：curl 脚本验证 API 交互

## Open Questions

- None — 讨论阶段已解决所有关键决策
