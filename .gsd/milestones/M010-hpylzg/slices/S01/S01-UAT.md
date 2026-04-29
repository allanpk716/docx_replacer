# S01: UpdateService 热重载 + appsettings.json 写回 — UAT

**Milestone:** M010-hpylzg
**Written:** 2026-04-29T09:32:25.573Z

# S01: UpdateService 热重载 + appsettings.json 写回 — UAT

**Milestone:** M010-hpylzg
**Written:** 2026-04-29

## UAT Type

- UAT mode: artifact-driven
- Why this mode is sufficient: This slice is a backend service method with no GUI. Correctness is fully verified through unit tests and interface contract inspection. No runtime server or user interaction is needed.

## Preconditions

- .NET 8 SDK installed
- Project builds successfully (`dotnet build`)
- Test runner available (`dotnet test`)

## Smoke Test

Run `dotnet test --filter "UpdateServiceTests" --verbosity normal` — all 21 tests pass, confirming the ReloadSource method works correctly for both in-memory hot-reload and appsettings.json persistence.

## Test Cases

### 1. HTTP Source Hot-Reload

1. Create UpdateService instance with default config (empty UpdateUrl → GitHub source)
2. Call `ReloadSource("http://192.168.1.100:8080", "beta")`
3. **Expected:** `UpdateSourceType` = "HTTP", `EffectiveUpdateUrl` = "http://192.168.1.100:8080/beta/"

### 2. GitHub Fallback

1. Create UpdateService with HTTP source configured
2. Call `ReloadSource("", "stable")`
3. **Expected:** `UpdateSourceType` = "GitHub", `EffectiveUpdateUrl` = ""

### 3. appsettings.json Persistence

1. Call `ReloadSource("http://example.com", "preview")`
2. Read appsettings.json from disk
3. **Expected:** `Update.UpdateUrl` = "http://example.com", `Update.Channel` = "preview", all other sections (Logging, Performance) unchanged

### 4. Write Failure Resilience

1. Set AppSettingsPath to a nonexistent directory path
2. Call `ReloadSource("http://example.com", "stable")`
3. **Expected:** No exception thrown, in-memory fields still updated (UpdateSourceType = "HTTP", EffectiveUpdateUrl = "http://example.com/stable/"), Warning log emitted

### 5. Null/Edge Case Handling

1. Call `ReloadSource(null, null)`
2. **Expected:** Treated as empty URL → GitHub source, no crash

## Edge Cases

### Trailing Slash Normalization
1. Call `ReloadSource("http://example.com/", "beta")`
2. **Expected:** EffectiveUpdateUrl = "http://example.com/beta/" (no double slash)

### Whitespace Trimming
1. Call `ReloadSource("  http://example.com  ", "  beta  ")
2. **Expected:** URL and channel trimmed before use

## Failure Signals

- Any test failure in `dotnet test --filter "UpdateServiceTests"` indicates a regression
- Compilation error on `dotnet build` indicates interface contract violation
- Missing `ReloadSource` or `EffectiveUpdateUrl` on `IUpdateService` indicates incomplete implementation

## Not Proven By This UAT

- Actual file system write to real appsettings.json (tests use temp files via AppSettingsPath injection)
- Integration with IConfiguration reloadOnChange (runtime config monitoring)
- End-to-end flow with CheckForUpdatesAsync using reloaded source
- Thread safety of concurrent ReloadSource calls

## Notes for Tester

- All verification is automated via unit tests — run the test command to validate
- The `internal AppSettingsPath` property is the test injection point for persistence tests
- StubUpdateService in CLI tests was updated to match the expanded interface — verify CLI tests also pass
- JsonNode.ToJsonString() uses JsonSerializerOptions (not JsonWriterOptions) in .NET 8
