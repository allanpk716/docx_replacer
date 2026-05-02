---
id: S01
parent: M010-hpylzg
milestone: M010-hpylzg
provides:
  - ["IUpdateService.ReloadSource(string updateUrl, string channel) — hot-reload method for runtime source switching", "IUpdateService.EffectiveUpdateUrl — public property returning current effective update URL (with channel path)", "IUpdateService.UpdateSourceType — property returning 'HTTP' or 'GitHub'", "appsettings.json Update:UpdateUrl and Update:Channel persistence via PersistToAppSettings"]
requires:
  []
affects:
  - ["S02"]
key_files:
  - ["Services/Interfaces/IUpdateService.cs", "Services/UpdateService.cs", "Tests/UpdateServiceTests.cs", "Tests/DocuFiller.Tests/Cli/CliRunnerTests.cs", "Tests/DocuFiller.Tests/Cli/UpdateCommandTests.cs"]
key_decisions:
  - ["Removed readonly from _updateSource/_updateUrl/_channel/_sourceType fields to enable runtime hot-reload", "Promoted EffectiveUpdateUrl from internal to public interface member for S02 GUI consumption", "Added internal AppSettingsPath property for test-path injection to avoid modifying real appsettings.json during tests", "PersistToAppSettings uses System.Text.Json.Nodes for JSON manipulation instead of full serializer round-trip"]
patterns_established:
  - ["Internal property injection pattern for test-path substitution (AppSettingsPath)", "Persist-to-config-file pattern: read JSON → modify nodes → write back, with failure resilience (catch + Warning log)"]
observability_surfaces:
  - ["Structured Information log on ReloadSource: old source type/URL/channel → new values", "Warning log when appsettings.json write fails", "Warning log when appsettings.json doesn't exist"]
drill_down_paths:
  - [".gsd/milestones/M010-hpylzg/slices/S01/tasks/T01-SUMMARY.md", ".gsd/milestones/M010-hpylzg/slices/S01/tasks/T02-SUMMARY.md"]
duration: ""
verification_result: passed
completed_at: 2026-04-29T09:32:25.573Z
blocker_discovered: false
---

# S01: UpdateService 热重载 + appsettings.json 写回

**Added ReloadSource method to IUpdateService with in-memory source hot-reload and appsettings.json persistence, validated by 21 unit tests**

## What Happened

T01 added `ReloadSource(string updateUrl, string channel)` and `EffectiveUpdateUrl` to `IUpdateService` interface, and implemented in-memory hot-reload in `UpdateService` — removing `readonly` from `_updateSource`, `_updateUrl`, `_channel`, `_sourceType` fields, and implementing source switching logic (HTTP when updateUrl non-empty, GitHub when empty). Also updated two `StubUpdateService` implementations in CLI tests. Added 7 unit tests covering HTTP/GitHub switching, null handling, channel updates, and URL normalization.

T02 added `PersistToAppSettings` private method using `System.Text.Json.Nodes` to read/write appsettings.json, updating `Update:UpdateUrl` and `Update:Channel` nodes. File write failures are caught and logged as Warning without throwing. Added `internal AppSettingsPath` property for test-path injection. Added 4 persistence tests covering write-back, empty URL, write failure resilience, and preservation of other config sections.

Total: 21 tests pass (10 existing + 11 new), 0 build errors, 0 regressions.

## Verification

Slice-level verification passed:
- `dotnet build` — 0 errors, 0 warnings
- `dotnet test --filter "UpdateServiceTests"` — 21/21 tests passed (10 existing + 7 T01 + 4 T02)
- Interface contract: IUpdateService has ReloadSource(string, string), EffectiveUpdateUrl (string), UpdateSourceType (string)
- Must-have ReloadSource("http://192.168.1.100:8080", "beta") → UpdateSourceType="HTTP", EffectiveUpdateUrl="http://192.168.1.100:8080/beta/" — verified by test ReloadSource_http_changes_source_type_to_HTTP
- Must-have ReloadSource("", "stable") → UpdateSourceType="GitHub" — verified by test ReloadSource_empty_changes_source_type_to_GitHub
- appsettings.json persistence — verified by 4 dedicated tests (write-back, empty URL, failure resilience, section preservation)
- Existing tests all pass with no regressions

## Requirements Advanced

- R044 — ReloadSource 方法实现内存热重载（HTTP/GitHub 切换）+ appsettings.json 持久化，通过 21 个单元测试验证，包括写入失败不抛异常

## Requirements Validated

- R044 — 21/21 UpdateServiceTests 通过：7 个内存热重载测试 + 4 个持久化测试 + 10 个现有测试无回归。dotnet build 0 错误。接口契约满足。

## New Requirements Surfaced

None.

## Requirements Invalidated or Re-scoped

None.

## Operational Readiness

None.

## Deviations

None.

## Known Limitations

None.

## Follow-ups

None.

## Files Created/Modified

None.
