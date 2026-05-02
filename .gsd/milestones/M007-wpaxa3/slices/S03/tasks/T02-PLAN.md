---
estimated_steps: 1
estimated_files: 6
skills_used: []
---

# T02: Remove old update server scripts and simplify build.bat entry point

Delete publish.bat, release.bat, build-and-publish.bat (all reference the old update server API and update-publisher.exe). Delete config/publish-config.bat and config/release-config.bat.example (old server configs). Simplify build.bat to remove --publish mode, auto-detect git tag prompt, and the publish call path — make standalone the only mode. Verify dotnet build and dotnet test still pass after cleanup.

## Inputs

- `scripts/build.bat`
- `scripts/publish.bat`
- `scripts/release.bat`
- `scripts/build-and-publish.bat`
- `scripts/config/publish-config.bat`
- `scripts/config/release-config.bat.example`

## Expected Output

- `scripts/build.bat`

## Verification

cd C:/WorkSpace/agent/docx_replacer/.gsd/worktrees/M007-wpaxa3 && ! test -f scripts/publish.bat && ! test -f scripts/release.bat && ! test -f scripts/build-and-publish.bat && ! grep -q "publish" scripts/build.bat && dotnet build DocuFiller.csproj -c Release 2>&1 | tail -5
