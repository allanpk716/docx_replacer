# S01: Electron.NET PoC + 调研文档 — UAT

**Milestone:** M022
**Written:** 2026-05-04T15:38:26.708Z

# S01: Electron.NET PoC + 调研文档 — UAT

**Milestone:** M022
**Written:** 2026-05-04

## UAT Type

- UAT mode: artifact-driven
- Why this mode is sufficient: The PoC requires Node.js + interactive GUI for live Electron runtime (`electronize start`), which cannot be automated in this environment. Automated verification covers compilation success, code structure, and document completeness — sufficient to validate the research deliverables and prove the integration patterns compile correctly.

## Preconditions

- .NET 8 SDK installed
- Windows build environment with PowerShell access
- NuGet packages accessible (requires ProgramData/APPDATA/LOCALAPPDATA env vars — set automatically by PowerShell)

## Smoke Test

1. Open PowerShell
2. `cd poc/electron-net-docufiller && dotnet build`
3. **Expected:** Build succeeds with 0 errors, 0 warnings; `electron-net-docufiller.dll` present in `bin/Debug/net8.0/`

## Test Cases

### 1. Project Compilation

1. Run `powershell.exe -Command 'cd poc/electron-net-docufiller; dotnet build'`
2. **Expected:** Exit code 0, output shows "Build succeeded" with 0 errors

### 2. Research Document Completeness

1. Open `docs/cross-platform-research/electron-net-research.md`
2. Verify document contains sections for: 技术概述, DocuFiller 适配性, IPC 通信, NuGet 生态, .NET 8 支持, 跨平台覆盖, 打包分发, 社区活跃度, 性能特征, SWOT 分析, 成熟度评估, PoC 发现
3. **Expected:** All 13 required topics present; document has 12+ `##` headings

### 3. PoC Code Structure

1. Verify these files exist in `poc/electron-net-docufiller/`:
   - `Program.cs` (Electron bootstrap + DI + IPC handlers)
   - `Controllers/ProcessingController.cs` (3 API endpoints)
   - `Services/SimulatedProcessor.cs` (5-step mock processor)
   - `wwwroot/index.html` + `wwwroot/css/app.css` + `wwwroot/js/app.js` (frontend UI)
   - `electron.manifest.json` (Electron config)
   - `electron-net-docufiller.csproj` (project file)
2. **Expected:** All files present and non-empty

### 4. PoC Independence from Main Project

1. Verify no files in the main DocuFiller project were modified
2. Verify `poc/electron-net-docufiller/` is fully self-contained with its own .csproj, nuget.config, global.json
3. **Expected:** PoC has zero dependencies on parent project files

## Edge Cases

### Browser-Only Mode (No Electron Runtime)

1. Run `dotnet run` (without `electronize start`) in `poc/electron-net-docufiller/`
2. Navigate to the displayed localhost URL
3. **Expected:** Web UI loads; IPC status shows "Not running in Electron"; file dialog falls back gracefully; processing still works via SSE

## Failure Signals

- `dotnet build` returns non-zero exit code or compilation errors
- Research document missing required sections (fewer than 12 `##` headings)
- PoC files reference parent project namespaces or assemblies
- Build fails with NuGet restore errors (env var issue — use PowerShell)

## Not Proven By This UAT

- Live Electron window rendering (requires `electronize start` + Node.js + interactive GUI)
- Native file dialog actually opening (requires Electron runtime)
- Cross-platform compilation on Linux/macOS (out of scope for this PoC)
- Performance under real document processing loads
- Actual DocuFiller business logic integration (PoC uses simulated processor)

## Notes for Tester

- Build MUST be run via PowerShell on Windows — bash lacks ProgramData/APPDATA/LOCALAPPDATA env vars
- `electronize start` requires Node.js installed and available on PATH
- The PoC uses ElectronNET.API 23.6.2 which targets net6.0 but is forward-compatible with net8.0
- SSE progress endpoint is at `GET /api/process?file=example.docx`
- IPC status endpoint is at `GET /api/ipc/status`
