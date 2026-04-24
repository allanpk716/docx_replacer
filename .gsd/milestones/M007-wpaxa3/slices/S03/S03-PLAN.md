# S03: 发布管道改造

**Goal:** Transform build-internal.bat from a plain zip publisher to a Velopack-compatible publish pipeline: dotnet publish with PublishSingleFile=true, then vpk pack to produce Setup.exe + Portable.zip + .nupkg + releases.win.json. Remove all old update server scripts and configs (publish.bat, release.bat, build-and-publish.bat, config/) and simplify build.bat entry point.
**Demo:** build-internal.bat 产出 Velopack 格式完整发布物：Setup.exe + Portable.zip + .nupkg + releases.win.json

## Must-Haves

- build-internal.bat runs dotnet publish with PublishSingleFile=true and IncludeNativeLibrariesForSelfExtract=true
- build-internal.bat calls vpk pack to produce DocuFiller-Setup.exe, DocuFiller-Portable.zip, DocuFiller-{version}-full.nupkg, releases.win.json
- Old publish/release scripts (publish.bat, release.bat, build-and-publish.bat) and their configs removed
- build.bat simplified to standalone-only mode
- dotnet build succeeds, dotnet test passes (162 tests)
- No Chinese characters in any BAT file
- No references to old update server (UPDATE_SERVER_URL, update-publisher.exe, update-client) remain in scripts/

## Proof Level

- This slice proves: integration — the publish pipeline is a build-time integration surface; verification exercises the actual dotnet publish command and validates script structure, but vpk pack requires the vpk tool to be installed

## Integration Closure

Upstream surfaces consumed:
- DocuFiller.csproj Velopack NuGet reference (from S01)
- Program.cs VelopackApp initialization (from S01)
New wiring introduced:
- build-internal.bat VPK_PACK function calling vpk pack CLI
What remains before the milestone is truly usable end-to-end:
- S04 must verify the produced Setup.exe/Portable.zip install and run correctly on a clean Windows machine

## Verification

- build-internal.bat echoes each phase (GET_VERSION, CLEAN_BUILD, VPK_PACK) with clear success/failure messages
- vpk pack output (artifact list) echoed on success
- vpk not-found error provides install instructions: "dotnet tool install -g vpk"

## Tasks

- [x] **T01: Transform build-internal.bat to Velopack publish pipeline** `est:45m`
  Modify build-internal.bat to use PublishSingleFile=true and vpk pack instead of the old tar-based zip workflow. Strip 'v' prefix from git tags for semver2 compatibility with vpk. Replace CREATE_PACKAGE with VPK_PACK function that calls vpk pack to produce Setup.exe, Portable.zip, .nupkg, and releases.win.json. Add vpk availability check with install instructions.
  - Files: `scripts/build-internal.bat`
  - Verify: cd C:/WorkSpace/agent/docx_replacer/.gsd/worktrees/M007-wpaxa3 && grep -q "PublishSingleFile=true" scripts/build-internal.bat && grep -q "IncludeNativeLibrariesForSelfExtract=true" scripts/build-internal.bat && grep -q "vpk pack" scripts/build-internal.bat && grep -q "--packId DocuFiller" scripts/build-internal.bat && grep -q "--mainExe DocuFiller.exe" scripts/build-internal.bat && ! grep -q "PublishSingleFile=false" scripts/build-internal.bat && ! grep -qP "[\x{4e00}-\x{9fff}]" scripts/build-internal.bat

- [x] **T02: Remove old update server scripts and simplify build.bat entry point** `est:30m`
  Delete publish.bat, release.bat, build-and-publish.bat (all reference the old update server API and update-publisher.exe). Delete config/publish-config.bat and config/release-config.bat.example (old server configs). Simplify build.bat to remove --publish mode, auto-detect git tag prompt, and the publish call path — make standalone the only mode. Verify dotnet build and dotnet test still pass after cleanup.
  - Files: `scripts/build.bat`, `scripts/publish.bat`, `scripts/release.bat`, `scripts/build-and-publish.bat`, `scripts/config/publish-config.bat`, `scripts/config/release-config.bat.example`
  - Verify: cd C:/WorkSpace/agent/docx_replacer/.gsd/worktrees/M007-wpaxa3 && ! test -f scripts/publish.bat && ! test -f scripts/release.bat && ! test -f scripts/build-and-publish.bat && ! grep -q "publish" scripts/build.bat && dotnet build DocuFiller.csproj -c Release 2>&1 | tail -5

## Files Likely Touched

- scripts/build-internal.bat
- scripts/build.bat
- scripts/publish.bat
- scripts/release.bat
- scripts/build-and-publish.bat
- scripts/config/publish-config.bat
- scripts/config/release-config.bat.example
