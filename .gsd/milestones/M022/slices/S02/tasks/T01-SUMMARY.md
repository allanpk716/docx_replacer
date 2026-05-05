---
id: T01
parent: S02
milestone: M022
key_files:
  - poc/tauri-docufiller/package.json
  - poc/tauri-docufiller/src-tauri/Cargo.toml
  - poc/tauri-docufiller/src-tauri/tauri.conf.json
  - poc/tauri-docufiller/src-tauri/capabilities/default.json
  - poc/tauri-docufiller/src-tauri/build.rs
  - poc/tauri-docufiller/src-tauri/src/lib.rs
  - poc/tauri-docufiller/src-tauri/src/main.rs
  - poc/tauri-docufiller/src/index.html
  - poc/tauri-docufiller/sidecar-dotnet/sidecar-dotnet.csproj
  - poc/tauri-docufiller/sidecar-dotnet/Program.cs
key_decisions:
  - Upgraded rustc 1.86→1.95 (Tauri v2.11 requires time crate 0.3.47+ needing rustc 1.88+)
  - Removed reqwest from Rust backend; sidecar health check done via frontend JS fetch() instead
  - Sidecar health check via CSP connect-src (http://localhost:5000) from frontend, not Rust HTTP client
duration: 
verification_result: passed
completed_at: 2026-05-04T16:04:41.381Z
blocker_discovered: false
---

# T01: Scaffold Tauri v2 + .NET sidecar DocuFiller PoC project with both toolchains verified compiling

**Scaffold Tauri v2 + .NET sidecar DocuFiller PoC project with both toolchains verified compiling**

## What Happened

Created the complete Tauri v2 + .NET sidecar project structure in `poc/tauri-docufiller/` with all 10 required files:

**Tauri v2 backend (src-tauri/):**
- `Cargo.toml` — tauri 2.x, tauri-plugin-dialog 2, tauri-plugin-shell 2, serde
- `tauri.conf.json` — app identifier `com.docufiller.tauri-poc`, window config, CSP allowing `connect-src http://localhost:5000` for sidecar communication, dialog/shell plugins enabled
- `capabilities/default.json` — grants core:default, dialog:default, shell:allow-open permissions
- `build.rs` — standard tauri_build::build()
- `src/lib.rs` — Tauri app with dialog + shell plugin registrations and a `greet` command
- `src/main.rs` — Windows entry point calling lib::run()
- `icons/` — placeholder 32x32.png, 128x128.png, icon.ico

**Frontend (src/):**
- `index.html` — minimal HTML page with Tauri runtime detection, greet command test, and sidecar health check via JavaScript fetch() (leveraging CSP connect-src)

**.NET sidecar (sidecar-dotnet/):**
- `sidecar-dotnet.csproj` — .NET 8 console app with ASP.NET Core (Microsoft.NET.Sdk.Web)
- `Program.cs` — minimal Kestrel HTTP server listening on `http://localhost:5000` with `/api/health` endpoint

**Key decisions made during execution:**
1. Upgraded rustc from 1.86.0 to 1.95.0 — Tauri v2.11's transitive `time` crate (v0.3.47) requires rustc 1.88+, and `cargo update --precise` cannot downgrade because `plist v1.9.0` pins `time = "^0.3.47"`.
2. Removed `reqwest` from Rust backend — sidecar health check moved to frontend JavaScript `fetch()` instead. This avoids heavy networking dependencies in Rust and keeps the backend minimal. The CSP `connect-src http://localhost:5000` already allows this pattern.
3. Used PowerShell with explicit env vars (ProgramData, APPDATA, LOCALAPPDATA) and full path to dotnet.exe for builds in the git worktree environment, as bash env vars don't propagate to Windows shell API that NuGet uses internally.

**Both builds verified passing:**
- `dotnet build` in sidecar-dotnet/ → exit 0, 0 warnings, 0 errors
- `cargo build` in src-tauri/ → exit 0, compiled successfully in ~15s

## Verification

Both toolchain builds verified passing:

1. **dotnet build** (sidecar-dotnet/): Run via PowerShell with env vars set for worktree compatibility. Output: "已成功生成" (Build succeeded), 0 warnings, 0 errors, exit code 0. The sidecar DLL was produced at sidecar-dotnet/bin/Debug/net8.0/sidecar-dotnet.dll.

2. **cargo build** (src-tauri/): Compiled all Tauri dependencies + tauri-docufiller crate. Output: "Finished dev profile [unoptimized + debuginfo] target(s) in 15.92s", exit code 0. Required upgrading rustc from 1.86.0 to 1.95.0 to satisfy time crate v0.3.47 dependency.

3. **Project structure verified**: All 10 expected files present (package.json, Cargo.toml, tauri.conf.json, capabilities/default.json, build.rs, lib.rs, main.rs, index.html, sidecar-dotnet.csproj, Program.cs) plus icon assets.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `powershell.exe -NoProfile -Command '...; Start-Process -FilePath "C:\Program Files\dotnet\dotnet.exe" -ArgumentList "build" ...'` | 0 | ✅ pass | 1280ms |
| 2 | `cd poc/tauri-docufiller/src-tauri && cargo build` | 0 | ✅ pass | 15920ms |

## Deviations

1. Added main.rs entry point (plan didn't mention it, but Tauri v2 lib.rs pattern requires a main.rs that calls lib::run()).
2. Added icon assets (32x32.png, 128x128.png, icon.ico) — required by tauri-build, not mentioned in plan.
3. Removed reqwest dependency — plan implied check_sidecar_health in Rust, but transitive dependency issues made this impractical. Moved health check to frontend JavaScript fetch() instead, which works via CSP connect-src.
4. Upgraded rustc from 1.86.0 to 1.95.0 to resolve Tauri v2.11 build failure with time crate v0.3.47+.

## Known Issues

None.

## Files Created/Modified

- `poc/tauri-docufiller/package.json`
- `poc/tauri-docufiller/src-tauri/Cargo.toml`
- `poc/tauri-docufiller/src-tauri/tauri.conf.json`
- `poc/tauri-docufiller/src-tauri/capabilities/default.json`
- `poc/tauri-docufiller/src-tauri/build.rs`
- `poc/tauri-docufiller/src-tauri/src/lib.rs`
- `poc/tauri-docufiller/src-tauri/src/main.rs`
- `poc/tauri-docufiller/src/index.html`
- `poc/tauri-docufiller/sidecar-dotnet/sidecar-dotnet.csproj`
- `poc/tauri-docufiller/sidecar-dotnet/Program.cs`
