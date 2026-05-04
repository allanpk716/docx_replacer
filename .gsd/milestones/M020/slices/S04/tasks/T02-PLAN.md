---
estimated_steps: 16
estimated_files: 1
skills_used: []
---

# T02: Update CLAUDE.md file structure and OpenXML references

Update the structural accuracy sections of CLAUDE.md:

1. **File Structure Notes**: Complete rewrite of the directory tree to match actual codebase:
   - Remove `Views/` top-level directory (MainWindow.xaml is at project root, not in Views/)
   - Change `Utils/OpenXmlTableCellHelper.cs` → `Utils/OpenXmlHelper.cs`
   - Change `Services/Interfaces/ 10 个服务接口定义` → `Services/Interfaces/ 11 个服务接口定义`
   - Add `Services/UpdateService.cs` to Services/ section
   - Remove `scripts/config/` (directory no longer exists)
   - Add `update-server/` directory with Go update server description
   - Add `Resources/` directory (app.ico, app.png)
   - Add `DocuFiller/Views/UpdateSettingsWindow.xaml`, `DownloadProgressWindow.xaml` to DocuFiller/ section
   - Add `ViewModels/DownloadProgressViewModel.cs`, `UpdateSettingsViewModel.cs`
   - Add `Cli/Commands/UpdateCommand.cs` to Cli/Commands section
   - Keep output directories (Examples/, Templates/, Logs/, Output/) as-is

2. **OpenXML Integration section**: Update the reference from `Utils/OpenXmlTableCellHelper.cs` to `Utils/OpenXmlHelper.cs` in the 相关文件 subsection under Table Content Control Handling

3. **Final verification**: Ensure every file path mentioned in File Structure Notes actually exists in the codebase. Run: for each file mentioned in the tree, check existence with ls.

All changes are to `CLAUDE.md` only.

## Inputs

- `CLAUDE.md`
- `Utils/OpenXmlHelper.cs`
- `Services/UpdateService.cs`
- `update-server/main.go`
- `DocuFiller/Views/UpdateSettingsWindow.xaml`

## Expected Output

- `CLAUDE.md`

## Verification

grep -c 'OpenXmlHelper' CLAUDE.md returns >= 2 and grep -c 'OpenXmlTableCellHelper' CLAUDE.md returns 0 and grep -c 'update-server' CLAUDE.md returns >= 1 and grep -c '11 个服务接口' CLAUDE.md returns >= 1 and grep -c 'UpdateCommand' CLAUDE.md returns >= 1
