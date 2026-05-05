---
estimated_steps: 8
estimated_files: 1
skills_used: []
---

# T02: Wire migration into main.go startup and verify full build

Integrate the migration package into the server startup sequence in main.go. Add a `-migrate-app-id` CLI flag (default: "docufiller", empty string = skip migration). The startup order becomes:
1. Parse flags
2. Create Store
3. Init SQLite database
4. Run migration.Migrate(dataDir, migrateAppId) if flag is non-empty
5. Run migration.SyncMetadata(dataDir, db) to ensure SQLite is consistent with file system
6. Create handlers and start server

Log migration events as structured JSON. Verify the full project builds and all existing tests still pass (no regressions).

## Inputs

- `update-hub/main.go`
- `update-hub/migration/migrate.go`

## Expected Output

- `update-hub/main.go`

## Verification

GOCACHE=/tmp/go-cache go test ./... -count=1 && GOCACHE=/tmp/go-cache go build -o update-hub.exe .
