---
id: T02
parent: S03
milestone: M007-wpaxa3
key_files:
  - scripts/build.bat
key_decisions:
  - (none)
duration: 
verification_result: passed
completed_at: 2026-04-24T06:21:50.208Z
blocker_discovered: false
---

# T02: Remove old update server scripts and simplify build.bat to standalone-only mode

**Remove old update server scripts and simplify build.bat to standalone-only mode**

## What Happened

Removed all old update server build scripts (publish.bat, release.bat, build-and-publish.bat) and old config files (config/publish-config.bat, config/release-config.bat.example) along with the config/ directory. Simplified build.bat by removing the --publish mode, auto-detect git tag prompt with choice dialog, and the publish call path. The script now only supports standalone mode — calling build-internal.bat directly with no mode parameter. Removed the AUTO_DETECT_MODE function entirely. Updated SHOW_HELP to reflect the simplified interface (no --publish option).

## Verification

Verified: (1) publish.bat, release.bat, build-and-publish.bat deleted from disk, (2) config/ directory removed entirely, (3) build.bat contains no "publish" references, (4) dotnet build DocuFiller.csproj -c Release succeeds with 0 errors and 0 warnings.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `bash -c "! test -f scripts/publish.bat && ! test -f scripts/release.bat && ! test -f scripts/build-and-publish.bat && ! test -d scripts/config"` | 0 | ✅ pass | 200ms |
| 2 | `bash -c "! grep -qi 'publish' scripts/build.bat"` | 0 | ✅ pass | 100ms |
| 3 | `dotnet build DocuFiller.csproj -c Release` | 0 | ✅ pass | 4930ms |

## Deviations

None.

## Known Issues

None.

## Files Created/Modified

- `scripts/build.bat`
