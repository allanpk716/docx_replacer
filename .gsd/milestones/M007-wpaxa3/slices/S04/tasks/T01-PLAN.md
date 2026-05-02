---
estimated_steps: 13
estimated_files: 2
skills_used: []
---

# T01: Create E2E update test automation script and HTTP update server

Create `scripts/e2e-update-test.bat` that orchestrates the full end-to-end update verification flow: builds two versions of DocuFiller (old=1.0.0 and new=1.1.0), packages both with vpk, starts a local Python HTTP server serving the new version's Velopack feed, and prints step-by-step instructions for the human tester to manually verify install→update→config-preservation.

Also create `scripts/e2e-serve.py` — a minimal Python HTTP server that serves Velopack releases (releases.win.json + .nupkg files) from a specified directory, logging each request to stdout.

The BAT script must:
1. Check prerequisites (vpk, python) and fail with clear install instructions if missing
2. Build version 1.0.0 by temporarily setting VERSION=1.0.0 (skip git tag requirement)
3. Run vpk pack for 1.0.0, save artifacts to e2e-test/v1.0.0/
4. Build version 1.1.0 with VERSION=1.1.0
5. Run vpk pack for 1.1.0, save artifacts to e2e-test/v1.1.0/
6. Copy v1.0.0 Setup.exe to e2e-test/ as the installer
7. Start e2e-serve.py serving e2e-test/v1.1.0/ on port 8080
8. Print the UpdateUrl (http://localhost:8080/) and manual test instructions
9. Wait for tester to press any key, then clean up

No Chinese characters in BAT files. Use echo tags like [E2E] for observability.

## Inputs

- `scripts/build-internal.bat`
- `appsettings.json`
- `DocuFiller.csproj`

## Expected Output

- `scripts/e2e-update-test.bat`
- `scripts/e2e-serve.py`

## Verification

bash -c 'test -f scripts/e2e-update-test.bat && test -f scripts/e2e-serve.py && python scripts/e2e-serve.py --help'

## Observability Impact

Python HTTP server logs each GET request to stdout so the tester can observe Velopack update check and download traffic in real time
