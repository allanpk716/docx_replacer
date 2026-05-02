---
id: T01
parent: S01
milestone: M010-hpylzg
key_files:
  - Services/Interfaces/IUpdateService.cs
  - Services/UpdateService.cs
  - Tests/UpdateServiceTests.cs
  - Tests/DocuFiller.Tests/Cli/CliRunnerTests.cs
  - Tests/DocuFiller.Tests/Cli/UpdateCommandTests.cs
key_decisions:
  - (none)
duration: 
verification_result: passed
completed_at: 2026-04-29T09:26:15.980Z
blocker_discovered: false
---

# T01: Added ReloadSource method and EffectiveUpdateUrl property to IUpdateService, implemented in-memory hot-reload in UpdateService with 7 new unit tests

**Added ReloadSource method and EffectiveUpdateUrl property to IUpdateService, implemented in-memory hot-reload in UpdateService with 7 new unit tests**

## What Happened

Added `void ReloadSource(string updateUrl, string channel)` method and `string EffectiveUpdateUrl` property to `IUpdateService` interface. In `UpdateService`, removed `readonly` modifiers from `_updateSource`, `_updateUrl`, `_channel`, and `_sourceType` fields (kept `_isInstalled` readonly), promoted `EffectiveUpdateUrl` from `internal` to `public` interface member, and implemented `ReloadSource` with full in-memory source hot-reload logic (HTTP↔GitHub switching, null handling, channel trimming, structured logging of old→new transitions). Updated two existing `StubUpdateService` implementations in `CliRunnerTests` and `UpdateCommandTests` to satisfy the expanded interface. Added 7 new unit tests covering: HTTP source switch, GitHub fallback, channel update, null URL, null channel, trailing slash normalization, and whitespace trimming.

## Verification

Build: `dotnet build` — 0 errors. Tests: `dotnet test --filter "UpdateServiceTests" --verbosity normal` — all 17 tests passed (10 existing + 7 new). Must-haves verified: interface has ReloadSource + EffectiveUpdateUrl, ReloadSource correctly switches HTTP/GitHub and updates all fields, EffectiveUpdateUrl is public interface member, 7 new tests cover ReloadSource in-memory behavior, all existing tests still pass.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet build` | 0 | ✅ pass | 1930ms |
| 2 | `dotnet test --filter "UpdateServiceTests" --verbosity normal` | 0 | ✅ pass (17/17 tests) | 2580ms |

## Deviations

None. Plan executed as specified.

## Known Issues

None.

## Files Created/Modified

- `Services/Interfaces/IUpdateService.cs`
- `Services/UpdateService.cs`
- `Tests/UpdateServiceTests.cs`
- `Tests/DocuFiller.Tests/Cli/CliRunnerTests.cs`
- `Tests/DocuFiller.Tests/Cli/UpdateCommandTests.cs`
