---
estimated_steps: 36
estimated_files: 3
skills_used: []
---

# T02: Clean old update system residuals from config and scripts

Remove all old update system residuals from App.config, build-internal.bat, and sync-version.bat. Then run the full test suite to confirm zero regressions.

## Steps
1. **App.config**: Remove the 3 old update config entries:
   - `<add key="UpdateServerUrl" value="http://192.168.1.100:8080" />`
   - `<add key="UpdateChannel" value="stable" />`
   - `<add key="CheckUpdateOnStartup" value="true" />`
   Also remove the XML comment `<!-- 更新配置 -->` above them.
   Do NOT remove the other appSettings entries (log, file processing, performance, UI) — they are still used.

2. **build-internal.bat**: 
   - Remove the line `call :COPY_EXTERNAL_FILES` (in the main flow section)
   - Remove the entire `:COPY_EXTERNAL_FILES` function block (from `:COPY_EXTERNAL_FILES` to its `exit /b 0`)
   - Remove the line `call :PUBLISH_TO_SERVER` and the surrounding if block (in the main flow section)
   - Remove the entire `:PUBLISH_TO_SERVER` function block
   - Remove the entire `:GET_RELEASE_NOTES` function block (only used by PUBLISH_TO_SERVER)
   - Simplify the MODE validation: remove `publish` from the valid modes check since PUBLISH_TO_SERVER is gone. The script should now only support `standalone` mode.
   - Remove references to `CHANNEL` variable that were only used for publishing
   - Clean up any remaining references to `update-client`, `publish-client`, `External\`

3. **sync-version.bat**: Remove the entire block that syncs version to `update-client.config.yaml`:
   ```
   REM Update update-client.config.yaml (only the current_version field)
   if exist "%PROJECT_ROOT%\External\update-client.config.yaml" (
       powershell -Command "..."
   )
   ```

4. Run `dotnet build` to verify compilation
5. Run `dotnet test` to verify all tests pass
6. Run grep to confirm no old update system references remain:
   - `grep -r "UpdateServerUrl\|UpdateChannel\|CheckUpdateOnStartup\|COPY_EXTERNAL_FILES\|update-client\|publish-client" App.config scripts/ --include="*.config" --include="*.bat"`
   - Should return 0 matches

## Must-Haves
- [ ] App.config has 0 old update config entries
- [ ] build-internal.bat has no COPY_EXTERNAL_FILES, no PUBLISH_TO_SERVER, no update-client/publish-client references
- [ ] sync-version.bat has no update-client.config.yaml sync block
- [ ] dotnet build passes with 0 errors
- [ ] dotnet test passes with 0 failures
- [ ] grep confirms 0 old update system references in config and scripts

## Inputs

- `App.config`
- `scripts/build-internal.bat`
- `scripts/sync-version.bat`
- `DocuFiller.csproj`
- `Program.cs`

## Expected Output

- `App.config`
- `scripts/build-internal.bat`
- `scripts/sync-version.bat`

## Verification

cd "C:/WorkSpace/agent/docx_replacer/.gsd/worktrees/M007-wpaxa3" && dotnet build --verbosity quiet 2>&1 | grep -c "error" && dotnet test --no-build --verbosity minimal 2>&1 | tail -5 && grep -rc "UpdateServerUrl\|UpdateChannel\|CheckUpdateOnStartup\|COPY_EXTERNAL_FILES\|update-client\|publish-client" App.config scripts/ --include="*.config" --include="*.bat" 2>/dev/null || echo "0 old references found"
