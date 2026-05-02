# S01: GitHub Actions CI/CD 发布流水线 — UAT

**Milestone:** M009-q7p4iu
**Written:** 2026-04-26T10:57:01.678Z

# S01: GitHub Actions CI/CD 发布流水线 — UAT

**Milestone:** M009-q7p4iu
**Written:** 2026-04-26

## UAT Type

- UAT mode: artifact-driven
- Why this mode is sufficient: This slice produces a CI/CD workflow file — its correctness is verified structurally (YAML validity, step presence, artifact coverage). Runtime verification requires pushing a v* tag to GitHub which is a deployment action, not a development-time test.

## Preconditions

- `.github/workflows/build-release.yml` exists in the repository
- The repository is hosted on GitHub with Actions enabled
- A `v*` tag can be pushed to trigger the workflow

## Smoke Test

1. Verify the workflow file exists and is non-empty (2212 bytes)
2. **Expected:** File exists at `.github/workflows/build-release.yml`

## Test Cases

### 1. Trigger Pattern Validation

1. Open `.github/workflows/build-release.yml`
2. Verify `on: push: tags: ['v*']` trigger exists
3. **Expected:** Workflow only triggers on version tags (v1.0.0, v2.3.1, etc.)

### 2. Build Configuration Validation

1. Check runner is `windows-latest`
2. Check .NET 8 setup step exists (`actions/setup-dotnet@v4` with `dotnet-version: '8.0.x'`)
3. Check vpk tool installation step exists
4. **Expected:** All build prerequisites are configured correctly

### 3. Version Extraction Validation

1. Check version extraction step uses `GITHUB_REF` and strips `refs/tags/` prefix and `v` prefix
2. Check version is output via `$GITHUB_OUTPUT`
3. **Expected:** Tag `v1.2.3` produces version `1.2.3` for downstream steps

### 4. Dotnet Publish Validation

1. Check `dotnet publish` uses correct flags: `-c Release -r win-x64 --self-contained`
2. Check `PublishSingleFile=true` and `IncludeNativeLibrariesForSelfExtract=true` are set
3. **Expected:** Publish configuration matches build-internal.bat CLEAN_BUILD function

### 5. Velopack Pack Validation

1. Check `vpk pack` uses correct arguments: `--packId DocuFiller --packVersion ${{ steps.version.outputs.version }} --packDir publish --mainExe DocuFiller.exe --outputDir artifacts`
2. Verify no `--noPortable` flag (Portable.zip should be produced by default)
3. **Expected:** All Velopack artifacts are generated in `artifacts/` directory

### 6. Release Upload Validation

1. Check `softprops/action-gh-release@v2` is used
2. Check `generate_release_notes: true` is set
3. Check files list includes all 4 artifact patterns:
   - `artifacts/DocuFillerSetup.exe`
   - `artifacts/*Portable*.zip`
   - `artifacts/*.nupkg`
   - `artifacts/releases.win.json`
4. **Expected:** GitHub Release is created with all 4 artifact types

### 7. Observability Tags Validation

1. Check `[GET_VERSION]` tag appears in version extraction step
2. Check `[CLEAN_BUILD]` tag appears in publish step
3. Check `[VPK_PACK]` tag appears in Velopack pack step
4. **Expected:** All 3 observability tags are present for log grep-ability

## Edge Cases

### Non-version Tag Push

1. Push a tag that does not match `v*` pattern (e.g., `release-1.0`)
2. **Expected:** Workflow does NOT trigger

### Permissions Check

1. Verify `permissions: contents: write` is set at top level
2. **Expected:** Workflow has permission to create GitHub Releases

## Failure Signals

- Workflow file missing or empty
- Missing artifact patterns in release step
- Missing version extraction (would cause vpk pack to fail)
- Missing .NET 8 setup (would cause build to fail)
- Missing vpk installation (would cause pack to fail)

## Not Proven By This UAT

- Actual GitHub Actions execution (requires tag push to GitHub)
- Velopack pack producing all 4 expected files (requires runtime execution)
- Release creation with correct file uploads (requires runtime execution)
- Download and installation of produced Setup.exe (requires human testing)

## Notes for Tester

- The workflow was structurally validated with 24 checks — all pass
- `dotnet build -c Release` succeeds confirming CI compatibility
- The real end-to-end test requires: `git tag v1.0.0 && git push origin v1.0.0`
- Watch for GitHub Actions run in repository's Actions tab
- Verify Release page shows all 4 artifact types after successful run
