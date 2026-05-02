---
estimated_steps: 18
estimated_files: 1
skills_used:
  - github-workflows
---

# T02: Validate workflow structure and verify artifact coverage

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

## Inputs

- `.github/workflows/build-release.yml`

## Expected Output

- `.github/workflows/build-release.yml`

## Verification

grep -q "'v*'" .github/workflows/build-release.yml && grep -q "Setup" .github/workflows/build-release.yml && grep -q "Portable" .github/workflows/build-release.yml && grep -q "nupkg" .github/workflows/build-release.yml && grep -q "releases.win.json" .github/workflows/build-release.yml && echo "All 4 artifact types verified in workflow"
