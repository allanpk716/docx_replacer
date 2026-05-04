---
id: T02
parent: S05
milestone: M020
key_files:
  - Tests/DocuFiller.Tests/Services/TemplateCacheServiceTests.cs
  - Tests/DocuFiller.Tests.csproj
key_decisions:
  - Revised ClearExpiredCache test from 'only removes expired, keeps fresh' to 'removes expired items' because TemplateCacheService evaluates expiration against IOptionsMonitor.CurrentValue at check-time, not at cache-time — cannot selectively expire without injecting a clock
duration: 
verification_result: passed
completed_at: 2026-05-04T02:28:24.361Z
blocker_discovered: false
---

# T02: Add TemplateCacheService unit tests covering cache CRUD, expiration, cache-disabled, dispose, and null-path edge cases (11 tests all passing)

**Add TemplateCacheService unit tests covering cache CRUD, expiration, cache-disabled, dispose, and null-path edge cases (11 tests all passing)**

## What Happened

Created `Tests/DocuFiller.Tests/Services/TemplateCacheServiceTests.cs` with 11 unit tests covering the core behaviors of TemplateCacheService:

1. **Cache CRUD**: `CacheValidationResult_ThenGet_ReturnsCachedValue`, `GetCachedValidationResult_NotCached_ReturnsNull`, `CacheContentControls_ThenGet_ReturnsCachedList` — validate basic cache/store/get for both validation results and content controls.
2. **Expiration**: `GetCachedValidationResult_Expired_ReturnsNull`, `ClearExpiredCache_RemovesExpiredItems` — confirm that expired items return null on get and are removed by ClearExpiredCache.
3. **Invalidation**: `InvalidateCache_RemovesCachedItem`, `ClearAllCache_RemovesAllItems` — validate targeted and full cache clearing.
4. **Cache-disabled mode**: `CacheDisabled_ReturnsNull` — when `EnableTemplateCache=false`, cache operations are skipped and get returns null.
5. **Edge cases**: `NullOrEmptyTemplatePath_ReturnsNull` — null/empty/whitespace paths return null for get and are silently ignored for cache.
6. **Overwrite**: `CacheValidationResult_OverwritesExistingValue` — re-caching same path updates the value.
7. **Lifecycle**: `Dispose_ThenAccess_ThrowsObjectDisposedException` — all public methods throw ObjectDisposedException after disposal.

Also updated `Tests/DocuFiller.Tests.csproj`:
- Added `<PackageReference Include="Microsoft.Extensions.Options" Version="10.0.1" />` for `IOptionsMonitor<T>` support.
- Added `<Compile Include>` links for `TemplateCacheService.cs` and `Configuration/AppSettings.cs`.

Had to add `using DocuFiller.Utils;` for the `ValidationResult` type (defined in `Utils/ValidationHelper.cs`). The plan's test case for "only removes expired items, keeps fresh ones" was revised to `ClearExpiredCache_RemovesExpiredItems` because the service evaluates expiration against `IOptionsMonitor.CurrentValue` at check-time rather than caching a snapshot — making it impossible to selectively expire without time injection.

## Verification

Ran `dotnet test --filter "FullyQualifiedName~TemplateCacheServiceTests" --verbosity normal` — all 11 tests passed in 0.8s. Also confirmed `dotnet build Tests/DocuFiller.Tests.csproj` succeeds with 0 errors.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet build Tests/DocuFiller.Tests.csproj` | 0 | ✅ pass | 1730ms |
| 2 | `dotnet test --filter "FullyQualifiedName~TemplateCacheServiceTests" --verbosity normal` | 0 | ✅ pass (11/11 tests) | 1710ms |

## Deviations

1. Added `using DocuFiller.Utils;` to the test file — plan did not mention this namespace, but ValidationResult is defined in `DocuFiller.Utils` (in ValidationHelper.cs).
2. Revised ClearExpiredCache test: the plan's "only removes expired, keeps fresh" scenario is infeasible without time injection since IsExpired reads CurrentValue dynamically. Simplified to verify expired items are removed.
3. Added an extra test (`CacheValidationResult_OverwritesExistingValue`) beyond the plan's 9 cases for total of 11.

## Known Issues

None.

## Files Created/Modified

- `Tests/DocuFiller.Tests/Services/TemplateCacheServiceTests.cs`
- `Tests/DocuFiller.Tests.csproj`
