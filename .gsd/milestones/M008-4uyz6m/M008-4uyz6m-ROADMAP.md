# M008-4uyz6m: 双通道更新系统 — Go 更新服务器 + stable/beta 通道 + 自动化发布

**Vision:** 为 DocuFiller 的 Velopack 自动更新引入 stable/beta 双通道机制：开发 Go 轻量更新服务器管理多通道版本发布（上传、promote、自动清理），客户端通过 appsettings.json 选择通道即时切换，发布脚本一条命令完成构建+推送。

## Success Criteria

- Go 更新服务器：上传、promote、列表、清理 API 全部可用，curl 可验证
- 客户端 Channel=beta 时从 beta 通道检查更新，Channel 为空默认 stable
- build-internal.bat beta 一条命令完成编译+打包+上传
- promote API 能将 beta 版本推到 stable，stable 客户端收到更新
- 每通道超过 10 个版本自动清理
- dotnet build 0 errors，dotnet test 162 pass

## Slices

- [x] **S01: S01** `risk:high` `depends:[]`
  > After this: 启动服务器，curl 上传 .nupkg 到 beta 通道，从 /beta/releases.win.json 下载，promote 到 stable，查询版本列表，上传第 11 个版本触发自动清理

- [x] **S02: S02** `risk:medium` `depends:[]`
  > After this: 修改 appsettings.json Channel=beta，启动 DocuFiller，检查更新时请求 {UpdateUrl}/beta/releases.win.json

- [x] **S03: S03** `risk:low` `depends:[]`
  > After this: build-internal.bat standalone beta 完成编译+打包+自动上传到 Go 服务器的 beta 通道

- [x] **S04: S04** `risk:low` `depends:[]`
  > After this: 完整流程：Go 服务器运行 → 上传 beta → 客户端检测到更新 → promote stable → stable 客户端检测到更新

## Boundary Map

### S01 → S02
Produces:
- HTTP 静态文件服务：/{channel}/releases.win.json 和 /{channel}/*.nupkg
- REST API：POST /api/channels/{channel}/releases、POST /api/channels/stable/promote、GET /api/channels/{channel}/releases
- API 认证：Authorization: Bearer {token}

Consumes: nothing (leaf node)

### S01 → S03
Produces:
- 上传 API 端点：POST /api/channels/{channel}/releases
- API 认证要求：Bearer Token

Consumes: nothing (leaf node)

### S02 → S04
Produces:
- UpdateService URL 拼接：{UpdateUrl}/{Channel}/
- appsettings.json Channel 字段
- 向后兼容逻辑（Channel 为空默认 stable）

Consumes from S01:
- HTTP 静态文件服务：/{channel}/releases.win.json

### S03 → S04
Produces:
- build-internal.bat channel 参数支持
- 自动上传逻辑（curl POST）

Consumes from S01:
- 上传 API：POST /api/channels/{channel}/releases
- Token 认证
