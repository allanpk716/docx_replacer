---
id: T02
parent: S02
milestone: M020
key_files:
  - Services/Interfaces/IDocumentProcessor.cs
  - Services/DocumentProcessorService.cs
  - Tests/Integration/HeaderFooterCommentIntegrationTests.cs
  - Tests/DocuFiller.Tests/Cli/CommandValidationTests.cs
key_decisions:
  - Used FormattedCellValue.FromPlainText() factory method for test data construction instead of object initializer (PlainText is a computed property)
duration: 
verification_result: untested
completed_at: 2026-05-03T17:50:40.655Z
blocker_discovered: false
---

# T02: Remove dead code: ProcessSingleDocumentAsync and GenerateOutputFileNameWithTimestamp from DocumentProcessorService, update interface and tests

**Remove dead code: ProcessSingleDocumentAsync and GenerateOutputFileNameWithTimestamp from DocumentProcessorService, update interface and tests**

## What Happened

Deleted ProcessSingleDocumentAsync (public method on IDocumentProcessor and its implementation in DocumentProcessorService) and GenerateOutputFileNameWithTimestamp (unused private method). Updated StubDocumentProcessor in CommandValidationTests to remove the stub method. Rewrote 3 tests in HeaderFooterCommentIntegrationTests to use ProcessDocumentWithFormattedDataAsync with FormattedCellValue.FromPlainText() instead of the old Dictionary<string, object> + ProcessSingleDocumentAsync pattern. Had to fix the initial approach of setting PlainText directly (it's a computed property) by switching to the FromPlainText factory method. Build: 0 errors. Tests: 256 passed (229 + 27), 0 failed.

## Verification

grep confirms 0 occurrences of ProcessSingleDocumentAsync and GenerateOutputFileNameWithTimestamp in .cs files (excluding .gsd). dotnet build: 0 errors. dotnet test: 256 passed, 0 failed. All 3 rewritten integration tests pass.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| — | No verification commands discovered | — | — | — |

## Deviations

Plan suggested wrapping values as new FormattedCellValue { PlainText = ..., Fragments = [...] } but PlainText is a read-only computed property. Used FormattedCellValue.FromPlainText() factory method instead, which is the correct API.

## Known Issues

None.

## Files Created/Modified

- `Services/Interfaces/IDocumentProcessor.cs`
- `Services/DocumentProcessorService.cs`
- `Tests/Integration/HeaderFooterCommentIntegrationTests.cs`
- `Tests/DocuFiller.Tests/Cli/CommandValidationTests.cs`
