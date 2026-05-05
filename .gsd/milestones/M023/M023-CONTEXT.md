# M023: update-hub 独立项目

**Gathered:** 2026-05-05
**Status:** Ready for planning

## Project Description

将 DocuFiller 项目中嵌入的 Go 更新服务器（`update-server/`，~2400 行代码）抽取出来，在 `C:\WorkSpace\agent\update-hub\` 创建独立项目。演进为通用内网自更新平台，支持多应用、多平台、多语言客户端。

## Why This Milestone

当前更新服务器硬编码只服务 DocuFiller 一个应用，只支持 Windows（`releases.win.json`），只有 `stable/beta` 两个通道。用户有多个内网发布的程序（Go、Python、跨平台），需要一个统一的更新分发平台。

## User-Visible Outcome

### When this milestone is complete, the user can:

- 在浏览器打开 `http://172.18.200.47:30001/` 看到 Web 管理界面
- 登录后看到 DocuFiller 应用的 stable/beta 通道和所有版本
- 通过 Web UI 上传新版本（支持 release notes 备注）
- 通过 Web UI 执行 promote（beta → stable）
- 用 `build-internal.bat` 改 URL 路径后成功上传到新服务器
- DocuFiller 客户端从新 URL 成功拉取更新

### Entry point / environment

- Entry point: `http://172.18.200.47:30001/` (Web UI) + Velopack SimpleWebSource API
- Environment: Windows Server 2019, NSSM Windows Service, port 30001
- Live dependencies: 现有 DocuFiller 客户端（正在使用旧服务器自动更新）

## Completion Class

- Contract complete means: 所有 API 端点可被 curl/Velopack 客户端正确调用，返回正确的 Velopack feed 格式
- Integration complete means: Web UI 可完整操作（登录→查看→上传→promote→删除），DocuFiller 客户端成功从新服务器拉取更新
- Operational complete means: 新服务器在 Windows Server 2019 上通过 NSSM 运行，旧数据已迁移，服务稳定运行

## Final Integrated Acceptance

To call this milestone complete, we must prove:

- 旧 DocuFiller stable/beta 数据自动迁移到 `data/docufiller/stable/` 和 `data/docufiller/beta/`，Velopack 客户端能拉取到正确版本
- Web UI 完整操作流：登录 → 查看应用列表 → 上传新版本（带备注） → promote → 删除旧版本
- 服务器在端口 30001 稳定运行，不影响现有 DocuFiller 客户端的更新流程

## Architectural Decisions

### 更新协议：统一 Velopack

**Decision:** 所有接入的应用统一使用 Velopack 打包和更新协议，不做自定义协议。

**Rationale:** Velopack 已经解决了最难的问题（delta 更新、签名验证、安装/便携模式自更新、权限处理、多平台支持）。DocuFiller 已深度集成 Velopack，迁移成本最小。Go/Python 项目用 `vpk pack` 打包也完全可行。

**Alternatives Considered:**
- 双协议（Velopack + 自定义 JSON REST）— 增加服务器复杂度，客户端也需要两套逻辑
- 完全自定义协议 — 需要重新发明 delta 更新、签名验证等，工作量巨大

### 数据存储：SQLite 元数据 + 文件系统 Artifacts

**Decision:** 用 SQLite 存储应用元数据、release notes 备注、上传历史。Artifact 文件（.nupkg、feed JSON）仍然走文件系统。

**Rationale:** Web UI 需要结构化查询（应用列表、版本排序、备注搜索），纯文件系统做不好。SQLite 单文件、零运维、Go 标准库自带驱动。

**Alternatives Considered:**
- 纯文件系统 + JSON 元数据文件 — 并发写入和查询效率差
- PostgreSQL/MySQL — 内网单服务器杀鸡用牛刀，增加运维负担

### Web UI 技术栈：Vue 3 + Vite + Go embed

**Decision:** Vue 3 + Vite 构建 SPA，编译产物通过 Go `embed` 包内嵌到服务器二进制中。

**Rationale:** 用户选择 Vue。Go embed 实现单文件部署，符合 Go 单二进制的部署哲学。SPA 的路由 fallback（所有未匹配路径返回 index.html）是成熟模式。

**Alternatives Considered:**
- Go 模板渲染 — 用户体验差，不适合现代管理界面
- 独立前端项目 — 部署时需要分别管理两个服务

### 认证方案：管理密码 + JWT session + Bearer token 兼容

**Decision:** 启动参数配置管理密码。Web UI 登录后发 JWT session cookie。API 同时支持 Bearer token（兼容现有 curl 上传）和 JWT session（Web UI 使用）。

**Rationale:** 内网单用户场景，不需要多用户/角色系统。兼容 Bearer token 确保现有 `build-internal.bat` 不需要改动认证方式。

**Alternatives Considered:**
- 只用 Bearer token — Web UI 体验差（每次刷新都要重新输入）
- OAuth2 — 内网环境杀鸡用牛刀

## Error Handling Strategy

- 上传失败 → 事务性（新文件写入 temp 再 rename，feed 合并失败不影响已有文件，沿用现有的原子写入模式）
- 并发安全 → SQLite WAL 模式 + 文件写入用 temp+rename 原子模式
- Web UI 认证失败 → 401 + 前端重定向到登录页
- 服务器启动失败 → 清晰日志输出（沿用现有的 structured JSON logging）
- 数据迁移 → 幂等设计，可重复运行；迁移前备份旧数据目录

## Risks and Unknowns

- Vue SPA embed 进 Go 二进制的路由 fallback — 成熟模式但需要在 S03 第一个 slice 验证
- 旧数据迁移的原子性 — ~1.3GB 数据需要移动/重命名，需要确保中途失败可恢复
- NSSM 服务替换的停机时间 — 停旧服务 → 替换二进制 → 迁移数据 → 启新服务，需要最小化停机

## Existing Codebase / Prior Art

- `update-server/` — 当前 Go 更新服务器，~2400 行，handler/storage/middleware/model 四层结构
- `update-server/handler/upload.go` — multipart 上传处理，releases.win.json 特殊合并逻辑
- `update-server/handler/promote.go` — beta → stable 提升逻辑，文件复制 + feed 合并
- `update-server/storage/store.go` — 文件系统存储，原子写入（temp + rename）
- `update-server/middleware/auth.go` — Bearer token 认证中间件
- `scripts/build-internal.bat` — 构建脚本，UPLOAD 子程序用 curl multipart 上传
- `Services/UpdateService.cs` — DocuFiller 客户端更新服务，用 Velopack SimpleWebSource

## Relevant Requirements

- R066 — 多应用 Velopack feed 分发
- R067 — 多 OS feed 支持
- R068 — 应用自动注册
- R069 — 动态通道
- R070 — SQLite 元数据
- R071 — Release notes 备注
- R072 — Web 管理界面
- R073 — 认证方案
- R074 — 旧数据迁移
- R075 — NSSM 服务部署

## Scope

### In Scope

- Go 服务器核心 API（多应用、多 OS、动态通道、应用自动注册）
- SQLite 元数据层（应用信息、release notes 备注、上传历史）
- Vue 3 Web 管理界面（登录、查看、上传、promote、删除）
- Go embed SPA 前端
- JWT session + Bearer token 双认证
- 旧数据自动迁移
- Windows Server 2019 NSSM 部署
- Go 和 Python 客户端示例代码
- 使用文档

### Out of Scope / Non-Goals

- CLI 管理工具（deferred）
- 多服务器/多实例同步（deferred）
- 多用户/角色系统
- Docker 容器化（Windows Server 2019 直接部署 NSSM 服务）
- 版本回滚功能
- 自定义协议（统一 Velopack）

## Technical Constraints

- Windows Server 2019 目标环境
- 端口 30001 不变（已有端口映射）
- NSSM Windows 服务管理
- Go 1.22+ 编译
- Vue 3 + Vite 前端
- Go embed 前端资源（单二进制部署）
- 现有 DocuFiller 客户端 Velopack SDK 不需要改代码

## Integration Points

- Velopack SimpleWebSource — 客户端通过 HTTP GET 拉取 releases feed 和 .nupkg 文件
- curl multipart POST — 现有 build-internal.bat 的上传方式
- Windows Server SSH — 部署通道（paramiko/Python 脚本）
- NSSM — Windows 服务管理

## Testing Requirements

- Go 单元测试：handler、storage、migration 各层
- API 集成测试：上传 → 查询 → promote → 删除完整流程
- 前端手动验证：Web UI 各功能页面
- 部署验证：SSH 到服务器确认服务启动、数据迁移成功、Web UI 可访问

## Acceptance Criteria

### S01 — Go 服务器核心 API
- `POST /api/apps/{appId}/channels/{channel}/releases` 上传成功
- `GET /{appId}/{channel}/releases.win.json` 返回正确的 Velopack feed
- `GET /{appId}/{channel}/releases.linux.json` 返回 404 或对应 feed
- `GET /{appId}/{channel}/*.nupkg` 返回 artifact 文件
- 应用自动注册：第一次上传时自动创建应用记录
- Bearer token 认证保护上传/promote/delete

### S02 — SQLite 元数据
- 上传时可选附带备注，备注持久化到 SQLite
- `GET /api/apps` 返回应用列表
- `GET /api/apps/{appId}/channels/{channel}/versions` 返回版本列表含备注

### S03 — Web 管理界面
- 登录页：输入密码，获取 JWT session
- 应用列表页：显示所有已注册应用
- 通道/版本页：显示选定应用的通道和版本
- 上传页：选择文件 + 填写备注，上传到指定应用/通道
- Promote：从 beta → stable 一键提升
- 删除：删除指定版本

### S04 — 数据迁移 + 部署
- 旧 `data/stable/` 迁移到 `data/docufiller/stable/`
- 旧 `data/beta/` 迁移到 `data/docufiller/beta/`
- 迁移幂等：重复运行不破坏已有数据
- NSSM 服务停止旧服务、替换二进制、启动新服务
- 端口 30001 保持不变

## Open Questions

- 无——所有关键决策已在讨论中确认
