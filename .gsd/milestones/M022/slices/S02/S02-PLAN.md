# S02: Tauri + .NET sidecar PoC + 调研文档

**Goal:** 创建可编译运行的 Tauri v2 + .NET sidecar 迷你 DocuFiller PoC 项目，并撰写完整的 Tauri + .NET sidecar 技术调研报告。PoC 实现：Tauri 原生文件选择对话框→.NET sidecar HTTP API 模拟文档处理→SSE 进度推送到前端，验证 Tauri 前端与 .NET 后端的集成模式、CSP 配置、跨进程通信等关键集成点。
**Demo:** 能编译运行的 Tauri + .NET sidecar 迷你 DocuFiller，附完整调研报告

## Must-Haves

- poc/tauri-docufiller/ 下 Tauri Rust 后端 `cargo build` 编译通过
- poc/tauri-docufiller/sidecar-dotnet/ 下 .NET sidecar `dotnet build` 编译通过
- PoC 包含：Tauri 原生文件选择对话框（dialog 插件）、.NET sidecar HTTP API（文件处理模拟）、SSE 进度推送、前端进度条 UI
- docs/cross-platform-research/tauri-dotnet-research.md 包含 12+ 章节，覆盖技术概述、DocuFiller 适配性、IPC 机制、sidecar 模式、跨平台支持、优缺点、成熟度评估等
- PoC 代码完全独立于现有 DocuFiller 项目，不修改任何现有文件

## Proof Level

- This slice proves: contract — PoC proves the Tauri v2 + .NET sidecar project can be scaffolded and compiled, demonstrating key integration patterns (native dialog, HTTP sidecar communication, SSE progress). Real runtime verification (tauri dev) requires WebView2 and interactive GUI; automated verification is compilation success and code structure checks.

## Integration Closure

Upstream surfaces consumed: none (independent PoC slice, no prior dependencies)
New wiring introduced: poc/tauri-docufiller/ is a self-contained Tauri v2 app + .NET sidecar; the two processes communicate via HTTP (Kestrel) with SSE progress streaming
What remains: S05 (最终评估) will consume the research document and PoC findings for cross-scheme comparison

## Verification

- Runtime signals: .NET sidecar logs to console via ASP.NET Core default logging (ILogger); Tauri Rust backend logs via tauri::api logging macros; SSE event stream visible in browser dev tools
- Inspection surfaces: `cargo build` and `dotnet build` output for dependency resolution; .NET sidecar console output for HTTP request logs; Tauri dev console for frontend errors
- Failure visibility: Rust compilation errors surface immediately; .NET build errors via standard dotnet CLI; CSP violations visible in Tauri dev console

## Tasks

- [ ] **T01: Scaffold Tauri v2 + .NET sidecar project and verify toolchain** `est:1.5h`
  Create the complete Tauri v2 + .NET sidecar project structure in poc/tauri-docufiller/. This task proves the toolchain works end-to-end on Windows: Tauri CLI, Cargo/Rust compilation, .NET sidecar compilation, and basic Tauri window rendering.
  - Files: `poc/tauri-docufiller/package.json`, `poc/tauri-docufiller/src-tauri/Cargo.toml`, `poc/tauri-docufiller/src-tauri/tauri.conf.json`, `poc/tauri-docufiller/src-tauri/capabilities/default.json`, `poc/tauri-docufiller/src-tauri/build.rs`, `poc/tauri-docufiller/src-tauri/src/lib.rs`, `poc/tauri-docufiller/src/index.html`, `poc/tauri-docufiller/sidecar-dotnet/sidecar-dotnet.csproj`, `poc/tauri-docufiller/sidecar-dotnet/Program.cs`
  - Verify: powershell -Command "cd poc/tauri-docufiller/sidecar-dotnet; dotnet build" (exit code 0) AND cd poc/tauri-docufiller/src-tauri && cargo build (exit code 0)

- [ ] **T02: Implement mini DocuFiller PoC with native dialog, sidecar IPC, and progress bar** `est:2h`
  Build the mini DocuFiller PoC on top of T01's scaffold. Implement the complete interaction flow: user clicks to select a file via Tauri native dialog → Tauri command sends file path to .NET sidecar HTTP API → sidecar simulates processing with SSE progress updates → frontend displays real-time progress bar.
  - Files: `poc/tauri-docufiller/src-tauri/src/lib.rs`, `poc/tauri-docufiller/src/index.html`, `poc/tauri-docufiller/src/app.js`, `poc/tauri-docufiller/src/styles.css`, `poc/tauri-docufiller/sidecar-dotnet/Program.cs`, `poc/tauri-docufiller/src-tauri/tauri.conf.json`
  - Verify: powershell -Command "cd poc/tauri-docufiller/sidecar-dotnet; dotnet build" (exit code 0) AND cd poc/tauri-docufiller/src-tauri && cargo build (exit code 0)

- [ ] **T03: Write Tauri + .NET sidecar comprehensive research document** `est:1.5h`
  Write the complete Tauri + .NET sidecar research document at docs/cross-platform-research/tauri-dotnet-research.md. This is the primary deliverable for S05's cross-scheme comparison.
  - Files: `docs/cross-platform-research/tauri-dotnet-research.md`
  - Verify: test -f docs/cross-platform-research/tauri-dotnet-research.md && wc -l docs/cross-platform-research/tauri-dotnet-research.md

## Files Likely Touched

- poc/tauri-docufiller/package.json
- poc/tauri-docufiller/src-tauri/Cargo.toml
- poc/tauri-docufiller/src-tauri/tauri.conf.json
- poc/tauri-docufiller/src-tauri/capabilities/default.json
- poc/tauri-docufiller/src-tauri/build.rs
- poc/tauri-docufiller/src-tauri/src/lib.rs
- poc/tauri-docufiller/src/index.html
- poc/tauri-docufiller/sidecar-dotnet/sidecar-dotnet.csproj
- poc/tauri-docufiller/sidecar-dotnet/Program.cs
- poc/tauri-docufiller/src/app.js
- poc/tauri-docufiller/src/styles.css
- docs/cross-platform-research/tauri-dotnet-research.md
