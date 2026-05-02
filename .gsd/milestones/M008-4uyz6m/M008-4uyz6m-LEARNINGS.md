---
phase: execute
phase_name: M008-4uyz6m Execution
project: DocuFiller
generated: "2026-04-25T00:58:03Z"
counts:
  decisions: 6
  lessons: 4
  patterns: 4
  surprises: 2
missing_artifacts: []
---

### Decisions

- **Go 单二进制更新服务器** — 选择 Go 语言开发更新服务器，放在项目子目录 update-server/，零外部依赖，单二进制部署。文件系统即存储，不需要数据库。Source: M008-4uyz6m-CONTEXT.md/Architectural Decisions
- **APIHandler multiplexer 路由模式** — 在 handler/api.go 中使用单一处理器路由 /api/channels/ 前缀下的所有子路径（upload/list/promote），基于路径后缀 + HTTP 方法分发，避免同前缀路由冲突。Source: S01-SUMMARY.md/Key decisions
- **Bearer Token 认证仅限 POST 端点** — 静态文件服务（GET）和版本列表（GET）无需认证，仅上传和 promote 等变更操作需要 Bearer Token。内网环境简单认证够用。Source: S01-SUMMARY.md/Key decisions
- **客户端通道切换方式：appsettings.json Channel 字段** — 在 Update 节点下增加 Channel 字段（stable/beta），兼顾 GUI 和 CLI 模式，即时生效。Channel 为空默认 stable，向后兼容。Source: M008-4uyz6m-CONTEXT.md/Architectural Decisions
- **发布脚本环境变量门控上传** — build-internal.bat 通过 `if defined CHANNEL` 判断是否执行上传，需要 UPDATE_SERVER_URL 和 UPDATE_SERVER_TOKEN 环境变量。省略 channel 参数时完全跳过上传，向后兼容。Source: S03-SUMMARY.md/Key decisions
- **每通道保留 10 版本自动清理** — 上传和 promote 时自动触发清理，超过 10 个版本删除最老的。无需人工干预。Source: M008-4uyz6m-CONTEXT.md/Architectural Decisions

### Lessons

- **Go test 必须在 go.mod 所在目录执行** — 从仓库根目录运行 `go test ./update-server/...` 会报错 "directory prefix does not contain main module"。正确做法：`cd update-server && go test ./...`。Source: S04-SUMMARY.md/Verification
- **BAT 脚本中 curl 捕获 HTTP 状态码** — 使用 `curl -s -o nul -w %%{http_code}` 模式，Windows BAT 中必须双百分号转义且使用 nul（非 /dev/null）。Source: S03-SUMMARY.md/Key decisions
- **E2E 脚本既是验证工具也是运维文档** — e2e-dual-channel-test.sh 证明了 C# UpdateService 将使用的实际 HTTP 请求模式，可随时重新运行验证系统完整性。Source: S04-SUMMARY.md/What Happened
- **Go test 数量可能因 test 发现机制略有浮动** — 计划 50 个测试，实际执行时为 52 个（test discovery 包含了额外子测试），不影响质量判定。Source: S04-SUMMARY.md/Verification

### Patterns

- **原子文件写入（temp + rename）** — release feed 写入使用临时文件 + 重命名策略，防止并发访问时读到不完整的 JSON。Source: S01-SUMMARY.md/Key decisions
- **结构化 JSON 日志** — Go 服务器为每个 HTTP 请求记录 method/path/status/duration_ms，业务事件（upload、cleanup、promote）记录带上下文的独立结构化日志。Source: S01-SUMMARY.md/observability_surfaces
- **build-internal.bat 上传子程序模式** — :UPLOAD 子程序封装完整的上传流程（环境变量检查 → curl multipart POST → 状态验证），可被主流程在 VPK_PACK 后条件调用。Source: S03-SUMMARY.md/Patterns
- **通道隔离 URL 构造** — UpdateService 使用 `TrimEnd('/') + '/' + channel + '/'` 简单拼接，不使用 Uri 类，确保 URL 格式一致性。Source: S02-SUMMARY.md/Key decisions

### Surprises

- **Go test 数量浮动** — 同一测试文件在不同执行中可能报告略有不同的测试数（t.Helper 或子测试发现差异），不影响实际覆盖。Source: S04-SUMMARY.md/Verification
- **.NET 测试总数增加** — 原计划 162 个测试，S02 新增 6 个 UpdateServiceTests 后变为 168 个，是正向偏差但需要更新文档中的基准数字。Source: S02-SUMMARY.md/Verification
