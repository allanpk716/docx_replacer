# M023: update-hub 独立项目

**Vision:** 将 DocuFiller 嵌入的 Go 更新服务器抽取为独立的通用内网自更新平台（update-hub），支持多应用、多平台、多语言客户端，带 Vue 3 Web 管理界面，部署到 Windows Server 2019 替换现有服务。

## Success Criteria

- 新服务器在 Windows Server 2019 上通过 NSSM 运行在端口 30001
- 旧 DocuFiller stable/beta 数据自动迁移到 data/docufiller/stable/ 和 data/docufiller/beta/
- Web UI 可登录、查看应用列表、上传新版本（带备注）、promote、删除
- 现有 build-internal.bat 改 URL 路径后能成功上传
- Velopack 客户端能从新 URL 拉取更新

## Slices

- [x] **S01: S01** `risk:high` `depends:[]`
  > After this: curl 上传 .nupkg 到新服务器 /api/apps/docufiller/channels/stable/releases，Velopack SimpleWebSource 客户端从 /docufiller/stable/releases.win.json 成功拉取更新

- [x] **S02: S02** `risk:medium` `depends:[]`
  > After this: 上传时附带备注文本，通过 GET /api/apps 查询应用列表，通过 GET /api/apps/{appId}/channels/{channel}/versions 查询版本列表含备注

- [x] **S03: S03** `risk:medium` `depends:[]`
  > After this: 浏览器打开 http://server:30001/ 看到 Web UI，登录后查看应用列表、上传新版本（带备注）、promote、删除

- [x] **S04: S04** `risk:high` `depends:[]`
  > After this: 旧 DocuFiller 数据自动迁移到 data/docufiller/，服务在端口 30001 启动，Web UI 可访问且显示迁移后的数据

## Boundary Map

## Boundary Map

### S01 → S02

Produces:
- handler/upload.go → UploadHandler (multipart 上传，feed 合并，artifact 存储)
- handler/list.go → ListHandler (版本列表 API)
- handler/promote.go → PromoteHandler (beta → stable 提升)
- handler/static.go → StaticHandler (feed 和 artifact 文件分发)
- storage/store.go → Store (文件系统 CRUD，原子写入)
- model/release.go → ReleaseFeed, ReleaseAsset (Velopack feed 数据模型)

Consumes: nothing (leaf node)

### S01 → S03

Produces:
- 所有 REST API 端点 (upload/list/promote/delete)
- Bearer token 认证中间件
- Velopack feed + artifact 静态文件服务

Consumes: nothing (leaf node, same as S02)

### S02 → S03

Produces:
- SQLite 元数据查询 API (应用列表、版本列表含备注)
- Release notes 备注存储和查询接口
- database/db.go → DB (SQLite 初始化、迁移、CRUD)

Consumes from S01:
- storage/store.go → Store (元数据写入与文件存储同步)
- model/release.go → ReleaseFeed (版本信息提取)

### S01+S02+S03 → S04

Produces (from all):
- 完整的 Go 服务器二进制（含 embed 前端）
- 数据迁移逻辑 (migration/)
- NSSM 部署脚本

Consumes:
- 旧服务器数据目录结构 (data/{channel}/)
- 旧服务器 NSSM 服务配置 (DocuFillerUpdateServer)
