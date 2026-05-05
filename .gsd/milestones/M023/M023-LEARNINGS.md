---
phase: complete-milestone
phase_name: M023 Completion
project: docx_replacer
generated: "2026-05-05T08:00:00Z"
counts:
  decisions: 8
  lessons: 8
  patterns: 6
  surprises: 3
missing_artifacts: []
---

# M023 Learnings

### Decisions

- **Go 1.22 ServeMux over manual dispatch** — Chose Go 1.22's built-in method+path routing (`"POST /api/apps/{appId}/channels/{channel}/releases"`) over manual router registration. Eliminates boilerplate, provides PathValue extraction, and pattern specificity (literal `/assets/` beats wildcard `/`). Source: S01-SUMMARY.md/What Happened
- **modernc.org/sqlite over mattn/go-sqlite3** — Chose pure Go SQLite driver (modernc.org/sqlite v1.34.5) over CGO-backed mattn/go-sqlite3. Avoids CGO toolchain issues on Windows, cross-compiles cleanly. Trade-off: ~2x slower than CGO for heavy queries, but negligible for our read-mostly workload. Source: S02-SUMMARY.md/What Happened
- **Best-effort metadata (file authoritative, SQLite additive)** — File storage is the source of truth; SQLite is an additive index. Metadata operations are nil-safe and errors are logged but never block file operations. This simplifies error handling and means metadata can always be rebuilt via SyncMetadata. Source: S02-SUMMARY.md/What Happened
- **Vue 3 + Go embed for single-binary deployment** — Chose Go `//go:embed web/dist` over serving frontend separately. Single binary deployment matches Go philosophy. Custom SPAHandler needed because http.FileServer doesn't handle index.html fallback for client-side routes. Source: S03-SUMMARY.md/What Happened
- **Bearer token precedence over JWT cookie** — Bearer token header checked first, JWT cookie as fallback. Ensures backward compatibility with existing curl/build-internal.bat clients while enabling Web UI session management. Source: S03-SUMMARY.md/What Happened
- **os.Rename for atomic migration (not copy+delete)** — Used os.Rename for per-channel migration because it's atomic on same filesystem. Copy+delete would leave inconsistent state on failure. Trade-off: fails across volumes, needs copy+delete fallback for cross-drive scenarios. Source: S04-SUMMARY.md/What Happened
- **Migration errors are fatal** — Migration failures call log.Fatalf to prevent serving with inconsistent data. Better to fail loudly than silently serve partial state. Source: S04-SUMMARY.md/What Happened
- **All GET /api/* endpoints public** — Read-only query endpoints (app list, version list) don't require auth. Simplifies frontend development and maintains compatibility with Velopack clients that do unauthenticated GET requests. Source: S02-SUMMARY.md/What Happened

### Lessons

- **WAL mode must be set via DSN query params, not PRAGMA** — `file:data.db?_journal_mode=WAL&_busy_timeout=5000` works across database/sql connection pooling. `PRAGMA journal_mode=WAL` only applies to the single connection that executes it. Source: S02-SUMMARY.md/What Happened
- **GOCACHE env var must be explicitly set in some agent/Git Bash environments** — Go doesn't always auto-detect %LocalAppData% in Git Bash shells. `GOCACHE="$TEMP/go-cache"` is the portable workaround. Source: S01-SUMMARY.md/Verification
- **esbuild postinstall may fail in restricted CI/agent environments** — `npm install --ignore-scripts` is a viable workaround when esbuild's native binary download fails. Source: S03-SUMMARY.md/Deviations
- **Go 1.22 doesn't have t.Context()** — Use `context.Background()` in tests instead. `t.Context()` was added in Go 1.24. Source: S04-SUMMARY.md/Deviations
- **os.Rename fails across filesystem volumes** — Atomic rename only works within the same filesystem. Cross-drive migration needs copy+delete with proper cleanup on failure. Source: S04-SUMMARY.md/Known Limitations
- **http.FileServer doesn't handle SPA fallback** — Need a custom handler that serves index.html for non-file paths. http.FileServer returns 404 for client-side routes like /login or /apps/docufiller. Source: S03-SUMMARY.md/What Happened
- **Upload dialog must use raw fetch() not wrapper** — apiFetch wrappers that set Content-Type: application/json will corrupt multipart/form-data boundaries. Use raw fetch() for file uploads. Source: S03-SUMMARY.md/What Happened
- **Empty Bearer token string disables auth entirely** — When `-token ""` is passed, auth middleware skips validation. Useful for dev/local but must be documented to prevent accidental auth bypass. Source: S01-SUMMARY.md/What Happened

### Patterns

- **Go 1.22 ServeMux method+path routing with PathValue extraction** — Register routes like `mux.Handle("POST /api/apps/{appId}/channels/{channel}/releases", ...)` and extract path parameters via `r.PathValue("appId")`. Pattern specificity is automatic. Source: S01-SUMMARY.md/Patterns Established
- **Multi-app URL pattern: /api/apps/{appId}/channels/{channel}/...** — All API endpoints follow this hierarchical pattern. Maps cleanly to filesystem layout `data/{appId}/{channel}/`. Source: S01-SUMMARY.md/Patterns Established
- **Atomic file writes (temp+rename)** — Write to temp file in same directory, then os.Rename to final path. Ensures atomicity on same filesystem and prevents partial writes. Source: S01-SUMMARY.md/Patterns Established
- **Structured JSON logging for all events** — All HTTP requests, metadata operations, and migration steps logged as structured JSON with method, path, status, duration_ms fields. Bearer token never logged. Source: S01-SUMMARY.md/Patterns Established
- **Best-effort metadata with nil-safe DB pattern** — All handlers check `if h.DB != nil` before metadata operations. Errors logged but never block file operations. Source: S02-SUMMARY.md/Patterns Established
- **Idempotent operations with skip-not-error pattern** — Migration skips existing targets instead of erroring. Delete returns success with files_deleted: 0 when nothing to remove. Source: S04-SUMMARY.md/Patterns Established

### Surprises

- **Go 1.22 ServeMux pattern specificity works perfectly for SPA + API + static routing** — Expected routing conflicts between SPA fallback (`/`) and API endpoints (`/api/*`) and Velopack static paths (`/{appId}/`). Go 1.22's most-specific-pattern-first matching resolved all conflicts without custom routing logic. Source: S03-SUMMARY.md/What Happened
- **Integration test discovered the need for auth_check endpoint** — The Vue auth composable needed a way to verify JWT session status on page load. GET /api/auth/check wasn't in the original plan but became essential for the frontend UX. Source: S03-SUMMARY.md/Deviations
- **Pure Go SQLite performed well enough for metadata workload** — Expected noticeable performance difference from CGO-backed SQLite, but modernc.org/sqlite handled our read-mostly metadata queries without measurable latency impact. Source: S02-SUMMARY.md/What Happened
