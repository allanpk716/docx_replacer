# S02: Tauri + .NET sidecar PoC + 调研文档 — UAT

**Milestone:** M022
**Written:** 2026-05-04T16:19:36.297Z

# S02: Tauri + .NET sidecar PoC + 调研文档 — UAT

**Milestone:** M022
**Written:** 2026-05-05

## UAT Type

- UAT mode: artifact-driven
- Why this mode is sufficient: The slice goal is a PoC that compiles and a research document. Runtime testing (tauri dev) requires WebView2 and interactive GUI, which is beyond automated verification scope. The proof level is "contract" — compilation success and code structure verification.

## Preconditions

- Windows machine with rustc 1.88+ and .NET 8 SDK installed
- Working directory at poc/tauri-docufiller/

## Smoke Test

Verify both toolchains compile:
1. `cd poc/tauri-docufiller/src-tauri && cargo build` → exit 0
2. `cd poc/tauri-docufiller/sidecar-dotnet && dotnet build` → exit 0

## Test Cases

### 1. Research Document Completeness

1. Open `docs/cross-platform-research/tauri-dotnet-research.md`
2. Verify table of contents lists 14+ numbered chapters
3. Verify section 13 "PoC 发现总结" references actual T01/T02 development experience
4. Verify section 12 "成熟度评估" contains TRL ratings
5. **Expected:** 649+ lines, 16 sections, PoC findings with concrete evidence

### 2. PoC Project Structure

1. Verify `poc/tauri-docufiller/package.json` exists with tauri dependencies
2. Verify `poc/tauri-docufiller/src-tauri/Cargo.toml` has tauri 2.x, tauri-plugin-dialog, tauri-plugin-shell
3. Verify `poc/tauri-docufiller/sidecar-dotnet/sidecar-dotnet.csproj` targets net8.0
4. **Expected:** All project files present with correct dependencies

### 3. PoC Code Quality

1. Read `poc/tauri-docufiller/src-tauri/src/lib.rs` — verify `open_file_dialog` and `start_sidecar` commands exist
2. Read `poc/tauri-docufiller/sidecar-dotnet/Program.cs` — verify `/api/health` and `/api/process/stream` endpoints
3. Read `poc/tauri-docufiller/src/app.js` — verify SSE ReadableStream consumption and progress bar update
4. **Expected:** Complete interaction flow: native dialog → sidecar HTTP → SSE progress → UI update

### 4. Independence from Main Project

1. Search poc/tauri-docufiller/ for references to ../../DocuFiller or parent project paths
2. **Expected:** Zero references — PoC is fully self-contained

## Edge Cases

### CSP Configuration

1. Read `poc/tauri-docufiller/src-tauri/tauri.conf.json`
2. Verify `connect-src` includes `http://localhost:5000` for sidecar communication
3. **Expected:** CSP allows sidecar HTTP connections

## Failure Signals

- cargo build fails → Rust toolchain or dependency issue
- dotnet build fails → .NET SDK or NuGet restore issue
- Research document missing sections → incomplete research coverage
- References to main project files → PoC not independent

## Not Proven By This UAT

- Live `tauri dev` runtime execution (requires WebView2 and interactive GUI)
- Actual .docx file processing (sidecar simulates processing with delays)
- Cross-platform compilation (Linux/macOS builds not tested)
- SSE connection stability under load or error conditions
- Sidecar lifecycle management (crash recovery, graceful shutdown)

## Notes for Tester

- rustc must be 1.88+ (upgraded from 1.86 during development due to time crate v0.3.47)
- On git worktrees, dotnet build may need explicit env vars: `$env:ProgramData`, `$env:APPDATA`, `$env:LOCALAPPDATA`
- The PoC uses `std::process::Command` for sidecar launching (not tauri-plugin-shell) — this is intentional for simplicity
- SSE uses ReadableStream API (not EventSource) for better error handling
