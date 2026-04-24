---
estimated_steps: 37
estimated_files: 4
skills_used: []
---

# T01: Add Velopack NuGet + initialize VelopackApp + create IUpdateService interface

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

## Inputs

- `DocuFiller.csproj`
- `Program.cs`
- `appsettings.json`

## Expected Output

- `DocuFiller.csproj`
- `Program.cs`
- `appsettings.json`
- `Services/Interfaces/IUpdateService.cs`

## Verification

cd "C:/WorkSpace/agent/docx_replacer/.gsd/worktrees/M007-wpaxa3" && dotnet build --verbosity quiet 2>&1 | grep -E "error|Error" | head -5; echo "---"; grep -c "VelopackApp.Build" Program.cs; grep -c "UpdateUrl" appsettings.json; test -f Services/Interfaces/IUpdateService.cs && echo "IUpdateService.cs exists" || echo "IUpdateService.cs MISSING"
