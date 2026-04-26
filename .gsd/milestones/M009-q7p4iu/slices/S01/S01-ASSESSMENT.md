---
sliceId: S01
uatType: artifact-driven
verdict: PASS
date: 2026-04-26T18:57:04.000Z
---

# UAT Result — S01

## Checks

| Check | Mode | Result | Notes |
|-------|------|--------|-------|
| Smoke: Workflow file exists and is non-empty | artifact | PASS | File exists at `.github/workflows/build-release.yml`, 2796 bytes |
| TC1: Trigger pattern `on: push: tags: ['v*']` | artifact | PASS | `grep` confirms single `v*` tag pattern in `on.push.tags` |
| TC2a: Runner is `windows-latest` | artifact | PASS | Found in `jobs.build-and-release.runs-on` |
| TC2b: .NET 8 setup via `actions/setup-dotnet@v4` | artifact | PASS | Step present with `dotnet-version: '8.0.x'` |
| TC2c: dotnet-version is 8.0.x | artifact | PASS | Confirmed in setup-dotnet step |
| TC2d: vpk tool installation | artifact | PASS | `dotnet tool install -g vpk` step present |
| TC3a: Version extraction strips `refs/tags/` | artifact | PASS | `${GITHUB_REF#refs/tags/}` found in bash step |
| TC3b: Version extraction strips `v` prefix | artifact | PASS | `${TAG#v}` found in bash step |
| TC3c: Version output via `$GITHUB_OUTPUT` | artifact | PASS | `echo "version=$VERSION" >> "$GITHUB_OUTPUT"` confirmed |
| TC4a: `dotnet publish -c Release` | artifact | PASS | Flag present in publish step |
| TC4b: `-r win-x64` | artifact | PASS | Flag present in publish step |
| TC4c: `--self-contained` | artifact | PASS | Flag present in publish step |
| TC4d: `PublishSingleFile=true` | artifact | PASS | Property set in publish step |
| TC4e: `IncludeNativeLibrariesForSelfExtract=true` | artifact | PASS | Property set in publish step |
| TC5a: `--packId DocuFiller` | artifact | PASS | Argument present in vpk pack step |
| TC5b: `--packVersion` from step output | artifact | PASS | Uses `${{ steps.version.outputs.version }}` |
| TC5c: `--packDir publish` | artifact | PASS | Argument present |
| TC5d: `--mainExe DocuFiller.exe` | artifact | PASS | Argument present |
| TC5e: `--outputDir artifacts` | artifact | PASS | Argument present |
| TC5f: No `--noPortable` flag (Portable.zip produced by default) | artifact | PASS | `grep -c "--noPortable"` returns 0 — flag correctly omitted |
| TC6a: `softprops/action-gh-release@v2` | artifact | PASS | Release step uses correct action |
| TC6b: `generate_release_notes: true` | artifact | PASS | Flag set in release step |
| TC6c: `artifacts/DocuFillerSetup.exe` in files list | artifact | PASS | Pattern present in release files |
| TC6d: `artifacts/*Portable*.zip` in files list | artifact | PASS | Pattern present in release files |
| TC6e: `artifacts/*.nupkg` in files list | artifact | PASS | Pattern present in release files |
| TC6f: `artifacts/releases.win.json` in files list | artifact | PASS | Pattern present in release files |
| TC7a: `[GET_VERSION]` observability tag | artifact | PASS | 2 occurrences (echo + tag output) |
| TC7b: `[CLEAN_BUILD]` observability tag | artifact | PASS | 2 occurrences (echo + success) |
| TC7c: `[VPK_PACK]` observability tag | artifact | PASS | 3 occurrences (echo + success + artifact list) |
| Edge: Non-version tag push does not trigger | artifact | PASS | Workflow trigger only matches `v*` pattern — no other triggers defined |
| Edge: `permissions: contents: write` | artifact | PASS | Top-level permissions block present |

## Overall Verdict

PASS — All 24 test cases, 2 edge cases, and the smoke test pass. The workflow file is structurally correct with all required triggers, build steps, artifact patterns, and observability tags in place.

## Notes

- Full end-to-end verification requires pushing a `v*` tag to GitHub and confirming the Actions run produces all 4 artifacts on the Release page.
- The "Non-version Tag Push" edge case is confirmed by structural analysis: the only trigger is `on: push: tags: ['v*']`, so non-matching tags will not trigger the workflow.
- Runtime execution of `vpk pack` to confirm it produces exactly Setup.exe, Portable.zip, .nupkg, and releases.win.json is deferred to the actual tag push (as noted in UAT "Not Proven" section).
