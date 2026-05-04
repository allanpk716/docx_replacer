---
estimated_steps: 23
estimated_files: 1
skills_used: []
---

# T03: Update CLAUDE.md to document new ViewModel architecture and CT.Mvvm usage

Update CLAUDE.md to reflect the new ViewModel architecture after M020's refactoring. The current CLAUDE.md describes the old monolithic MainWindowViewModel (1615 lines). After S01–S04, the architecture has changed to a coordinator + sub-ViewModel pattern with CommunityToolkit.Mvvm.

Steps:
1. Read current CLAUDE.md to understand existing architecture documentation
2. Update the Architecture Overview section to describe the new ViewModel structure:
   - MainWindowViewModel as coordinator (~400 lines, hand-written INPC, holds sub-VM references)
   - FillViewModel (CT.Mvvm, filling logic: single file + folder mode)
   - CleanupViewModel (CT.Mvvm, integrated from old independent version)
   - UpdateViewModel (CT.Mvvm, update check/download/settings)
   - DownloadProgressViewModel (CT.Mvvm, download progress display)
   - UpdateSettingsViewModel (CT.Mvvm, update settings editing)
3. Update the Service Layer Architecture table to include CT.Mvvm usage pattern
4. Add a new section or subsection on CommunityToolkit.Mvvm conventions:
   - New ViewModels use [ObservableProperty] and [RelayCommand] source generators
   - Existing MainWindowViewModel coordinator uses hand-written INPC (gradual migration)
   - ObservableObject.cs and RelayCommand.cs retained for backward compatibility
5. Update the DI lifecycle table if sub-VM registrations changed
6. Update Key Data Models if new ViewModel-related models were introduced
7. Verify CLAUDE.md mentions CommunityToolkit.Mvvm at least twice and lists all sub-ViewModels

Must-haves:
- [ ] ViewModel structure section describes coordinator + sub-VM pattern
- [ ] CommunityToolkit.Mvvm usage guidance is documented
- [ ] All 6 ViewModels listed with their inheritance/CT.Mvvm status
- [ ] DI registration section updated if needed

## Inputs

- `CLAUDE.md`

## Expected Output

- `CLAUDE.md`

## Verification

grep -c 'CommunityToolkit' CLAUDE.md (>= 2); grep -c 'FillViewModel\|CleanupViewModel\|UpdateViewModel\|DownloadProgressViewModel\|UpdateSettingsViewModel' CLAUDE.md (>= 5); grep -c 'coordinator\|协调' CLAUDE.md (>= 1); wc -l CLAUDE.md (should be similar or larger than original)
