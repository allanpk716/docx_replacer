---
estimated_steps: 24
estimated_files: 1
skills_used:
  - github-workflows
---

# T01: Create GitHub Actions release workflow YAML

Create .github/workflows/build-release.yml that triggers on v* tag push, builds DocuFiller on windows-latest with .NET 8, runs Velopack packaging, and creates a GitHub Release with all 4 artifact types.

## Steps

1. Create directory `.github/workflows/`
2. Create `build-release.yml` with:
   - **Trigger**: `on: push: tags: ['v*']` — only fires on version tags
   - **Permissions**: `contents: write` — needed for creating releases
   - **Runner**: `windows-latest`
   - **Checkout step**: `actions/checkout@v4` with `fetch-depth: 0` (needed for git describe)
   - **.NET setup**: `actions/setup-dotnet@v4` with `dotnet-version: '8.0.x'`
   - **Install vpk**: `dotnet tool install -g vpk`
   - **Extract version**: bash step reading `${GITHUB_REF#refs/tags/}`, strip leading 'v', set as output variable
   - **Publish**: `dotnet publish DocuFiller/DocuFiller.csproj -c Release -r win-x64 --self-contained -o publish -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true`
     - Note: if the checkout root contains the csproj directly (no DocuFiller/ subdirectory), adjust path to `DocuFiller.csproj`
   - **Velopack pack**: `vpk pack --packId DocuFiller --packVersion ${{ steps.version.outputs.version }} --packDir publish --mainExe DocuFiller.exe --outputDir artifacts`
     - Include `--packPortable true` flag to ensure Portable.zip is produced (required by R038)
   - **Create GitHub Release**: use `softprops/action-gh-release@v2` with:
     - `generate_release_notes: true` (auto-generates from commits)
     - `files` list covering all 4 artifact types: `artifacts/DocuFillerSetup.exe`, `artifacts/*Portable*.zip`, `artifacts/*.nupkg`, `artifacts/releases.win.json`
3. Verify YAML is syntactically valid

## Important constraints
- The csproj path depends on repo structure. Check if `DocuFiller.csproj` is at root or in a subdirectory. From the solution file, the project path is `DocuFiller.csproj` (at root relative to the solution).
- External/ directory is gitignored but no MSBuild target references it (verified). No special handling needed.
- Do NOT include upload to Go update server (that's the existing build-internal.bat flow for internal releases). This workflow only does GitHub Release.
- The workflow should produce the same Velopack artifacts as build-internal.bat but upload to GitHub instead of the Go server.

## Inputs

- `scripts/build-internal.bat`
- `DocuFiller.csproj`
- `DocuFiller.sln`

## Expected Output

- `.github/workflows/build-release.yml`

## Verification

powershell -Command "try { $null = [System.Management.Automation.PSParser]::Tokenize((Get-Content -Raw .github/workflows/build-release.yml), [ref]$null); Write-Host 'YAML syntax check: PASS' } catch { Write-Host 'YAML parse error:' $_.Exception.Message; exit 1 }" && grep -q "on:" .github/workflows/build-release.yml && grep -q "tags:" .github/workflows/build-release.yml && grep -q "vpk pack" .github/workflows/build-release.yml && grep -q "action-gh-release" .github/workflows/build-release.yml

## Observability Impact

Signals added: GitHub Actions run status and step logs for each build phase
How a future agent inspects: `gh run list --workflow=build-release.yml` or GitHub Actions tab
Failure state exposed: Failed run log with step-level error details
