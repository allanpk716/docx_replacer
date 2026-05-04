---
estimated_steps: 3
estimated_files: 1
skills_used:
  - verify-before-complete
---

# T02: Audit DocumentCleanupService catch blocks and verify logging

**Slice:** S04 — 服务接口补全 + CleanupService 日志补充
**Milestone:** M021

## Description

Verify all catch (Exception) blocks in DocumentCleanupService have _logger.LogError calls with exception parameter. If any catch block is missing logging, add it following the existing pattern. Verify dotnet build and dotnet test pass. This task also confirms the class constructor properly injects ILogger<DocumentCleanupService>.

## Steps

1. Search all `catch (Exception` blocks in `DocuFiller/Services/DocumentCleanupService.cs` (expect 5)
2. For each catch block, verify it contains `_logger.LogError(ex, ...)` within the block. If missing, add it following the pattern used in the other catch blocks
3. Run `dotnet build` and `dotnet test` to confirm no regressions

## Must-Haves

- [ ] All 5 catch (Exception) blocks in DocumentCleanupService have `_logger.LogError(ex, ...)` calls
- [ ] Constructor injects `ILogger<DocumentCleanupService>` (verify not null)
- [ ] dotnet build 0 errors, dotnet test all pass

## Verification

- `dotnet build --no-restore 2>&1 | tail -3` — expect 0 errors
- `dotnet test --no-build --verbosity minimal 2>&1 | tail -5` — expect all pass
- `grep -c '_logger.LogError' DocuFiller/Services/DocumentCleanupService.cs` — expect >= 5

## Inputs

- `DocuFiller/Services/DocumentCleanupService.cs` — cleanup service to audit for logging completeness

## Expected Output

- `DocuFiller/Services/DocumentCleanupService.cs` — service with any missing logging calls added (or confirmed unchanged if already complete)
