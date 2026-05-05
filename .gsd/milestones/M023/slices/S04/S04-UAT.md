# S04: 数据迁移 + Windows Server 部署 — UAT

**Milestone:** M023
**Written:** 2026-05-05T07:44:42.389Z

# S04: 数据迁移 + Windows Server 部署 — UAT

**Milestone:** M023
**Written:** 2026-05-05

## UAT Type

- UAT mode: artifact-driven
- Why this mode is sufficient: Migration and deployment are infrastructure concerns — correctness proven through unit/integration tests and build verification. Live deployment requires Windows Server 2019 with NSSM, which is not available in the CI environment.

## Preconditions

- Go 1.22+ build environment
- Existing old-format data directory with `data/stable/releases.*.json` and/or `data/beta/releases.*.json`

## Smoke Test

Run `go build -o update-hub.exe .` — binary compiles successfully with migration wired into startup.

## Test Cases

### 1. Old-format data migration

1. Create `data/stable/releases.win.json` with a valid Velopack feed containing at least one version
2. Create `data/beta/releases.win.json` with a valid Velopack feed
3. Run update-hub with `-migrate-app-id docufiller`
4. **Expected:** `data/stable/` moved to `data/docufiller/stable/`, `data/beta/` moved to `data/docufiller/beta/`
5. **Expected:** Logs contain `migration_move` events for each channel, then `migration_complete`

### 2. Migration idempotency

1. After test case 1, run update-hub again with same flags
2. **Expected:** Logs contain `migration_skip` events (target directories already exist)
3. **Expected:** No errors, service starts normally

### 3. SQLite metadata sync

1. After migration, query `update-hub.db`: `SELECT * FROM versions WHERE app_id = 'docufiller'`
2. **Expected:** All versions from the migrated feed files appear in the database with correct version numbers, release notes, and asset entries

### 4. Migration skip when flag is empty

1. Start update-hub with `-migrate-app-id ""`
2. **Expected:** Log contains `migration_skip` event
3. **Expected:** SyncMetadata still runs (unconditional), ensuring DB consistency with existing filesystem layout

### 5. NSSM service installation

1. Copy `deploy/install-service.bat` to the server alongside `update-hub.exe`
2. Run `install-service.bat` from an elevated CMD prompt
3. **Expected:** Service "UpdateHub" registered, auto-start configured, log directory created
4. Verify: `nssm status UpdateHub` returns "SERVICE_RUNNING" after `start-service.bat`

### 6. Deploy file completeness

1. List files in `deploy/` directory
2. **Expected:** install-service.bat, uninstall-service.bat, start-service.bat, stop-service.bat, README.md all present

## Edge Cases

### Empty data directory

1. Run update-hub with empty `data/` directory
2. **Expected:** No migration events logged, SyncMetadata completes with no-op

### Directory with only .db or .json special files (no feed files)

1. Create `data/metadata/` containing only `cache.db` and `info.json`
2. Run migration
3. **Expected:** `data/metadata/` is NOT detected as a channel directory, NOT moved

### Data directory does not exist

1. Remove `data/` directory entirely
2. Run migration
3. **Expected:** No error — Migrate creates nothing, SyncMetadata handles missing dir gracefully

## Failure Signals

- `migration_move_error` log with file path — rename failed (permissions, cross-device)
- `sync_metadata_error` log — SQLite upsert failed
- Service fails to start — check NSSM logs in `logs/` directory
- Port 30001 not listening — check if another process occupies the port

## Not Proven By This UAT

- Live NSSM service registration on actual Windows Server 2019 (requires real server)
- Real data migration with ~1.1GB stable + ~222MB beta DocuFiller data (CI uses synthetic test data)
- Web UI displaying migrated data after migration (covered by S03, but not tested end-to-end with migration in CI)
- Service auto-start after server reboot
- Log rotation after 10MB threshold
