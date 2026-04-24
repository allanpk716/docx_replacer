# S03: 发布管道改造 — UAT

**Milestone:** M007-wpaxa3
**Written:** 2026-04-24T06:23:23.265Z

## UAT Type

- UAT mode: artifact-driven
- Why this mode is sufficient: This slice modifies build scripts, not runtime application code. Verification focuses on script content correctness and absence of old artifacts rather than live application behavior.

## Preconditions

- Windows environment with git available
- dotnet 8.0 SDK installed
- vpk CLI tool installed (for full pipeline test; `dotnet tool install -g vpk`)
- Repository at a valid git tag (e.g. `v1.0.0`) or with version override

## Smoke Test

1. Open `scripts/build.bat` and confirm it only calls `build-internal.bat` with no mode parameter and no references to --publish.
2. Open `scripts/build-internal.bat` and confirm it contains `PublishSingleFile=true`, `vpk pack`, and `--packId DocuFiller`.

## Test Cases

### 1. Old update server scripts fully removed

1. List files in `scripts/` directory
2. Confirm `publish.bat`, `release.bat`, `build-and-publish.bat` do NOT exist
3. Confirm `scripts/config/` directory does NOT exist
4. **Expected:** No old update server artifacts remain anywhere under `scripts/`

### 2. build.bat is standalone-only

1. Read `scripts/build.bat`
2. Confirm no string matching "publish" (case-insensitive) appears
3. Confirm no `--publish` option in SHOW_HELP or parameter parsing
4. **Expected:** build.bat only supports standalone build mode

### 3. build-internal.bat Velopack pipeline structure

1. Read `scripts/build-internal.bat`
2. Confirm `PublishSingleFile=true` is set in the dotnet publish command
3. Confirm `IncludeNativeLibrariesForSelfExtract=true` is set
4. Confirm `vpk pack --packId DocuFiller --mainExe DocuFiller.exe` is called in VPK_PACK function
5. Confirm version stripping logic handles 'v' prefix (e.g. `v1.2.3` → `1.2.3`)
6. Confirm vpk availability check with error message containing `dotnet tool install -g vpk`
7. **Expected:** All Velopack pipeline elements present and correctly configured

### 4. No Chinese characters in BAT files

1. Run: `python -c "import re; [print(f'{f}: CJK found') for f in ['scripts/build.bat','scripts/build-internal.bat'] if open(f,encoding='utf-8').read() and re.search(r'[\u4e00-\u9fff]', open(f,encoding='utf-8').read())]"`
2. **Expected:** No output (no Chinese characters found)

### 5. dotnet build succeeds

1. Run: `dotnet build DocuFiller.csproj -c Release`
2. **Expected:** Build completes with 0 errors, 0 warnings

## Edge Cases

### vpk not installed

1. Run `scripts/build-internal.bat` when vpk is NOT installed
2. **Expected:** Script detects missing vpk and prints install instructions: "dotnet tool install -g vpk"
3. Script should exit with non-zero error code after VPK_PACK failure

### No git tag available

1. Run `scripts/build-internal.bat` when not on a git tag
2. **Expected:** GET_VERSION phase fails with clear error message about missing tag/version

## Failure Signals

- `scripts/publish.bat` or `scripts/release.bat` still existing
- `scripts/build.bat` containing "publish" references
- `scripts/build-internal.bat` missing PublishSingleFile=true or vpk pack
- dotnet build failing with errors
- Chinese characters in any BAT file

## Not Proven By This UAT

- Actual vpk pack execution (requires vpk CLI and git tag)
- Produced artifacts (Setup.exe, Portable.zip, .nupkg, releases.win.json) — verified by S04 end-to-end test
- dotnet test regression (verified by R027 separately)

## Notes for Tester

- The build scripts are Windows BAT files; verification via bash requires `bash -c` wrapper on Windows
- vpk CLI is not a hard requirement for this slice — the script checks for it gracefully
- The config/ directory removal is intentional — old publish/release configs are no longer needed
