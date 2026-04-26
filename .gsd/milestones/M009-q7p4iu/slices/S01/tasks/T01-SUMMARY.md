---
id: T01
parent: S01
milestone: M009-q7p4iu
key_files:
  - .github/workflows/build-release.yml
key_decisions:
  - Velopack Portable.zip is produced by default — no --packPortable flag needed, matching build-internal.bat behavior
duration: 
verification_result: passed
completed_at: 2026-04-26T10:52:39.059Z
blocker_discovered: false
---

# T01: Create GitHub Actions release workflow that builds DocuFiller on v* tag push and publishes Setup.exe, Portable.zip, .nupkg, and releases.win.json to GitHub Releases

**Create GitHub Actions release workflow that builds DocuFiller on v* tag push and publishes Setup.exe, Portable.zip, .nupkg, and releases.win.json to GitHub Releases**

## What Happened

Created `.github/workflows/build-release.yml` — a GitHub Actions workflow that triggers on `v*` tag pushes, builds DocuFiller on `windows-latest` with .NET 8, packages it with Velopack (`vpk pack`), and publishes all four artifact types (Setup.exe, Portable.zip, .nupkg, releases.win.json) to a GitHub Release via `softprops/action-gh-release@v2`.

Key implementation details:
- **Trigger**: `on: push: tags: ['v*']` — only fires on version tags, matching the existing build-internal.bat convention.
- **Permissions**: `contents: write` — needed for creating releases.
- **Version extraction**: Bash step reads `GITHUB_REF`, strips the `refs/tags/` prefix and leading `v`, outputs to `$GITHUB_OUTPUT` for downstream steps.
- **Publish**: `dotnet publish DocuFiller.csproj` with `PublishSingleFile=true` and `IncludeNativeLibrariesForSelfExtract=true`, matching build-internal.bat's CLEAN_BUILD function exactly.
- **Velopack pack**: Uses `vpk pack` with the same arguments as build-internal.bat (packId, packVersion, packDir, mainExe, outputDir). Portable.zip is produced by default (Velopack generates it unless `--noPortable` is specified — no `--packPortable` flag exists).
- **Release creation**: Uses `softprops/action-gh-release@v2` with `generate_release_notes: true` and file globs for all 4 artifact types.
- **Observability**: Step echo tags `[GET_VERSION]`, `[CLEAN_BUILD]`, `[VPK_PACK]` match the build-internal.bat convention for log grep-ability.

One deviation from plan: the task plan mentioned `--packPortable true` flag, but Velopack's `vpk pack` on Windows produces Portable.zip by default (the flag doesn't exist — only `--noPortable` to skip it). This was verified against the Velopack CLI reference docs.

## Verification

Structural validation of the workflow YAML file:
1. File exists and is readable (2212 bytes)
2. Node.js structural checks verified all 19 required patterns: trigger on v* tags, permissions, windows-latest runner, actions/checkout@v4 with fetch-depth: 0, setup-dotnet@v4 with .NET 8.0.x, vpk tool install, version extraction from GITHUB_REF, dotnet publish with PublishSingleFile, vpk pack with correct args, softprops/action-gh-release@v2 with generate_release_notes, and all 4 artifact file patterns (Setup.exe, Portable.zip, .nupkg, releases.win.json)
3. grep-based checks confirmed presence of `on:`, `tags:`, `vpk pack`, and `action-gh-release`

Note: Full end-to-end verification requires pushing a v* tag to GitHub and observing the Actions run — this is the runtime verification that can only happen after the workflow is merged.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `node structural-check (19 patterns)` | 0 | ✅ pass | 500ms |
| 2 | `grep 'on:' && grep 'tags:' && grep 'vpk pack' && grep 'action-gh-release' build-release.yml` | 0 | ✅ pass | 200ms |

## Deviations

Omitted `--packPortable true` flag from vpk pack command. The task plan specified this flag, but Velopack's vpk pack on Windows produces Portable.zip by default — the flag does not exist in the CLI (only `--noPortable` exists to skip it). Verified against Velopack docs (docs.velopack.io/reference/cli/content/vpk-windows). This matches the existing build-internal.bat which also does not use any portable flag.

## Known Issues

None.

## Files Created/Modified

- `.github/workflows/build-release.yml`
