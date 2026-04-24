---
phase: execution
phase_name: M007-wpaxa3 Execution
project: DocuFiller
generated: 2026-04-24T06:45:00Z
counts:
  decisions: 4
  lessons: 2
  patterns: 4
  surprises: 1
missing_artifacts: []
---

### Decisions

- **Velopack as update framework (D016):** Selected Velopack (Squirrel/Clowd.Squirrel successor) over AutoUpdater.NET and custom solutions due to internal network friendliness (HTTP static files only), single-EXE support, delta updates, and active maintenance (3900+ commits). Not revisable — core architectural choice.
  Source: M007-wpaxa3-CONTEXT.md/Architectural Decisions

- **Update config in appsettings.json (D017):** Placed Update:UpdateUrl in appsettings.json instead of App.config for consistency with existing config patterns. Empty string = "not configured" pattern. Revisable if multi-environment config needed.
  Source: M007-wpaxa3-CONTEXT.md/Architectural Decisions

- **No PublishTrimmed (D018):** PublishSingleFile=true but PublishTrimmed=false because EPPlus and OpenXML use reflection extensively. 80-120MB binary size acceptable for internal network deployment. Revisable if size reduction needed and trimming safety verified.
  Source: M007-wpaxa3-CONTEXT.md/Architectural Decisions

- **Custom WPF update dialogs (D019):** Not using Velopack built-in WinForms dialogs to maintain visual consistency with WPF UI theme. Custom MessageBox-based confirmation and progress.
  Source: M007-wpaxa3-CONTEXT.md/Architectural Decisions

### Lessons

- **Velopack NuGet transitive dependency via wildcard Compile includes:** Test projects using `<Compile Include="..\..\Services\Interfaces\*.cs" .../>` auto-include IUpdateService.cs which references Velopack.UpdateInfo, requiring Velopack package reference in all test csprojs. This was not anticipated in the original plan.
  Source: S01-SUMMARY.md/Key Decisions

- **Velopack 0.0.1298 ApplyUpdatesAndRestart requires VelopackAsset parameter:** Unlike the intuitive "apply all pending" pattern, ApplyUpdatesAndRestart needs a specific VelopackAsset from UpdatePendingRestart. Must download first, then read the property, then pass it.
  Source: S02-SUMMARY.md/Key Decisions

### Patterns

- **VelopackApp.Build().Run() as first line of Main():** Velopack convention requires Build().Run() before any CLI/GUI branching so it can intercept install/update/restart hooks before WPF initialization.
  Source: S01-SUMMARY.md/Patterns Established

- **Per-method UpdateManager instance pattern:** Each UpdateService API method creates an independent UpdateManager instance rather than sharing one, avoiding Velopack internal state management issues.
  Source: S02-SUMMARY.md/Patterns Established

- **Optional constructor injection for backward compatibility:** MainWindowViewModel accepts `IUpdateService? updateService = null` as optional parameter, maintaining compatibility with existing DI registration and test scenarios.
  Source: S02-SUMMARY.md/Patterns Established

- **Phase-labeled echo messages in build scripts:** Build scripts use `[PHASE_NAME] SUCCESS/FAILED` tagged echo messages for observability during long-running build processes.
  Source: S03-SUMMARY.md/Patterns Established

### Surprises

- **Velopack UpdateManager cannot function in non-Velopack environments:** When running via `dotnet run` (not vpk-packaged), UpdateManager cannot connect to update sources or perform actual updates. Full E2E verification requires vpk-packaged binaries on clean Windows, making automated CI testing of the update flow impractical.
  Source: S02-SUMMARY.md/Known Limitations
