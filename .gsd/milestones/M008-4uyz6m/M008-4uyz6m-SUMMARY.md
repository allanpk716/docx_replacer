---
id: M008-4uyz6m
title: "双通道更新系统 — Go 更新服务器 + stable/beta 通道 + 自动化发布"
status: complete
completed_at: 2026-04-25T00:59:27.731Z
key_decisions:
  - Go 单二进制更新服务器（零外部依赖，文件系统即存储，原子写入）
  - APIHandler multiplexer 路由模式（共享 /api/channels/ 前缀分发）
  - Bearer Token 认证仅限 POST 端点，静态文件和版本列表公开
  - 客户端 URL 构造使用 TrimEnd('/') + '/' + channel + '/' 简单拼接
  - 发布脚本 upload 环境变量门控（UPDATE_SERVER_URL + UPDATE_SERVER_TOKEN），向后兼容
  - 每通道保留 10 版本自动清理，上传和 promote 时触发
key_files:
  - update-server/main.go
  - update-server/handler/static.go
  - update-server/handler/upload.go
  - update-server/handler/promote.go
  - update-server/handler/list.go
  - update-server/handler/api.go
  - update-server/handler/handler_test.go
  - update-server/handler/upload_test.go
  - update-server/middleware/auth.go
  - update-server/model/release.go
  - update-server/storage/store.go
  - update-server/storage/cleanup.go
  - update-server/storage/store_test.go
  - update-server/storage/cleanup_test.go
  - update-server/go.mod
  - Services/UpdateService.cs
  - Services/Interfaces/IUpdateService.cs
  - appsettings.json
  - Tests/UpdateServiceTests.cs
  - scripts/build-internal.bat
  - scripts/build.bat
  - scripts/e2e-dual-channel-test.sh
  - scripts/test-update-server.sh
lessons_learned:
  - Go test 命令必须在 go.mod 所在目录执行，不能从仓库根目录运行（go test ./update-server/... 会失败）
  - APIHandler multiplexer 模式适合多个路由共享同一前缀的场景，避免重复注册中间件
  - BAT 脚本中使用 curl -s -o nul -w %%{http_code} 模式可在不输出响应体的同时捕获 HTTP 状态码
  - 端到端验证脚本（e2e-dual-channel-test.sh）既是验证工具也是运维文档，证明 C# 客户端将使用的实际 HTTP 请求模式
---

# M008-4uyz6m: 双通道更新系统 — Go 更新服务器 + stable/beta 通道 + 自动化发布

**构建了 Go 轻量更新服务器（上传/promote/列表/清理 API + 静态文件服务），改造客户端 UpdateService 支持通道选择，build-internal.bat 支持一条命令构建+发布，端到端双通道流程验证通过（52 Go + 168 .NET 测试全通过）。**

## What Happened

## 概述

M008-4uyz6m 为 DocuFiller 的 Velopack 自动更新系统引入了 stable/beta 双通道机制。通过 4 个切片（S01-S04），完成了 Go 更新服务器开发、客户端通道支持、发布脚本改造和端到端验证。

## S01: Go 更新服务器（4 任务）

在 `update-server/` 子目录构建了完整的 Go HTTP 更新服务器，单二进制零外部依赖。提供 5 个 API 端点：静态文件服务（/{channel}/releases.win.json + *.nupkg）、上传（POST /api/channels/{channel}/releases）、promote（POST /api/channels/{target}/promote）、版本列表（GET /api/channels/{channel}/releases）。采用 Bearer Token 认证（仅 POST 端点）、原子文件写入、APIHandler multiplexer 路由模式、结构化 JSON 日志、每通道保留 10 个版本的自动清理。52 个 Go 单元测试覆盖 handler（38）和 storage（14）。

## S02: 客户端通道支持（2 任务）

修改 UpdateService 从 appsettings.json 的 Update:Channel 读取通道配置，构造 {UpdateUrl}/{Channel}/ 格式的通道感知 URL。Channel 为空默认 stable，保持向后兼容。新增 6 个 xunit 单元测试覆盖各种配置组合（默认 stable、显式 beta、Channel 键缺失、UpdateUrl 为空、有/无末尾斜杠）。

## S03: 发布脚本改造（1 任务）

build-internal.bat 新增可选的 CHANNEL 参数（stable/beta），验证通过后在 VPK_PACK 完成后自动调用 :UPLOAD 子程序，通过 curl multipart POST 将 releases.win.json 和 .nupkg 文件上传到 Go 更新服务器。需要 UPDATE_SERVER_URL 和 UPDATE_SERVER_TOKEN 环境变量。向后兼容——不带参数时跳过上传步骤。

## S04: 端到端验证（2 任务）

创建了 e2e-dual-channel-test.sh 集成脚本，覆盖完整双通道流程：上传到 beta → 验证 beta feed → 确认通道隔离 → promote beta→stable → 验证 stable feed → 自动清理（11 版本触发移除最老版本）。全部 13 个断言通过。运行完整测试套件确认无回归：52 Go + 168 .NET = 220 总测试通过。

## 最终验证结果

- Go build: exit 0, go vet: clean
- Go tests: 52 PASS, 0 FAIL
- .NET build: 0 errors, 0 warnings
- .NET tests: 168 PASS (141 unit + 27 E2E), 0 failures
- E2E dual-channel: 13 assertions PASS

## Success Criteria Results

| # | 成功标准 | 结果 | 证据 |
|---|---------|------|------|
| 1 | Go 更新服务器：上传、promote、列表、清理 API 全部可用，curl 可验证 | ✅ 通过 | S01 交付 5 个 API 端点，52 Go 测试通过，go build exit 0，test-update-server.sh 集成测试通过 |
| 2 | 客户端 Channel=beta 时从 beta 通道检查更新，Channel 为空默认 stable | ✅ 通过 | S02 改造 UpdateService，6 个单元测试验证所有配置场景 |
| 3 | build-internal.bat beta 一条命令完成编译+打包+上传 | ✅ 通过 | S03 新增 channel 参数，grep 验证 28 UPLOAD 行 + 10 CHANNEL 行 |
| 4 | promote API 能将 beta 版本推到 stable，stable 客户端收到更新 | ✅ 通过 | S01 promote handler + S04 e2e-dual-channel-test.sh 13 断言全通过 |
| 5 | 每通道超过 10 个版本自动清理 | ✅ 通过 | S01 cleanup 模块，11 版本测试（移除 1 个）+ 15 版本测试（移除 5 个）|
| 6 | dotnet build 0 errors，dotnet test 162 pass | ✅ 通过（超额完成） | dotnet build 0 errors 0 warnings，168 tests pass（比目标的 162 多 6 个 S02 新增测试）|

## Definition of Done Results

| 项目 | 状态 | 证据 |
|------|------|------|
| 所有切片完成 | ✅ | S01 (4/4 tasks), S02 (2/2), S03 (1/1), S04 (2/2) — 全部 complete |
| 所有 slice summary 存在 | ✅ | S01-SUMMARY.md, S02-SUMMARY.md, S03-SUMMARY.md, S04-SUMMARY.md |
| 跨切片集成验证 | ✅ | S04 e2e-dual-channel-test.sh 13 断言覆盖完整双通道流程 |
| 代码变更存在 | ✅ | 26 个文件变更（3113 行新增），Go 服务器 16 源文件 + 4 测试文件 + 4 脚本 + 4 C# 文件 |
| Go build + vet 通过 | ✅ | go build exit 0, go vet no issues |
| .NET build 通过 | ✅ | 0 errors, 0 warnings |
| 测试全通过 | ✅ | 52 Go + 168 .NET = 220 total, 0 failures |

## Requirement Outcomes

| 需求 ID | 变更 | 状态 | 证据 |
|----------|------|------|------|
| R030 | Active → validated | ✅ validated | 静态文件服务 /{channel}/releases.win.json + *.nupkg，50 Go 测试通过 |
| R031 | Active → validated | ✅ validated | POST upload API + Bearer auth + feed 合并，httptest + curl 验证 |
| R032 | Active → validated | ✅ validated | POST promote API + 404 处理，handler_test.go 5 测试用例 |
| R033 | Active → validated | ✅ validated | GET version list API，无认证，3 测试用例通过 |
| R034 | Active → validated | ✅ validated | CleanupOldVersions 保留 10 版本，11/15 版本测试场景通过 |
| R035 | Active → validated | ✅ validated | build-internal.bat channel 参数 + curl 上传，grep 验证 |
| R036 | Active → validated | ✅ validated | e2e-dual-channel-test.sh 13 断言 + 52 Go + 168 .NET 全通过 |

## Deviations

原始计划 test 目标为 162 个，实际 168 个（S02 新增 6 个 UpdateServiceTests，dotnet test 总数从 162 增加到 168）。Go 测试也从计划的 50 增加到 52（S04 执行时测试数量略有变化）。均为正向偏差。

## Follow-ups

- 将 update-server/ 考虑迁移为独立仓库（当前作为子目录开发）
- 考虑为 Go 服务器添加 HTTPS 支持（当前仅 HTTP）
- 考虑添加版本回滚 API（当前只有自动清理，没有手动回滚能力）
- 考虑添加上传文件校验（文件大小限制、文件类型白名单）
