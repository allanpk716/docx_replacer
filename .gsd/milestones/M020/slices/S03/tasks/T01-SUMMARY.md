---
id: T01
parent: S03
milestone: M020
key_files:
  - Utils/OpenXmlHelper.cs
  - Services/DocumentProcessorService.cs
  - Tests/DocuFiller.Tests.csproj
key_decisions:
  - Unified HasAncestorWithSameTag to use direct SdtProperties access (matching DPS version) rather than GetControlTag call (CCP version), for consistency
  - Added CommentManager and ILogger as parameters to AddProcessingComment rather than making OpenXmlHelper non-static
  - Removed debug logging from FindAllTargetRuns in shared version; callers can add their own
duration: 
verification_result: passed
completed_at: 2026-05-03T18:02:09.228Z
blocker_discovered: false
---

# T01: Create OpenXmlHelper shared utility class with 6 extracted methods, migrate DocumentProcessorService to use shared implementations

**Create OpenXmlHelper shared utility class with 6 extracted methods, migrate DocumentProcessorService to use shared implementations**

## What Happened

Created `Utils/OpenXmlHelper.cs` as a static utility class in the `DocuFiller.Utils` namespace, containing 6 shared methods extracted from the duplicated implementations in DocumentProcessorService and ContentControlProcessor:

1. `GetControlTag(SdtElement)` — pure method returning the control's tag value
2. `ExtractExistingText(SdtElement)` — concatenates all Text descendants for comment tracking
3. `FindContentContainer(SdtElement)` — null-coalescing pattern to find SdtContentRun/Block/Cell
4. `FindAllTargetRuns(SdtElement)` — simplified from original (removed debug logging, callers handle their own)
5. `AddProcessingComment(...)` — takes CommentManager + ILogger as parameters since it needs them
6. `HasAncestorWithSameTag(SdtElement, string)` — uses direct SdtProperties access (unified from both versions)

Migrated DocumentProcessorService by replacing all 10 call sites (GetControlTag×4, ExtractExistingText×1, AddProcessingComment×1, FindContentContainer×1, HasAncestorWithSameTag×3) with `OpenXmlHelper.` prefixed calls, then deleted all 6 private methods from the class.

Added `Utils/OpenXmlHelper.cs` to the test project's linked files in `Tests/DocuFiller.Tests.csproj`. Also added `using DocuFiller.Services` to OpenXmlHelper.cs for CommentManager access.

## Verification

`dotnet build DocuFiller.csproj` — 0 errors, `dotnet build Tests/DocuFiller.Tests.csproj` — 0 errors, `dotnet test --no-build --verbosity minimal` — 229 passed, 0 failed. Grep confirms zero private method remnants (GetControlTag, ExtractExistingText, FindContentContainer, FindAllTargetRuns, AddProcessingComment, HasAncestorWithSameTag) in DocumentProcessorService.cs, and all 10 call sites correctly reference `OpenXmlHelper.`.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet build DocuFiller.csproj` | 0 | ✅ pass | 4170ms |
| 2 | `dotnet build Tests/DocuFiller.Tests.csproj` | 0 | ✅ pass | 1760ms |
| 3 | `dotnet test Tests/DocuFiller.Tests.csproj --no-build --verbosity minimal` | 0 | ✅ pass (229 tests) | 9000ms |

## Deviations

Added `using DocuFiller.Services` import to OpenXmlHelper.cs for CommentManager access. Added OpenXmlHelper.cs to test project csproj linked files — the plan didn't mention this but it was required for the test project to compile.

## Known Issues

None.

## Files Created/Modified

- `Utils/OpenXmlHelper.cs`
- `Services/DocumentProcessorService.cs`
- `Tests/DocuFiller.Tests.csproj`
