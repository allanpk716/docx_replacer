---
id: T01
parent: S03
milestone: M007-wpaxa3
key_files:
  - scripts/build-internal.bat
key_decisions:
  - (none)
duration: 
verification_result: passed
completed_at: 2026-04-24T06:19:29.168Z
blocker_discovered: false
---

# T01: Transform build-internal.bat from tar-based zip to Velopack publish pipeline with PublishSingleFile=true and vpk pack

**Transform build-internal.bat from tar-based zip to Velopack publish pipeline with PublishSingleFile=true and vpk pack**

## What Happened

Transformed `scripts/build-internal.bat` from a plain tar-based zip packaging workflow to a Velopack-compatible publish pipeline. Key changes:

1. **GET_VERSION**: Added 'v' prefix stripping from git tags (e.g. `v1.2.3` -> `1.2.3`) for semver2 compatibility with vpk. Added phase-labeled echo messages.

2. **CLEAN_BUILD**: Changed `PublishSingleFile=false` to `PublishSingleFile=true` and added `IncludeNativeLibrariesForSelfExtract=true` for proper single-file self-extracting executables. Removed `PublishReadyToRun=false`. Changed output from `build\temp` to `build\publish`.

3. **VPK_PACK** (replaced CREATE_PACKAGE): New function that checks vpk availability (with install instructions: `dotnet tool install -g vpk`), runs `vpk pack --packId DocuFiller --packVersion {version} --packDir publish --mainExe DocuFiller.exe --outputDir build` to produce Setup.exe, Portable.zip, .nupkg, and releases.win.json. Cleans up intermediate publish directory after packaging. Lists produced artifacts on success.

4. **Removed**: Old tar-based zip creation logic, PACKAGE_PATH variable, references to `build\temp`.

All phase functions echo tagged messages (e.g. `[GET_VERSION]`, `[CLEAN_BUILD]`, `[VPK_PACK]`) with SUCCESS/FAILED status for observability.

## Verification

All 7 verification checks from the task plan pass:
1. PublishSingleFile=true present
2. IncludeNativeLibrariesForSelfExtract=true present
3. vpk pack command present
4. --packId DocuFiller present
5. --mainExe DocuFiller.exe present
6. PublishSingleFile=false not present
7. No Chinese characters in script

Slice-level verification also confirmed:
- Phase labels ([GET_VERSION], [CLEAN_BUILD], [VPK_PACK]) present with success/failure messages
- vpk not-found error provides install instructions: "dotnet tool install -g vpk"

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `grep -q "PublishSingleFile=true" scripts/build-internal.bat` | 0 | ✅ pass | 100ms |
| 2 | `grep -q "IncludeNativeLibrariesForSelfExtract=true" scripts/build-internal.bat` | 0 | ✅ pass | 100ms |
| 3 | `grep -q "vpk pack" scripts/build-internal.bat` | 0 | ✅ pass | 100ms |
| 4 | `grep -q "--packId DocuFiller" scripts/build-internal.bat` | 0 | ✅ pass | 100ms |
| 5 | `grep -q "--mainExe DocuFiller.exe" scripts/build-internal.bat` | 0 | ✅ pass | 100ms |
| 6 | `! grep -q "PublishSingleFile=false" scripts/build-internal.bat` | 0 | ✅ pass | 100ms |
| 7 | `python CJK character detection on scripts/build-internal.bat` | 0 | ✅ pass | 500ms |

## Deviations

None.

## Known Issues

None.

## Files Created/Modified

- `scripts/build-internal.bat`
