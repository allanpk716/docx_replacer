# S01: 移除更新功能和 JSON 编辑器遗留 — UAT

**Milestone:** M004-l08k3s
**Written:** 2026-04-23T14:18:36.485Z

# UAT: S01 — 移除更新功能和 JSON 编辑器遗留

## Preconditions
- Working directory: `C:/WorkSpace/agent/docx_replacer/.gsd/worktrees/M004-l08k3s`
- No `External/` directory present
- No update-client.exe or related files in the project

## Test Cases

### TC-01: Build succeeds without External directory
1. Run `dotnet build`
2. **Expected:** Build completes with 0 errors
3. **Actual:** ✅ 0 errors, 54 pre-existing warnings

### TC-02: No update-related code in csproj
1. Search DocuFiller.csproj for `ValidateUpdateClientFiles`, `ValidateReleaseFiles`, `update-client`
2. **Expected:** 0 matches
3. **Actual:** ✅ 0 matches

### TC-03: No update DI registrations in App.xaml.cs
1. Search App.xaml.cs for `IUpdateService`, `UpdateViewModel`, `UpdateBannerView`, `UpdateWindow`
2. **Expected:** 0 matches
3. **Actual:** ✅ 0 matches

### TC-04: No update logic in MainWindowViewModel
1. Search MainWindowViewModel.cs for `IUpdateService`, `UpdateBanner`, `CheckForUpdate`, `ShowUpdate`, `OnUpdateAvailable`, `VersionInfo`, `UpdateViewModel`
2. **Expected:** 0 matches
3. **Actual:** ✅ 0 matches

### TC-05: No update UI in MainWindow.xaml
1. Search MainWindow.xaml for `CheckForUpdate`, `updateViews`, `检查更新`
2. **Expected:** 0 matches
3. **Actual:** ✅ 0 matches

### TC-06: No update event handlers in MainWindow.xaml.cs
1. Search MainWindow.xaml.cs for `CheckForUpdate`
2. **Expected:** 0 matches
3. **Actual:** ✅ 0 matches

### TC-07: All update service/model/viewmodel/view files deleted
1. Search for any .cs or .xaml files under `*/Update/*` path
2. **Expected:** No results
3. **Actual:** ✅ No results

### TC-08: All JSON editor files deleted
1. Check `Services/JsonEditorService.cs`, `Models/JsonKeywordItem.cs`, `Models/JsonProjectModel.cs`, `ViewModels/JsonEditorViewModel.cs`, `Views/JsonEditorWindow.xaml`, `Views/JsonEditorWindow.xaml.cs` do not exist
2. Search for any files matching `JsonEditor*`, `IJsonEditor*`, `JsonKeywordItem*`, `JsonProjectModel*`, `KeywordValidation*`, `IKeywordValidation*`
3. **Expected:** No results
4. **Actual:** ✅ All deleted, no matches

### TC-09: External directory deleted
1. Check `External/` directory does not exist
2. **Expected:** Directory not found
3. **Actual:** ✅ Directory deleted

## Summary
All 9 test cases pass. The update system and JSON editor leftovers are fully removed. Build compiles cleanly.
