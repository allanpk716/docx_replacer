---
estimated_steps: 30
estimated_files: 9
skills_used: []
---

# T01: Scaffold Tauri v2 + .NET sidecar project and verify toolchain

Create the complete Tauri v2 + .NET sidecar project structure in poc/tauri-docufiller/. This task proves the toolchain works end-to-end on Windows: Tauri CLI, Cargo/Rust compilation, .NET sidecar compilation, and basic Tauri window rendering.

The project structure:
- src-tauri/ — Tauri v2 Rust backend (Cargo.toml, tauri.conf.json, lib.rs)
- src/ — Vanilla HTML/JS frontend served by Tauri's asset protocol
- sidecar-dotnet/ — .NET 8 minimal API console app (sidecar HTTP server)

Steps:
1. Install Tauri CLI as local dev dependency: add @tauri-apps/cli to package.json
2. Create package.json with tauri scripts (tauri dev, tauri build)
3. Create src-tauri/Cargo.toml with tauri 2.x dependency, tauri-plugin-dialog, tauri-plugin-shell
4. Create src-tauri/tauri.conf.json — configure app identifier (com.docufiller.tauri-poc), window title, frontendDist pointing to ../src, CSP allowing localhost:5000 for sidecar communication, dialog and shell plugins enabled
5. Create src-tauri/capabilities/default.json — grant dialog:default, shell:allow-open permissions
6. Create src-tauri/build.rs (standard tauri_build::build())
7. Create src-tauri/src/lib.rs — minimal Tauri app with plugin registrations (dialog, shell) and a basic greet command
8. Create src/index.html — minimal HTML page with a heading and basic JS to verify Tauri loads
9. Create sidecar-dotnet/sidecar-dotnet.csproj — .NET 8 console app with ASP.NET Core references
10. Create sidecar-dotnet/Program.cs — minimal Kestrel HTTP server with a /api/health endpoint
11. Run `dotnet build` in sidecar-dotnet/ — verify .NET sidecar compiles
12. Run `cargo build` in src-tauri/ — verify Tauri Rust backend compiles (first build downloads+compiles all crates, expect 5-10 min)

Must-Haves:
- [ ] poc/tauri-docufiller/package.json with tauri scripts
- [ ] poc/tauri-docufiller/src-tauri/Cargo.toml with tauri 2.x + plugins
- [ ] poc/tauri-docufiller/src-tauri/tauri.conf.json properly configured
- [ ] poc/tauri-docufiller/src-tauri/src/lib.rs with plugin registrations
- [ ] poc/tauri-docufiller/sidecar-dotnet/ compiles with `dotnet build`
- [ ] poc/tauri-docufiller/src-tauri/ compiles with `cargo build`

Key constraints:
- Use PowerShell for `dotnet build` (Git Bash may miss env vars — see S01 experience)
- Tauri v2 uses `tauri::Builder::default()` in lib.rs, not main.rs
- CSP in tauri.conf.json must allow connect-src to http://localhost:5000 for sidecar HTTP
- sidecar-dotnet targets net8.0 (matching DocuFiller), not net9.0

## Inputs

- `DocuFiller.csproj — reference for .NET 8 target framework and dependency patterns`
- `poc/electron-net-docufiller/electron-net-docufiller.csproj — reference for S01 PoC project structure`

## Expected Output

- `poc/tauri-docufiller/package.json — Node project with Tauri CLI and dev scripts`
- `poc/tauri-docufiller/src-tauri/Cargo.toml — Rust manifest with tauri 2.x and plugin dependencies`
- `poc/tauri-docufiller/src-tauri/tauri.conf.json — Tauri v2 config with window, CSP, and plugin settings`
- `poc/tauri-docufiller/src-tauri/capabilities/default.json — Permission grants for dialog and shell plugins`
- `poc/tauri-docufiller/src-tauri/build.rs — Standard Tauri build script`
- `poc/tauri-docufiller/src-tauri/src/lib.rs — Tauri app setup with plugin registrations and greet command`
- `poc/tauri-docufiller/src/index.html — Minimal HTML frontend`
- `poc/tauri-docufiller/sidecar-dotnet/sidecar-dotnet.csproj — .NET 8 console app with ASP.NET Core`
- `poc/tauri-docufiller/sidecar-dotnet/Program.cs — Minimal Kestrel server with /api/health endpoint`

## Verification

powershell -Command "cd poc/tauri-docufiller/sidecar-dotnet; dotnet build" (exit code 0) AND cd poc/tauri-docufiller/src-tauri && cargo build (exit code 0)
