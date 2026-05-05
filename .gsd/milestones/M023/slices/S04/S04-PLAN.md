# S04: 数据迁移 + Windows Server 部署

**Goal:** 自动检测旧 DocuFiller 数据格式（data/stable/, data/beta/）并迁移到新格式（data/docufiller/stable/, data/docufiller/beta/），同步 SQLite 元数据，提供 Windows Server NSSM 部署脚本。
**Demo:** 旧 DocuFiller 数据自动迁移到 data/docufiller/，服务在端口 30001 启动，Web UI 可访问且显示迁移后的数据

## Must-Haves

- migration 包测试证明：旧格式检测 → 文件移动 → SQLite 元数据同步全流程正确
- 迁移幂等：重复运行不报错、不重复数据
- main.go 集成迁移后 go build 成功，全量测试通过
- NSSM 部署脚本存在且语法正确
- README 包含完整的部署步骤说明

## Proof Level

- This slice proves: integration — migration 逻辑通过 Go httptest 和文件系统测试证明，SQLite 元数据同步通过真实数据库测试证明。NSSM 部署脚本为文档交付物，需人工验证。

## Integration Closure

- Upstream surfaces consumed: storage/store.go Store (文件系统操作), model/release.go (feed 数据模型), database/db.go DB (SQLite CRUD)
- New wiring introduced in this slice: main.go 启动时调用 migration.Migrate + migration.SyncMetadata
- What remains before the milestone is truly usable end-to-end: 人工在 Windows Server 上执行 NSSM 部署脚本、配置密码、验证 Web UI

## Verification

- Runtime signals: migration_start, migration_skip, migration_move, migration_complete 结构化 JSON 日志；sync_metadata_start, sync_metadata_app, sync_metadata_complete 元数据同步日志
- Inspection surfaces: data/ 目录结构对比迁移前后；SQLite update-hub.db 可用 sqlite3 查询 apps/versions 表
- Failure visibility: migration_move_error, sync_metadata_error 包含具体文件路径和错误信息

## Tasks

- [ ] **T01: Implement migration package with old-format detection, file move, and metadata sync** `est:1h`
  Create the migration package that handles the core data migration from old single-app format to new multi-app format.
  - Files: `update-hub/migration/migrate.go`, `update-hub/migration/migrate_test.go`
  - Verify: GOCACHE=/tmp/go-cache go test ./migration/... -v -count=1

- [ ] **T02: Wire migration into main.go startup and verify full build** `est:30m`
  Integrate the migration package into the server startup sequence in main.go. Add a `-migrate-app-id` CLI flag (default: "docufiller", empty string = skip migration). The startup order becomes:
  1. Parse flags
  2. Create Store
  3. Init SQLite database
  4. Run migration.Migrate(dataDir, migrateAppId) if flag is non-empty
  5. Run migration.SyncMetadata(dataDir, db) to ensure SQLite is consistent with file system
  6. Create handlers and start server
  - Files: `update-hub/main.go`
  - Verify: GOCACHE=/tmp/go-cache go test ./... -count=1 && GOCACHE=/tmp/go-cache go build -o update-hub.exe .

- [ ] **T03: Create NSSM deployment scripts and deployment README** `est:45m`
  Create Windows batch scripts for NSSM (Non-Sucking Service Manager) service management on Windows Server 2019. Also create a deployment README with step-by-step instructions.
  - Files: `update-hub/deploy/install-service.bat`, `update-hub/deploy/uninstall-service.bat`, `update-hub/deploy/start-service.bat`, `update-hub/deploy/stop-service.bat`, `update-hub/deploy/README.md`
  - Verify: powershell -Command "Get-ChildItem deploy/*.bat | ForEach-Object { Write-Host $_.Name }" && test -f deploy/README.md

## Files Likely Touched

- update-hub/migration/migrate.go
- update-hub/migration/migrate_test.go
- update-hub/main.go
- update-hub/deploy/install-service.bat
- update-hub/deploy/uninstall-service.bat
- update-hub/deploy/start-service.bat
- update-hub/deploy/stop-service.bat
- update-hub/deploy/README.md
