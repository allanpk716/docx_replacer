---
phase: complete-milestone
phase_name: M022 Milestone Completion
project: DocuFiller
generated: "2026-05-04T18:00:00Z"
counts:
  decisions: 5
  lessons: 6
  patterns: 3
  surprises: 3
missing_artifacts: []
---

# M022 Learnings

## Decisions

- **Chose SSE over framework-specific IPC for progress reporting in both PoCs** — SSE is HTTP-based, portable across Electron.NET and Tauri, and testable without any desktop runtime. Both PoCs independently converged on this pattern.
  Source: S01-SUMMARY.md/Key decisions

- **Chose std::process::Command over tauri-plugin-shell for sidecar launch** — Simpler dependency chain, fewer Tauri plugin version constraints, and sufficient for the use case of launching a single .NET process.
  Source: S02-SUMMARY.md/Key decisions

- **Removed reqwest from Rust backend; moved sidecar health check to frontend JavaScript** — Eliminated a heavy Rust dependency by leveraging the frontend's existing CSP connect-src capability to poll the sidecar HTTP endpoint directly.
  Source: S02-SUMMARY.md/Deviations

- **Adopted 9-dimension weighted scoring for framework comparison** — Technical feasibility (15%), WPF migration (15%), cross-platform coverage (15%), migration cost (10%), performance (10%), ecosystem (10%), Velopack compatibility (5%), community (10%), long-term maintenance (10%).
  Source: S05-SUMMARY.md/Key decisions

- **Recommended Avalonia UI (#1, 4.3/5) over Tauri (#2, 3.8/5) for DocuFiller migration** — XAML compatibility with WPF allows incremental migration, Velopack integrates seamlessly, and all service-layer code (11 interfaces) requires zero modification. Tauri requires full UI rewrite and Rust skills.
  Source: S05-SUMMARY.md/Key decisions

## Lessons

- **dotnet build in git worktrees on Windows requires PowerShell, not bash** — Git worktrees don't set APPDATA/LOCALAPPDATA/ProgramData env vars that dotnet needs for NuGet restore. Always use PowerShell or set env vars explicitly.
  Source: S01-SUMMARY.md/What Happened

- **Tauri v2 has a hidden rustc MSRV requirement (1.88+)** — The time crate (transitive dependency of tauri v2.11) requires rustc 1.88+, but Tauri's own docs don't prominently advertise this. Upgrading from 1.86 to 1.95 was necessary.
  Source: S02-SUMMARY.md/Key decisions

- **Electron.NET's API uses .BrowserWindows not .Windows** — Undocumented API surface; the standard ElectronJS API naming doesn't match Electron.NET's C# wrapper.
  Source: S01-SUMMARY.md/What Happened

- **All 16 core DocuFiller NuGet dependencies are pure managed implementations** — OpenXml, EPPlus, CommunityToolkit.Mvvm, Velopack, etc. all support net8.0 cross-platform. Migration risk is entirely in the UI layer (WPF → new framework), not business logic.
  Source: S04-SUMMARY.md/Key decisions

- **Self-contained PoC directories need their own nuget.config and global.json** — Without these, PoC projects inherit the main project's SDK version and NuGet source constraints, which can cause build failures.
  Source: S01-SUMMARY.md/Patterns established

- **Unix verification commands (test, wc) don't work on Windows** — Task plans should always provide Windows-compatible verification commands or use cross-platform alternatives.
  Source: S02-SUMMARY.md/Deviations

## Patterns

- **SSE-based progress reporting as portable cross-framework pattern** — Both Electron.NET and Tauri PoCs converged on SSE (Server-Sent Events) for progress streaming instead of framework-specific IPC (Electron IPC / Tauri commands). This makes the backend testable without any desktop runtime.
  Source: S01-SUMMARY.md/Patterns established

- **HybridSupport.IsElectronActive guard pattern** — Wrap Electron-specific code in runtime checks so the same ASP.NET Core app works in both Electron and browser contexts. Equivalent pattern in Tauri: CSP connect-src for frontend-origin detection.
  Source: S01-SUMMARY.md/Patterns established

- **Self-contained PoC project structure** — Each PoC lives in its own directory with own project file, dependency config (nuget.config/global.json for .NET, package.json for Node), and no references to the main DocuFiller project.
  Source: S01-SUMMARY.md/Patterns established

## Surprises

- **Electron.NET community decline risk** — Despite being at TRL 6 (production-viable), Electron.NET lags behind upstream Electron by major versions and has sparse commit activity. The community risk is higher than the technical risk.
  Source: S01-SUMMARY.md/Key decisions

- **Blazor Hybrid has no Linux support at all** — A Microsoft-backed framework that only covers Windows and macOS. TRL 4 on Linux. This is a critical gap for a cross-platform migration.
  Source: S03-SUMMARY.md/Key decisions (inferred from blazor-hybrid-research.md)

- **DocuFiller's migration workload is heavily UI-concentrated** — Only 5 file dialog call sites and 1 drag-drop file need rewriting. The entire service layer (11 interfaces), CLI, and update server are already cross-platform compatible.
  Source: S04-SUMMARY.md/Key decisions
