---
id: T02
parent: S01
milestone: M010-hpylzg
key_files:
  - Services/UpdateService.cs
  - Tests/UpdateServiceTests.cs
key_decisions:
  - Used JsonSerializerOptions instead of JsonWriterOptions for JsonNode.ToJsonString() — .NET 8 API requires this type
duration: 
verification_result: passed
completed_at: 2026-04-29T09:30:29.785Z
blocker_discovered: false
---

# T02: Added appsettings.json write-back to ReloadSource via PersistToAppSettings with 4 new persistence tests

**Added appsettings.json write-back to ReloadSource via PersistToAppSettings with 4 new persistence tests**

## What Happened

Added `internal string AppSettingsPath` property to UpdateService for test-path injection, and a `PersistToAppSettings` private method that reads appsettings.json via `System.Text.Json.Nodes`, updates the `Update:UpdateUrl` and `Update:Channel` values, and writes back with indented formatting. The method is called at the end of `ReloadSource` after in-memory fields are updated. File write failures are caught and logged as Warning without throwing (in-memory hot-reload still succeeds). If appsettings.json doesn't exist, a Warning is logged and persistence is skipped.

Fixed a compilation error: `JsonNode.ToJsonString()` takes `JsonSerializerOptions` (not `JsonWriterOptions`) in .NET 8 — corrected the parameter type.

Added 4 new tests: (1) `ReloadSource_persists_to_appsettings_json` — verifies UpdateUrl and Channel written to temp JSON file, (2) `ReloadSource_empty_url_persists_empty_string` — verifies empty URL persists as "" with GitHub source type, (3) `ReloadSource_persistence_failure_does_not_throw` — sets AppSettingsPath to nonexistent directory, verifies no exception and in-memory fields still updated, (4) `ReloadSource_preserves_other_settings` — verifies Logging and Performance sections remain intact after write-back.

## Verification

Build: `dotnet build` — 0 errors. Tests: `dotnet test --filter "UpdateServiceTests" --verbosity normal` — all 21 tests passed (10 existing + 7 from T01 + 4 new persistence tests). All must-haves verified: PersistToAppSettings uses System.Text.Json.Nodes, ReloadSource calls it at end, AppSettingsPath supports test injection, file write failure doesn't throw, other config sections preserved, 4 new tests cover persistence scenarios.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet build --verbosity quiet` | 0 | ✅ pass | 1810ms |
| 2 | `dotnet test --filter "UpdateServiceTests" --verbosity normal` | 0 | ✅ pass (21/21 tests) | 2440ms |

## Deviations

Fixed `JsonWriterOptions` → `JsonSerializerOptions` in `PersistToAppSettings` — the plan specified `JsonWriterOptions` but `JsonNode.ToJsonString()` requires `JsonSerializerOptions` in .NET 8. Minor API correction.

## Known Issues

None.

## Files Created/Modified

- `Services/UpdateService.cs`
- `Tests/UpdateServiceTests.cs`
