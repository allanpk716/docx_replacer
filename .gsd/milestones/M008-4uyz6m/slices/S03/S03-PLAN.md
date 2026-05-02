# S03: 发布脚本改造

**Goal:** 改造 build-internal.bat 支持 channel 参数（stable/beta），在 Velopack 打包完成后自动调用 Go 更新服务器的上传 API，将构建产物（.nupkg + releases.win.json）推送到指定通道。服务器地址和 Token 从环境变量 UPDATE_SERVER_URL / UPDATE_SERVER_TOKEN 读取。
**Demo:** build-internal.bat standalone beta 完成编译+打包+自动上传到 Go 服务器的 beta 通道

## Must-Haves

- build-internal.bat standalone beta 完成编译+打包+自动上传到 Go 服务器的 beta 通道
- build-internal.bat standalone stable 完成编译+打包+自动上传到 Go 服务器的 stable 通道
- 缺少 UPDATE_SERVER_URL 或 UPDATE_SERVER_TOKEN 时给出清晰提示
- build-internal.bat standalone（无 channel）仍可执行构建但不自动上传（向后兼容）

## Proof Level

- This slice proves: This slice proves: operational — the build script is a runtime artifact, not a code contract. Real curl command must succeed against the Go server.\nReal runtime required: yes (curl + Go server)\nHuman/UAT required: no

## Integration Closure

- Upstream surfaces consumed: POST /api/channels/{channel}/releases (multipart upload from S01), Bearer token auth\n- New wiring introduced: build-internal.bat calls curl to upload to update-server after vpk pack\n- What remains before the milestone is truly usable end-to-end: S04 端到端验证 (full E2E flow)

## Verification

- Runtime signals: [UPLOAD] tagged echo messages for each upload attempt (phase start, file names, HTTP status, success/failure)\n- Inspection surfaces: Console output from build-internal.bat\n- Failure visibility: [UPLOAD] FAILED with HTTP status code and error description

## Tasks

- [x] **T01: Add channel parameter and auto-upload to build-internal.bat** `est:1h`
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
  - Files: `scripts/build-internal.bat`, `scripts/build.bat`
  - Verify: grep -c "UPLOAD" scripts/build-internal.bat (expect >= 3 upload-related lines). grep -c "CHANNEL" scripts/build-internal.bat (expect >= 3 channel-related lines). grep -q "UPDATE_SERVER_URL" scripts/build-internal.bat (env var check present). grep -q "UPDATE_SERVER_TOKEN" scripts/build-internal.bat (env var check present). build-internal.bat standalone (no channel) should still pass build phase without error.

## Files Likely Touched

- scripts/build-internal.bat
- scripts/build.bat
