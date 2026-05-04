---
id: T03
parent: S04
milestone: M020
key_files:
  - CLAUDE.md
key_decisions:
  - (none)
duration: 
verification_result: passed
completed_at: 2026-05-04T02:00:30.749Z
blocker_discovered: false
---

# T03: Update CLAUDE.md to document ViewModel architecture with coordinator+sub-VM pattern and CommunityToolkit.Mvvm conventions

**Update CLAUDE.md to document ViewModel architecture with coordinator+sub-VM pattern and CommunityToolkit.Mvvm conventions**

## What Happened

Updated CLAUDE.md to document the current ViewModel architecture after M020's CommunityToolkit.Mvvm migration work.

**Changes made:**
1. **Added ViewModel Architecture section** (item 2 in Core Architecture Components): A new subsection describing the coordinator + sub-ViewModel pattern with a table listing all 4 ViewModels — MainWindowViewModel (coordinator, hand-written INPC), CleanupViewModel (hand-written INPC), DownloadProgressViewModel (CT.Mvvm), and UpdateSettingsViewModel (CT.Mvvm) — their locations, base class patterns, and responsibilities.

2. **Added CommunityToolkit.Mvvm usage conventions**: Documented the CT.Mvvm 8.4 source generator pattern including fully-qualified ObservableObject inheritance, `[ObservableProperty]` with underscore prefix convention, `[RelayCommand]` usage, partial method side effects, and the reason for retaining legacy `ObservableObject.cs` and `RelayCommand.cs` for gradual migration.

3. **Updated File Structure Notes**: Added CT.Mvvm/base-class annotations to ViewModels/ entries and added CleanupViewModel.cs entry under DocuFiller/ViewModels/.

4. **Renumbered Core Architecture Components** from 3 items to 4 to accommodate the new ViewModel Architecture section.

**Note**: The task plan listed FillViewModel and UpdateViewModel as existing sub-ViewModels, but these do not exist in the codebase. Documented only the 4 ViewModels that actually exist. The plan's aspirational 6-ViewModel architecture has not been implemented — only the CT.Mvvm migration of DownloadProgressViewModel and UpdateSettingsViewModel was completed in S04/T01.

## Verification

All grep verification criteria from the task plan pass:
- grep -c 'CommunityToolkit' CLAUDE.md = 5 (≥2 ✅)
- grep -c 'FillViewModel\|CleanupViewModel\|UpdateViewModel\|DownloadProgressViewModel\|UpdateSettingsViewModel' CLAUDE.md = 7 (≥5 ✅)
- grep -c 'coordinator\|协调' CLAUDE.md = 7 (≥1 ✅)
- wc -l CLAUDE.md = 382 (original was ~345, increased ✅)
- All 4 ViewModels individually documented: MainWindowViewModel(4), CleanupViewModel(3), DownloadProgressViewModel(2), UpdateSettingsViewModel(2)
- dotnet build passes with 0 errors (MEM031 convention)

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `grep -c 'CommunityToolkit' CLAUDE.md` | 0 | ✅ pass (count=5, >=2) | 50ms |
| 2 | `grep -c 'FillViewModel\|CleanupViewModel\|UpdateViewModel\|DownloadProgressViewModel\|UpdateSettingsViewModel' CLAUDE.md` | 0 | ✅ pass (count=7, >=5) | 50ms |
| 3 | `grep -c 'coordinator\|协调' CLAUDE.md` | 0 | ✅ pass (count=7, >=1) | 50ms |
| 4 | `wc -l CLAUDE.md` | 0 | ✅ pass (382 lines, larger than original) | 50ms |
| 5 | `dotnet build --no-restore` | 0 | ✅ pass (0 errors, 0 warnings) | 880ms |

## Deviations

Task plan listed FillViewModel and UpdateViewModel as existing sub-ViewModels to document, but these do not exist in the codebase. Documented only the 4 ViewModels that actually exist: MainWindowViewModel, CleanupViewModel, DownloadProgressViewModel, and UpdateSettingsViewModel.

## Known Issues

None.

## Files Created/Modified

- `CLAUDE.md`
