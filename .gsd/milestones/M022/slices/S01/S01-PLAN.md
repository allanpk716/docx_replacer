# S01: Electron.NET PoC + 调研文档

**Goal:** 创建可编译运行的 Electron.NET 迷你 DocuFiller PoC 项目，并撰写完整的 Electron.NET 技术调研报告。PoC 实现：文件选择→模拟文档处理→进度条显示，验证 Electron.NET 的前后端通信（IPC）、原生对话框、进度汇报等关键集成点。
**Demo:** 能编译运行的 Electron.NET 迷你 DocuFiller，附完整调研报告

## Must-Haves

- poc/electron-net-docufiller/ 项目在 Windows 上 `dotnet build` 编译通过
- PoC 包含：Electron.NET 窗口启动、文件选择对话框、IPC 通信、进度条模拟
- docs/cross-platform-research/electron-net-research.md 包含：技术概述、架构分析、.NET 8 兼容性、DocuFiller 适配性、优缺点、成熟度评估、社区活跃度等章节
- PoC 代码完全独立于现有 DocuFiller 项目，不修改任何现有文件

## Proof Level

- This slice proves: contract — PoC proves the Electron.NET project can be scaffolded, compiled, and demonstrates the key integration patterns (IPC, native dialogs, progress) that DocuFiller would need. Real runtime verification (electronize start) requires Node.js + interactive GUI and is done manually; automated verification is limited to compilation success and code structure checks.

## Integration Closure

- Upstream surfaces consumed: none (first PoC slice, no prior dependencies)
- New wiring introduced: poc/electron-net-docufiller/ is a self-contained ASP.NET Core + Electron.NET project
- What remains: S05 (最终评估) will consume the research document and PoC findings for cross-scheme comparison

## Verification

- Runtime signals: Electron.NET Console.WriteLine and Electron.IpcMain.On logging to stdout/stderr; ASP.NET Core default logging
- Inspection surfaces: `electronize start` console output for IPC message traces; project build output for dependency resolution
- Failure visibility: build errors surface immediately; runtime IPC failures visible in Electron dev tools console

## Tasks

- [ ] **T01: Scaffold Electron.NET project and verify toolchain** `est:1h`
  Initialize an ASP.NET Core + Electron.NET project in poc/electron-net-docufiller/. Set up the project structure, install ElectronNET.API NuGet package and electronize CLI tool, configure Program.cs for Electron.NET hosting, create a basic HTML frontend that opens in an Electron window. Verify the entire toolchain compiles (`dotnet build` succeeds).
  - Files: `poc/electron-net-docufiller/electron-net-docufiller.csproj`, `poc/electron-net-docufiller/Program.cs`, `poc/electron-net-docufiller/Startup.cs`, `poc/electron-net-docufiller/Properties/launchSettings.json`, `poc/electron-net-docufiller/wwwroot/index.html`, `poc/electron-net-docufiller/electron.manifest.json`
  - Verify: cd poc/electron-net-docufiller && dotnet build

- [ ] **T02: Implement mini DocuFiller PoC with IPC, file dialog, and progress bar** `est:2h`
  Build the mini DocuFiller PoC on top of T01's scaffold. Implement:
  - Files: `poc/electron-net-docufiller/Controllers/ProcessingController.cs`, `poc/electron-net-docufiller/Services/SimulatedProcessor.cs`, `poc/electron-net-docufiller/wwwroot/index.html`, `poc/electron-net-docufiller/wwwroot/css/app.css`, `poc/electron-net-docufiller/wwwroot/js/app.js`, `poc/electron-net-docufiller/Startup.cs`
  - Verify: cd poc/electron-net-docufiller && dotnet build

- [ ] **T03: Write Electron.NET comprehensive research document** `est:1.5h`
  Write the complete Electron.NET research document at docs/cross-platform-research/electron-net-research.md. This is the primary deliverable for S05's cross-scheme comparison.
  - Files: `docs/cross-platform-research/electron-net-research.md`
  - Verify: test -f docs/cross-platform-research/electron-net-research.md && grep -c "^## " docs/cross-platform-research/electron-net-research.md | grep -qE '^[0-9]+$' && echo "Sections found"

## Files Likely Touched

- poc/electron-net-docufiller/electron-net-docufiller.csproj
- poc/electron-net-docufiller/Program.cs
- poc/electron-net-docufiller/Startup.cs
- poc/electron-net-docufiller/Properties/launchSettings.json
- poc/electron-net-docufiller/wwwroot/index.html
- poc/electron-net-docufiller/electron.manifest.json
- poc/electron-net-docufiller/Controllers/ProcessingController.cs
- poc/electron-net-docufiller/Services/SimulatedProcessor.cs
- poc/electron-net-docufiller/wwwroot/css/app.css
- poc/electron-net-docufiller/wwwroot/js/app.js
- docs/cross-platform-research/electron-net-research.md
