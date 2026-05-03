# S02: E2E 便携版更新测试

**Goal:** 创建 E2E 自动化测试脚本，一键验证便携版在本地 HTTP 和内网 Go 服务器两种环境下的完整更新链路（检查→下载→应用→版本升级），同时验证安装版无回归。
**Demo:** E2E 脚本一键验证便携版在本地 HTTP 和内网 Go 服务器上的完整更新链路

## Must-Haves

- ## Must-Haves
- 本地 HTTP 环境 E2E 脚本：构建 v1.0.0 + v1.1.0，启动 e2e-serve.py，解压 Portable.zip，运行 `DocuFiller.exe update --yes`，解析 JSONL 输出验证版本升级
- Go 服务器环境 E2E 脚本：编译 Go 服务器，上传 v1.1.0 到 stable 通道，运行便携版 DocuFiller update --yes，验证更新成功
- 两个脚本都有明确的 PASS/FAIL 输出，可用于回归验证
- 测试指南文档更新，涵盖便携版 E2E 测试步骤
- ## Threat Surface
- **Abuse**: N/A — 测试脚本为本地开发工具，不暴露到生产环境
- **Data exposure**: 测试脚本可能从 .env 读取内网服务器凭据（UPDATE_SERVER_HOST/PORT/TOKEN），但仅用于上传测试版本
- **Input trust**: 无用户输入，脚本参数固定
- ## Requirement Impact
- **Requirements touched**: R005-R007 (E2E 测试覆盖便携版更新)
- **Re-verify**: dotnet build + dotnet test 全部通过（脚本不改动生产代码）
- **Decisions revisited**: 无

## Proof Level

- This slice proves: integration — 脚本验证跨组件交互：Velopack 打包 → HTTP/Go 服务器 → CLI update --yes → JSONL 输出解析

Real runtime required: yes（需要 vpk + dotnet + python + curl）
Human/UAT required: no（全自动 CLI 模式）

## Integration Closure

- Upstream surfaces consumed:
  - S01 产出: IUpdateService.IsPortable, CLI update --yes 无阻断
  - e2e-serve.py (本地 HTTP 服务器)
  - update-server/ (Go 更新服务器源码)
  - scripts/build-internal.bat 的构建逻辑（vpk pack 命令）
- New wiring introduced: 无新生产代码，仅测试脚本
- What remains: 手动 UAT 验证 GUI 便携版更新弹窗流程（不在自动化范围内）

## Verification

- Not provided.

## Tasks

- [x] **T01: Create local HTTP E2E portable update test script** `est:2h`
  Create `scripts/e2e-portable-update-test.bat` that automates the full portable update verification against a local HTTP server (e2e-serve.py).

The script should:
1. Check prerequisites (vpk, python, dotnet)
2. Build v1.0.0 and v1.1.0 from source, pack with vpk
3. Extract v1.0.0 Portable.zip to a test directory
4. Start e2e-serve.py serving v1.1.0 artifacts
5. Configure the portable app's update URL by creating update-config.json in %USERPROFILE%\.docx_replacer\
6. Run `DocuFiller.exe update --yes` from the portable directory
7. Parse JSONL output to verify update check succeeded (found new version, download progress, apply)
8. After update completes, verify the version upgraded by checking the running executable or JSONL output
9. Restore original csproj version, clean up
10. Print PASS/FAIL summary

Key constraints:
- BAT script files must not contain Chinese characters
- Use e2e-serve.py from scripts/ directory
- Restore csproj version on both success and failure paths
- Handle cleanup of background HTTP server process
- The portable update applies via Velopack Update.exe — after `update --yes`, the process may exit/restart, so the script must handle this
- If ApplyUpdatesAndRestart causes the process to not return, use a timeout and check the result from the output captured so far
  - Files: `scripts/e2e-portable-update-test.bat`, `scripts/e2e-serve.py`
  - Verify: cmd /c scripts\e2e-portable-update-test.bat --dry-run 2>&1 || echo 'dry-run mode: validate script syntax only'
findstr /n /c:"[E2E-PORTABLE]" scripts/e2e-portable-update-test.bat | findstr /c:"PASS" /c:"FAIL"

- [x] **T02: Create Go server E2E portable update test script** `est:2h`
  Create `scripts/e2e-portable-go-update-test.sh` that automates the full portable update verification against the internal Go update server.

The script should:
1. Build Go server binary from update-server/ source
2. Start server on a high port with temp data dir and test token
3. Build v1.0.0 and v1.1.0 from C# source, pack with vpk
4. Upload v1.1.0 artifacts to Go server's stable channel via curl POST
5. Verify GET /stable/releases.win.json contains the version
6. Extract v1.0.0 Portable.zip
7. Create update-config.json pointing to the Go server
8. Run `DocuFiller.exe update --yes` from the portable directory
9. Parse JSONL output to verify update succeeded
10. Clean up (kill server, remove temp dirs, restore csproj version)
11. Print PASS/FAIL summary

Key constraints:
- Script must run in git-bash on Windows
- Use the upload API pattern from e2e-dual-channel-test.sh as reference
- Go server serves from /{channel}/ subdirectories
- The update-config.json must use the Go server's URL (http://localhost:{port}/)
- Handle the fact that ApplyUpdatesAndRestart may cause the process to not return gracefully
  - Files: `scripts/e2e-portable-go-update-test.sh`, `update-server/main.go`, `scripts/e2e-dual-channel-test.sh`
  - Verify: bash -n scripts/e2e-portable-go-update-test.sh && echo 'Syntax OK' || echo 'Syntax error'
grep -c 'PASS\|FAIL' scripts/e2e-portable-go-update-test.sh

- [x] **T03: Update E2E test guide and validate script syntax** `est:30m`
  Update `docs/plans/e2e-update-test-guide.md` to add portable-specific E2E test sections:
1. Add 'Portable Update via Local HTTP' section referencing e2e-portable-update-test.bat
2. Add 'Portable Update via Go Server' section referencing e2e-portable-go-update-test.sh
3. Add prerequisites section for portable testing (portable extraction, update-config.json setup)
4. Update troubleshooting table for portable-specific issues
5. Validate both scripts parse correctly (bash -n for .sh, cmd /c echo check for .bat)
6. Run dotnet build to confirm no production code was modified
  - Files: `docs/plans/e2e-update-test-guide.md`, `scripts/e2e-portable-update-test.bat`, `scripts/e2e-portable-go-update-test.sh`
  - Verify: bash -n scripts/e2e-portable-go-update-test.sh
grep -c 'Portable' docs/plans/e2e-update-test-guide.md
dotnet build --no-restore -v q 2>&1 | tail -5

## Files Likely Touched

- scripts/e2e-portable-update-test.bat
- scripts/e2e-serve.py
- scripts/e2e-portable-go-update-test.sh
- update-server/main.go
- scripts/e2e-dual-channel-test.sh
- docs/plans/e2e-update-test-guide.md
