---
estimated_steps: 17
estimated_files: 2
skills_used: []
---

# T01: Add channel parameter and auto-upload to build-internal.bat

Modify build-internal.bat to accept an optional second parameter CHANNEL (stable/beta). After the VPK_PACK step completes successfully, if CHANNEL is provided and both UPDATE_SERVER_URL and UPDATE_SERVER_TOKEN environment variables are set, automatically upload all .nupkg files and releases.win.json from the build output to the Go update server via curl multipart POST.

Key implementation details:
1. Parse %2 as CHANNEL parameter after the existing MODE (%1)
2. Validate CHANNEL is either 'stable' or 'beta' if provided; reject other values with error
3. Add a new :UPLOAD subroutine after VPK_PACK
4. The UPLOAD subroutine:
   - Check UPDATE_SERVER_URL and UPDATE_SERVER_TOKEN env vars are set
   - If env vars missing, print clear error listing required vars and exit /b 1
   - Use curl -s -o /dev/null -w "%{http_code}" to POST each .nupkg file and releases.win.json
   - API endpoint: {UPDATE_SERVER_URL}/api/channels/{CHANNEL}/releases
   - Auth header: Authorization: Bearer {UPDATE_SERVER_TOKEN}
   - curl timeout: 60 seconds (--max-time 60)
   - Report success/failure with [UPLOAD] tagged echo messages (following MEM-076 pattern)
5. If CHANNEL is not provided, skip upload entirely (backward compatible, no error)
6. Update build.bat help text to document channel parameter
7. BAT script must NOT contain Chinese characters (project convention)
8. Update the main flow to call :UPLOAD after :VPK_PACK when CHANNEL is set

## Inputs

- `scripts/build-internal.bat`
- `scripts/build.bat`

## Expected Output

- `scripts/build-internal.bat`
- `scripts/build.bat`

## Verification

grep -c "UPLOAD" scripts/build-internal.bat (expect >= 3 upload-related lines). grep -c "CHANNEL" scripts/build-internal.bat (expect >= 3 channel-related lines). grep -q "UPDATE_SERVER_URL" scripts/build-internal.bat (env var check present). grep -q "UPDATE_SERVER_TOKEN" scripts/build-internal.bat (env var check present). build-internal.bat standalone (no channel) should still pass build phase without error.
