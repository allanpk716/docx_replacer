# S01: GitHub Actions CI/CD 发布流水线

**Goal:** Create a GitHub Actions workflow that automatically builds and publishes DocuFiller releases when a v* tag is pushed. The workflow produces Setup.exe, Portable.zip, .nupkg, and releases.win.json on the GitHub Release page.
**Demo:** After this: 打 v1.0.0 tag 推送到 GitHub，Actions 自动构建，Release 页面出现 Setup.exe + Portable.zip + .nupkg + releases.win.json

## Must-Haves

- Workflow file .github/workflows/build-release.yml exists and triggers on v* tag push
- Workflow runs on windows-latest with .NET 8 SDK
- Version is extracted from tag (v1.2.3 -> 1.2.3)
- dotnet publish produces self-contained single-file executable
- vpk pack produces Setup.exe, Portable.zip, .nupkg, and releases.win.json
- GitHub Release is created with all 4 artifact types uploaded
- YAML is valid and all required workflow elements are present

## Proof Level

- This slice proves: contract — workflow file defines the CI/CD contract; real runtime verified when a tag is pushed

## Integration Closure

- Upstream surfaces consumed: existing build-internal.bat logic (dotnet publish + vpk pack flags)
- New wiring introduced: .github/workflows/build-release.yml — the CI entrypoint
- What remains before the milestone is truly usable end-to-end: S02 (UpdateService multi-source) needs releases.win.json to be available at GitHub Releases for GitHubSource to work

## Verification

- Runtime signals: GitHub Actions run log with [GET_VERSION], [CLEAN_BUILD], [VPK_PACK] tagged phases (matching build-internal.bat convention)
- Inspection surfaces: GitHub Actions run history + Release page artifacts
- Failure visibility: GitHub Actions run status (green/red), step-level logs, release existence check
- Redaction constraints: No secrets in workflow — uses GITHUB_TOKEN auto-provisioned by Actions

## Tasks

- [x] **T01: Create GitHub Actions release workflow YAML** `est:1h`
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
  - Files: `.github/workflows/build-release.yml`
  - Verify: powershell -Command "try { $null = [System.Management.Automation.PSParser]::Tokenize((Get-Content -Raw .github/workflows/build-release.yml), [ref]$null); Write-Host 'YAML syntax check: PASS' } catch { Write-Host 'YAML parse error:' $_.Exception.Message; exit 1 }" && grep -q "on:" .github/workflows/build-release.yml && grep -q "tags:" .github/workflows/build-release.yml && grep -q "vpk pack" .github/workflows/build-release.yml && grep -q "action-gh-release" .github/workflows/build-release.yml

- [x] **T02: Validate workflow structure and verify artifact coverage** `est:20m`
  Verify the workflow file has correct trigger pattern, all required build steps, and uploads all 4 artifact types (Setup.exe, Portable.zip, .nupkg, releases.win.json) to the GitHub Release.

## Steps

1. Validate trigger pattern: workflow must trigger on `v*` tags only
2. Verify .NET 8 setup step exists
3. Verify vpk tool installation step exists
4. Verify version extraction from tag (strip 'v' prefix)
5. Verify dotnet publish step uses correct flags (self-contained, single-file, win-x64)
6. Verify vpk pack step includes `--packPortable` for Portable.zip generation
7. Verify GitHub Release step uploads all 4 artifact types:
   - Setup.exe (installer)
   - Portable.zip (portable archive)
   - .nupkg (Velopack package)
   - releases.win.json (Velopack update feed)
8. Run `dotnet build` to confirm the project builds without External/ directory (the csproj has no External/ target — this confirms CI compatibility)

## Important constraints
- This is a structural validation — we cannot run GitHub Actions locally
- The real end-to-end test happens when a v* tag is pushed to GitHub
- If `dotnet build` fails due to External/ or other local-only dependencies, document the issue and add a workaround step to the workflow in T01
  - Files: `.github/workflows/build-release.yml`
  - Verify: grep -q "'v*'" .github/workflows/build-release.yml && grep -q "Setup" .github/workflows/build-release.yml && grep -q "Portable" .github/workflows/build-release.yml && grep -q "nupkg" .github/workflows/build-release.yml && grep -q "releases.win.json" .github/workflows/build-release.yml && echo "All 4 artifact types verified in workflow"

## Files Likely Touched

- .github/workflows/build-release.yml
