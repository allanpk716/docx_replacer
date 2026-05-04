# S04: 服务接口补全 + CleanupService 日志补充

**Goal:** Verify and finalize: IDocumentProcessor interface fully covers DocumentProcessorService's public API (all methods and events); DocumentCleanupService's 5 catch blocks all have ILogger.LogError calls.
**Demo:** After this: IDocumentProcessor 接口覆盖 DocumentProcessorService 公共 API；CleanupService 5 个 catch 块有 ILogger

## Must-Haves

- Every public method and event on DocumentProcessorService has a matching member in IDocumentProcessor (Dispose from IDisposable excluded)
- Every catch (Exception) block in DocumentCleanupService has a _logger.LogError call
- dotnet build 0 errors, dotnet test all pass

## Verification

- `dotnet build --no-restore 2>&1 | tail -3` — expect 0 errors
- `dotnet test --no-build --verbosity minimal` — expect all pass
- `grep -c '_logger.LogError' DocuFiller/Services/DocumentCleanupService.cs` — expect >= 5

## Proof Level

- This slice proves: contract
- Real runtime required: no
- Human/UAT required: no

## Integration Closure

- No new wiring — this slice only ensures existing interface and logging contracts are complete.
- Downstream slices (S05, S06) consume IUpdateService and documentation, not affected by this slice.
- What remains before the milestone is truly usable end-to-end: S05 (auto-update), S06 (docs)

## Tasks

- [ ] **T01: Audit IDocumentProcessor completeness and fix gaps** `est:30m`
  - Why: The interface must fully cover the service's public API to maintain proper abstraction and testability.
  - Files: `Services/Interfaces/IDocumentProcessor.cs`, `Services/DocumentProcessorService.cs`
  - Do: Perform a method-by-method comparison between IDocumentProcessor interface and DocumentProcessorService public API. If any public method/event on the service is missing from the interface, add it. If the interface has stale members not on the service, flag for removal. Verify signatures match exactly (parameter types, return types, default values). Run dotnet build to confirm no compile errors.
  - Verify: `dotnet build --no-restore 2>&1 | tail -3` (expect 0 errors); `grep -c 'public.*(' Services/DocumentProcessorService.cs` (count service methods); `grep -c 'Task\|void\|event' Services/Interfaces/IDocumentProcessor.cs` (count interface members); counts should match
  - Done when: Every public method on DocumentProcessorService (except Dispose) has a corresponding member in IDocumentProcessor, dotnet build passes

- [ ] **T02: Audit DocumentCleanupService catch blocks and verify logging** `est:20m`
  - Why: All exception handlers should log errors for production diagnostics and failure visibility.
  - Files: `DocuFiller/Services/DocumentCleanupService.cs`
  - Do: Verify all catch (Exception) blocks in DocumentCleanupService have _logger.LogError calls with exception parameter. If any catch block is missing logging, add it following the existing pattern: `_logger.LogError(ex, $"清理文档/文件夹 {fileItem.FileName} 时发生异常")`. Verify dotnet build and dotnet test pass. This task also confirms the class constructor properly injects ILogger<DocumentCleanupService>.
  - Verify: `dotnet build --no-restore 2>&1 | tail -3` (expect 0 errors); `dotnet test --no-build --verbosity minimal 2>&1 | tail -5` (expect all pass); `grep -c '_logger.LogError' DocuFiller/Services/DocumentCleanupService.cs` (expect >= 5)
  - Done when: All 5 catch blocks have _logger.LogError, dotnet build and dotnet test pass

## Files Likely Touched

- `Services/Interfaces/IDocumentProcessor.cs`
- `DocuFiller/Services/DocumentCleanupService.cs`
