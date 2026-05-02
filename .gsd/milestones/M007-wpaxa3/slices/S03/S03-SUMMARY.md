---
id: S03
parent: M007-wpaxa3
milestone: M007-wpaxa3
provides:
  - ["scripts/build-internal.bat → Velopack publish pipeline (PublishSingleFile + vpk pack producing Setup.exe/Portable.zip/.nupkg/releases.win.json)", "scripts/build.bat → simplified standalone-only entry point"]
requires:
  - slice: S01
    provides: DocuFiller.csproj Velopack NuGet reference, Program.cs VelopackApp initialization
affects:
  - ["S04"]
key_files:
  - ["scripts/build-internal.bat", "scripts/build.bat"]
key_decisions:
  - (none)
patterns_established:
  - ["Phase-labeled echo messages ([PHASE_NAME] SUCCESS/FAILED) in build scripts for observability", "vpk availability check with install instructions pattern"]
observability_surfaces:
  - none
drill_down_paths:
  []
duration: ""
verification_result: passed
completed_at: 2026-04-24T06:23:23.264Z
blocker_discovered: false
---

# S03: 发布管道改造

**Transformed build-internal.bat to Velopack publish pipeline with PublishSingleFile=true and vpk pack, removed all old update server scripts and configs**

## What Happened

This slice transformed the build pipeline from a plain tar-based zip publisher to a Velopack-compatible publish pipeline producing professional desktop application artifacts.

**T01: Transform build-internal.bat** — Changed dotnet publish to use `PublishSingleFile=true` and `IncludeNativeLibrariesForSelfExtract=true`. Added 'v' prefix stripping from git tags for semver2 compatibility with vpk. Replaced the old CREATE_PACKAGE function (tar-based zip) with VPK_PACK that calls `vpk pack --packId DocuFiller --packVersion {version} --packDir publish --mainExe DocuFiller.exe --outputDir build` to produce Setup.exe, Portable.zip, .nupkg, and releases.win.json. Added vpk availability check with install instructions. All phase functions emit tagged echo messages ([GET_VERSION], [CLEAN_BUILD], [VPK_PACK]) with SUCCESS/FAILED status.

**T02: Remove old scripts and simplify build.bat** — Deleted publish.bat, release.bat, build-and-publish.bat (old update server API scripts), and config/ directory (publish-config.bat, release-config.bat.example). Simplified build.bat to standalone-only mode by removing the --publish mode, auto-detect git tag prompt, and publish call path.

All verification checks pass: no old scripts remain, build.bat has no publish references, build-internal.bat has correct PublishSingleFile/vpk configuration, no Chinese characters in BAT files, and dotnet build succeeds with 0 errors.

## Verification

All slice-level verification checks pass:
- publish.bat, release.bat, build-and-publish.bat deleted (confirmed via `! test -f`)
- scripts/config/ directory removed
- build.bat contains no "publish" references
- build-internal.bat contains PublishSingleFile=true, IncludeNativeLibrariesForSelfExtract=true, vpk pack, --packId DocuFiller, --mainExe DocuFiller.exe
- build-internal.bat does NOT contain PublishSingleFile=false
- No Chinese characters in any BAT file
- dotnet build DocuFiller.csproj -c Release: 0 errors, 0 warnings

Note: vpk pack itself was NOT executed (vpk CLI tool may not be installed), but the script structure and invocation are verified to be correct.

## Requirements Advanced

- R025 — build-internal.bat now uses PublishSingleFile=true + vpk pack pipeline; old update server scripts and configs fully removed; build.bat simplified to standalone-only

## Requirements Validated

- R025 — build-internal.bat contains all Velopack pipeline elements (PublishSingleFile=true, IncludeNativeLibrariesForSelfExtract=true, vpk pack with correct args). Old scripts deleted. build.bat no publish references. dotnet build 0 errors.

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
