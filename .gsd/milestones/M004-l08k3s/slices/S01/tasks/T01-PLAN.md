---
estimated_steps: 37
estimated_files: 17
skills_used: []
---

# T01: Remove csproj build gates and External directory, delete all update service files, clean App.xaml.cs DI

Remove the PreBuild validation gate that blocks building without External/update-client.exe, delete the External/ directory, delete all update-related service/model/viewmodel/view files, and clean update DI registrations from App.xaml.cs.

## Steps

1. In `DocuFiller.csproj`, remove the entire `<Target Name="ValidateUpdateClientFiles">` block (PreBuild validation)
2. In `DocuFiller.csproj`, remove the entire `<Target Name="ValidateReleaseFiles">` block (PostPublish validation)
3. In `DocuFiller.csproj`, remove the `<!-- Update Client External Files -->` ItemGroup containing update-client.exe and update-client.config.yaml references
4. Delete the `External/` directory and all its contents
5. Delete all update service files:
   - `Services/Update/IUpdateService.cs` (includes UpdateAvailableEventArgs class)
   - `Services/Update/UpdateClientService.cs`
   - `Services/Update/UpdateService.cs`
   - `DocuFiller/Services/Update/UpdateDownloader.cs`
6. Delete all update model files:
   - `Models/Update/DaemonProgressInfo.cs`
   - `Models/Update/DownloadProgress.cs`
   - `Models/Update/DownloadStatus.cs`
   - `Models/Update/UpdateClientResponseModels.cs`
   - `Models/Update/UpdateConfig.cs`
   - `Models/Update/VersionInfo.cs`
7. Delete all update viewmodel files:
   - `ViewModels/Update/UpdateBannerViewModel.cs`
   - `ViewModels/Update/UpdateViewModel.cs`
8. Delete all update view files:
   - `Views/Update/UpdateBannerView.xaml` + `.xaml.cs`
   - `Views/Update/UpdateWindow.xaml` + `.xaml.cs`
   - `Views/UpdateBannerView.xaml` + `.xaml.cs` (duplicate at root Views/ level)
9. In `App.xaml.cs`, remove these DI registrations from `ConfigureServices()`:
   - `services.AddSingleton<IUpdateService, UpdateClientService>();`
   - `services.AddTransient<ViewModels.Update.UpdateViewModel>();`
   - `services.AddTransient<ViewModels.Update.UpdateBannerViewModel>();`
   - `services.AddTransient<Views.Update.UpdateWindow>();`
   - `services.AddTransient<Views.Update.UpdateBannerView>();`
10. In `App.xaml.cs`, remove the `using DocuFiller.Services.Update;` import

## Must-Haves

- [ ] DocuFiller.csproj has no ValidateUpdateClientFiles, ValidateReleaseFiles targets, and no External file references
- [ ] External/ directory deleted
- [ ] All update files (Services/Update/*, Models/Update/*, ViewModels/Update/*, Views/Update/*, Views/UpdateBannerView.*, DocuFiller/Services/Update/*) deleted
- [ ] App.xaml.cs has no update service DI registrations

## Inputs

- `DocuFiller.csproj`
- `App.xaml.cs`
- `Services/Update/IUpdateService.cs`
- `Services/Update/UpdateClientService.cs`
- `Services/Update/UpdateService.cs`
- `DocuFiller/Services/Update/UpdateDownloader.cs`
- `Models/Update/DaemonProgressInfo.cs`
- `Models/Update/DownloadProgress.cs`
- `Models/Update/DownloadStatus.cs`
- `Models/Update/UpdateClientResponseModels.cs`
- `Models/Update/UpdateConfig.cs`
- `Models/Update/VersionInfo.cs`
- `ViewModels/Update/UpdateBannerViewModel.cs`
- `ViewModels/Update/UpdateViewModel.cs`
- `Views/Update/UpdateBannerView.xaml`
- `Views/Update/UpdateWindow.xaml`
- `Views/UpdateBannerView.xaml`

## Expected Output

- `DocuFiller.csproj`
- `App.xaml.cs`

## Verification

grep -c "ValidateUpdateClientFiles\|ValidateReleaseFiles\|update-client" DocuFiller.csproj returns 0; test ! -d External; grep -c "IUpdateService\|UpdateViewModel\|UpdateBannerView\|UpdateWindow" App.xaml.cs returns 0
