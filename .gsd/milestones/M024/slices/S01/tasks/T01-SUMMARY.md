---
id: T01
parent: S01
milestone: M024
key_files:
  - ViewModels/UpdateStatusViewModel.cs
  - Tests/DocuFiller.Tests/UpdateStatusViewModelTests.cs
key_decisions:
  - (none)
duration: 
verification_result: passed
completed_at: 2026-05-07T07:36:05.253Z
blocker_discovered: false
---

# T01: Added ShowCheckingAnimation computed property and moved Checking state before Task.Delay for immediate spinner display

**Added ShowCheckingAnimation computed property and moved Checking state before Task.Delay for immediate spinner display**

## What Happened

Implemented the ShowCheckingAnimation computed property on UpdateStatusViewModel that returns true when either CurrentUpdateStatus is Checking (auto-check path) or IsCheckingUpdate is true (manual check path). Moved the CurrentUpdateStatus = UpdateStatus.Checking assignment from inside InitializeUpdateStatusAsync to InitializeAsync, placed before the Task.Delay(5000) call, gated by a null check on _updateService. This ensures the spinner animation becomes visible immediately on startup rather than after the 5-second delay. Added PropertyChanged notifications for ShowCheckingAnimation in both OnCurrentUpdateStatusChanged and OnIsCheckingUpdateChanged partial methods. Added 5 unit tests covering all ShowCheckingAnimation state combinations.

## Verification

All 5 new ShowCheckingAnimation tests pass. Full test suite passes with 274 unit tests and 27 E2E tests, 0 failures, no regressions.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet test --filter "FullyQualifiedName~ShowCheckingAnimation"` | 0 | ✅ pass | 7940ms |
| 2 | `dotnet test (full suite)` | 0 | ✅ pass | 12000ms |

## Deviations

None.

## Known Issues

None.

## Files Created/Modified

- `ViewModels/UpdateStatusViewModel.cs`
- `Tests/DocuFiller.Tests/UpdateStatusViewModelTests.cs`
