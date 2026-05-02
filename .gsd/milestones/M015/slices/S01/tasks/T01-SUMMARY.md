---
id: T01
parent: S01
milestone: M015
key_files:
  - Services/UpdateService.cs
  - Services/Interfaces/IUpdateService.cs
  - Tests/UpdateServiceTests.cs
key_decisions:
  - Used SimpleWebSource with CDN URL https://github.com/allanpk716/docx_replacer/releases/latest/download/ instead of GithubSource with GitHub API, eliminating rate limit entirely
duration: 
verification_result: passed
completed_at: 2026-05-02T16:49:16.229Z
blocker_discovered: false
---

# T01: Replace GithubSource with SimpleWebSource using GitHub CDN direct URL to eliminate API rate limit

**Replace GithubSource with SimpleWebSource using GitHub CDN direct URL to eliminate API rate limit**

## What Happened

Replaced both GithubSource instantiations in UpdateService (constructor and ReloadSource) with SimpleWebSource using the GitHub CDN URL `https://github.com/allanpk716/docx_replacer/releases/latest/download/`. This eliminates the GitHub API rate limit (60 req/hr for anonymous users) by using direct CDN access to release assets. Updated EffectiveUpdateUrl to return the CDN URL instead of empty string, updated the IUpdateService doc comment, and simplified all logging (no more ternary `_updateUrl != "" ? ... : "GitHub Releases"` pattern). Updated three test assertions that expected empty-string EffectiveUpdateUrl in GitHub mode to assert the CDN URL instead. All 29 UpdateServiceTests pass.

## Verification

1. `dotnet build` — 0 errors, build succeeded
2. `dotnet test --filter "FullyQualifiedName~UpdateServiceTests"` — 29/29 tests passed
3. `grep -r "GithubSource" --include="*.cs" .` — no matches (exit code 1), confirming complete removal

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet build` | 0 | ✅ pass | 8160ms |
| 2 | `dotnet test --filter "FullyQualifiedName~UpdateServiceTests"` | 0 | ✅ pass | 175000ms |
| 3 | `grep -r "GithubSource" --include="*.cs" .` | 1 | ✅ pass (no matches = success) | 200ms |

## Deviations

None.

## Known Issues

None.

## Files Created/Modified

- `Services/UpdateService.cs`
- `Services/Interfaces/IUpdateService.cs`
- `Tests/UpdateServiceTests.cs`
