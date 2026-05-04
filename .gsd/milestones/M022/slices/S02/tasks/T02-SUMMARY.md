---
id: T02
parent: S02
milestone: M022
key_files:
  - poc/tauri-docufiller/src-tauri/src/lib.rs
  - poc/tauri-docufiller/src/index.html
  - poc/tauri-docufiller/src/app.js
  - poc/tauri-docufiller/src/styles.css
  - poc/tauri-docufiller/sidecar-dotnet/Program.cs
key_decisions:
  - Used std::process::Command for sidecar launching instead of tauri-plugin-shell ShellExt API — simpler, avoids additional shell permissions, and the PoC sidecar can also be started manually with dotnet run
  - Used ReadableStream reader for SSE parsing in frontend instead of EventSource API — better error handling and works with fetch() which respects CSP connect-src
  - Added Access-Control-Allow-Origin: * header to sidecar SSE response for cross-origin compatibility in dev mode
duration: 
verification_result: passed
completed_at: 2026-05-04T16:12:33.127Z
blocker_discovered: false
---

# T02: Implement mini DocuFiller PoC with native file dialog, .NET sidecar SSE progress API, and frontend progress bar — both toolchains compile clean

**Implement mini DocuFiller PoC with native file dialog, .NET sidecar SSE progress API, and frontend progress bar — both toolchains compile clean**

## What Happened

Implemented the complete DocuFiller PoC interaction flow across three layers:

**Rust backend (lib.rs):** Replaced T01's placeholder `greet` command with two production commands:
- `open_file_dialog` — Uses `tauri_plugin_dialog::DialogExt` to show a native OS file picker filtered for .docx/.xlsx files. Returns `Option<String>` (file path or None on cancel).
- `start_sidecar` — Launches `dotnet run --project ../sidecar-dotnet` via `std::process::Command` as a detached child process. Returns PID string.

**.NET sidecar (Program.cs):** Replaced T01's minimal Kestrel server with a full processing API:
- `GET /api/health` — Health check endpoint (retained from T01).
- `GET /api/process/stream?filePath=...` — SSE endpoint that simulates a 5-step document processing pipeline (Validating→Parsing→Filling→Generating→Finalizing), streaming progress events at 20% increments with 600ms delay per step. Each SSE event is JSON: `{step, progress, fileName, filePath}`. Includes a "Starting" (0%) and "Complete" (100%) event with output filename.
- CORS header `Access-Control-Allow-Origin: *` added for cross-origin SSE consumption.

**Frontend (index.html + app.js + styles.css):** Built a complete mini DocuFiller UI:
- Sidecar connection status indicator (green dot / red dot / orange pulse) with automatic health check on load.
- Native file selection button invoking `tauri.invoke('open_file_dialog')`.
- Start processing button triggering SSE fetch to sidecar `/api/process/stream`.
- HTML5 progress bar with gradient fill (blue→green) updating in real-time from SSE events.
- Dark-themed event log area showing timestamped processing steps.
- Sidecar control section with start/refresh buttons as fallback.
- JavaScript uses ReadableStream reader for SSE parsing (not EventSource, for better error handling).

Both toolchains compile without errors or warnings.

## Verification

Both toolchain builds verified:

1. **dotnet build** (sidecar-dotnet/): Run via PowerShell with explicit env vars for worktree compatibility. Exit code 0, no warnings or errors.

2. **cargo build** (src-tauri/): Compiled tauri-docufiller crate plus tauri-plugin-dialog v2.7.1, tauri-plugin-shell v2.3.5, tauri-plugin-fs v2.5.1. Finished dev profile in 25.06s, exit code 0, no warnings.

All 5 expected output files verified present: lib.rs, index.html, app.js, styles.css, Program.cs.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `powershell.exe -NoProfile -Command '...; dotnet build' (sidecar-dotnet/)` | 0 | ✅ pass | 1723ms |
| 2 | `cd poc/tauri-docufiller/src-tauri && cargo build` | 0 | ✅ pass | 25060ms |

## Deviations

None.

## Known Issues

None.

## Files Created/Modified

- `poc/tauri-docufiller/src-tauri/src/lib.rs`
- `poc/tauri-docufiller/src/index.html`
- `poc/tauri-docufiller/src/app.js`
- `poc/tauri-docufiller/src/styles.css`
- `poc/tauri-docufiller/sidecar-dotnet/Program.cs`
