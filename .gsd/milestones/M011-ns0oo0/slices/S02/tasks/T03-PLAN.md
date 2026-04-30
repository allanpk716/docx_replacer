---
estimated_steps: 15
estimated_files: 1
skills_used: []
---

# T03: Write unit tests for DownloadProgressViewModel and updated DownloadUpdatesAsync

Write xunit tests covering the core logic:

1. DownloadProgressViewModel tests:
   - Initial state: ProgressPercent=0, IsDownloading=true, IsCompleted=false, speed/ETA empty
   - UpdateProgress: single update sets percent, speed/ETA remain empty (need 2+ points for speed)
   - Speed calculation: two progress updates with known time delta → correct MB/s
   - ETA calculation: remaining bytes / speed → correct time string
   - Cancel: CancelCommand triggers cancellation, IsDownloading=false
   - Complete: reaches 100%, IsCompleted=true
   - Error handling: set ErrorMessage, IsCompleted=true
   - Edge cases: 0 totalBytes (avoid divide-by-zero), very fast download (speed clamped)

2. UpdateService.DownloadUpdatesAsync CancellationToken test:
   - Verify the method signature accepts CancellationToken (compile-time check is sufficient)
   - Verify CancellationToken is forwarded: mock/verify pattern or just ensure it compiles and passes default(CancellationToken)

All tests must run in CI without WPF — ensure ViewModel tests don't require Dispatcher (the injected dispatcher wrapper handles this).

Add DownloadProgressViewModel.cs to test csproj Compile includes if not already done in T02.

## Inputs

- `ViewModels/DownloadProgressViewModel.cs`
- `Services/Interfaces/IUpdateService.cs`
- `Services/UpdateService.cs`
- `Tests/DocuFiller.Tests.csproj`

## Expected Output

- `Tests/DownloadProgressViewModelTests.cs`

## Verification

dotnet test --filter "FullyQualifiedName~DownloadProgressViewModelTests" --verbosity minimal
