# S01: Velopack 集成 + 旧系统清理

**Goal:** dotnet build 编译通过，VelopackApp 在 Program.cs 中正确初始化，旧更新残留配置和脚本引用已清理，所有现有测试通过。为 S02 产出 IUpdateService 接口边界契约和 appsettings.json Update 配置节点。
**Demo:** dotnet build 编译通过，VelopackApp 在 Program.cs 中正确初始化，旧更新残留配置和脚本引用已清理，所有现有测试通过

## Must-Haves

- [x] dotnet build 编译无错误（0 errors）
- [x] Program.cs 中 VelopackApp.Build().Run() 位于 Main() 第一行
- [x] IUpdateService.cs 包含 CheckForUpdatesAsync / DownloadUpdatesAsync / ApplyUpdatesAndRestart 方法签名
- [x] appsettings.json 包含 Update:UpdateUrl 配置节点
- [x] App.config 中 UpdateServerUrl / UpdateChannel / CheckUpdateOnStartup 已移除
- [x] build-internal.bat 中无 COPY_EXTERNAL_FILES 和 update-client 引用
- [x] sync-version.bat 中无 update-client.config.yaml 引用
- [x] dotnet test 全部通过（162 tests, 0 failures）

## Proof Level

- This slice proves: contract — proves Velopack NuGet resolves, VelopackApp.Build().Run() compiles, IUpdateService interface exists with correct method signatures, old update residuals gone, all 162 tests green.

## Integration Closure

- New wiring introduced: VelopackApp.Build().Run() as first line of Program.Main(); IUpdateService interface at Services/Interfaces/IUpdateService.cs; Update:UpdateUrl config node in appsettings.json
- Upstream surfaces consumed: none (first slice)
- What remains: S02 implements IUpdateService with Velopack UpdateManager, adds UI; S03 rewrites build-internal.bat for vpk pack pipeline

## Verification

- None — this slice only adds library references, an interface, and cleans config/scripts. No runtime behavior changes until S02 wires UpdateService into the UI.

## Tasks

- [x] **T01: Add Velopack NuGet + initialize VelopackApp + create IUpdateService interface** `est:45m`
  Add the Velopack NuGet package to DocuFiller.csproj, bootstrap VelopackApp.Build().Run() at the very top of Program.Main(), add Update:UpdateUrl config section to appsettings.json, and create the IUpdateService interface at Services/Interfaces/IUpdateService.cs as the boundary contract for S02.

## Steps
1. Add `<PackageReference Include="Velopack" Version="0.0.1298" />` to DocuFiller.csproj in the existing ItemGroup with other PackageReferences
2. Run `dotnet restore` to resolve the new package
3. In `Program.cs`, add `using Velopack;` at the top, then add `VelopackApp.Build().Run();` as the **very first line** inside the `Main()` method, before the `if (args.Length > 0)` check. This must be first because Velopack may exit/restart the process during install/update hooks.
4. In `appsettings.json`, add a new top-level `"Update"` section with `"UpdateUrl": ""` (empty default — S02 will read this and treat empty as "update not configured"). Place it after the existing "UI" section.
5. Create `Services/Interfaces/IUpdateService.cs` with the following interface:
```csharp
using System;
using System.Threading.Tasks;
using Velopack;

namespace DocuFiller.Services.Interfaces
{
    /// <summary>
    /// 更新服务接口，封装 Velopack UpdateManager
    /// </summary>
    public interface IUpdateService
    {
        /// <summary>检查更新源是否有新版本</summary>
        Task<UpdateInfo?> CheckForUpdatesAsync();

        /// <summary>下载更新包</summary>
        Task DownloadUpdatesAsync(UpdateInfo updateInfo, Action<int>? progressCallback = null);

        /// <summary>应用已下载的更新并重启应用</summary>
        void ApplyUpdatesAndRestart();

        /// <summary>更新源 URL 是否已配置</summary>
        bool IsUpdateUrlConfigured { get; }
    }
}
```
Note: If Velopack's `UpdateInfo` type is in a different namespace or the API differs slightly, adjust the imports accordingly. Check the actual Velopack NuGet package API after restore.
6. Run `dotnet build` to verify compilation

## Must-Haves
- [ ] Velopack NuGet package added to csproj
- [ ] VelopackApp.Build().Run() is first line of Program.Main()
- [ ] Update:UpdateUrl config node exists in appsettings.json
- [ ] IUpdateService.cs exists with CheckForUpdatesAsync, DownloadUpdatesAsync, ApplyUpdatesAndRestart, IsUpdateUrlConfigured
- [ ] dotnet build passes with 0 errors
  - Files: `DocuFiller.csproj`, `Program.cs`, `appsettings.json`, `Services/Interfaces/IUpdateService.cs`
  - Verify: cd "C:/WorkSpace/agent/docx_replacer/.gsd/worktrees/M007-wpaxa3" && dotnet build --verbosity quiet 2>&1 | grep -E "error|Error" | head -5; echo "---"; grep -c "VelopackApp.Build" Program.cs; grep -c "UpdateUrl" appsettings.json; test -f Services/Interfaces/IUpdateService.cs && echo "IUpdateService.cs exists" || echo "IUpdateService.cs MISSING"

- [x] **T02: Clean old update system residuals from config and scripts** `est:30m`
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
  - Files: `App.config`, `scripts/build-internal.bat`, `scripts/sync-version.bat`
  - Verify: cd "C:/WorkSpace/agent/docx_replacer/.gsd/worktrees/M007-wpaxa3" && dotnet build --verbosity quiet 2>&1 | grep -c "error" && dotnet test --no-build --verbosity minimal 2>&1 | tail -5 && grep -rc "UpdateServerUrl\|UpdateChannel\|CheckUpdateOnStartup\|COPY_EXTERNAL_FILES\|update-client\|publish-client" App.config scripts/ --include="*.config" --include="*.bat" 2>/dev/null || echo "0 old references found"

## Files Likely Touched

- DocuFiller.csproj
- Program.cs
- appsettings.json
- Services/Interfaces/IUpdateService.cs
- App.config
- scripts/build-internal.bat
- scripts/sync-version.bat
