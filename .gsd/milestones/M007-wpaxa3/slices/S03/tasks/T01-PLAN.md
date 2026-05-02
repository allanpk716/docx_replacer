---
estimated_steps: 1
estimated_files: 1
skills_used: []
---

# T01: Transform build-internal.bat to Velopack publish pipeline

Modify build-internal.bat to use PublishSingleFile=true and vpk pack instead of the old tar-based zip workflow. Strip 'v' prefix from git tags for semver2 compatibility with vpk. Replace CREATE_PACKAGE with VPK_PACK function that calls vpk pack to produce Setup.exe, Portable.zip, .nupkg, and releases.win.json. Add vpk availability check with install instructions.

## Inputs

- `scripts/build-internal.bat`
- `DocuFiller.csproj`

## Expected Output

- `scripts/build-internal.bat`

## Verification

cd C:/WorkSpace/agent/docx_replacer/.gsd/worktrees/M007-wpaxa3 && grep -q "PublishSingleFile=true" scripts/build-internal.bat && grep -q "IncludeNativeLibrariesForSelfExtract=true" scripts/build-internal.bat && grep -q "vpk pack" scripts/build-internal.bat && grep -q "--packId DocuFiller" scripts/build-internal.bat && grep -q "--mainExe DocuFiller.exe" scripts/build-internal.bat && ! grep -q "PublishSingleFile=false" scripts/build-internal.bat && ! grep -qP "[\x{4e00}-\x{9fff}]" scripts/build-internal.bat
