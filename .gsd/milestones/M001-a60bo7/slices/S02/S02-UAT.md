# S02: 测试覆盖验证 — UAT

**Milestone:** M001-a60bo7
**Written:** 2026-04-23T04:21:01.228Z

# S02: 测试覆盖验证 — UAT

**Milestone:** M001-a60bo7
**Written:** 2026-04-23

## UAT Type

- UAT mode: artifact-driven
- Why this mode is sufficient: This slice adds only test code with no runtime behavior changes. Verification is fully automated via `dotnet test`. No human-experience or live-runtime testing is needed.

## Preconditions

- .NET 8 SDK installed
- Project restored (`dotnet restore`)
- Tests/DocuFiller.Tests.csproj builds successfully

## Smoke Test

1. Run `dotnet test` from the project root
2. **Expected:** Output shows "已通过! - 失败: 0，通过: 71，已跳过: 0" (all 71 tests pass)

## Test Cases

### 1. Edge case unit tests for 3-column format

1. Run `dotnet test --filter "ExcelDataParserServiceTests"`
2. **Expected:** 18 tests pass (6 pre-existing + 12 new edge case tests)

### 2. 3-column end-to-end integration test

1. Run `dotnet test --filter "EndToEnd_ThreeColumnExcelToWord_ReplacesCorrectlyAndExcludesIds"`
2. **Expected:** 1 test passes — verifies ParseExcelFileAsync → ProcessDocumentWithFormattedDataAsync pipeline with 3-column Excel data produces correct Word output with ID values excluded

### 3. Zero regressions

1. Run `dotnet test` (full suite)
2. **Expected:** 71 tests pass, 0 failures, 0 skipped — all pre-existing tests remain green

## Edge Cases

### Empty Excel file handling

1. The test suite documents that `ParseExcelFileAsync` throws `NullReferenceException` for empty worksheets (null Dimension). This is pre-existing behavior, not a regression.
2. **Expected:** Test `ParseExcelFileAsync_EmptyFile_ThrowsNullReferenceException` passes

### Whitespace-trimmed ID duplicate detection

1. IDs with leading/trailing whitespace (e.g., "  001  ") should match "001" for duplicate detection
2. **Expected:** Test `ValidateExcelFileAsync_IdWithWhitespace_TreatsAsDuplicateAfterTrim` passes

### Multiple duplicate IDs

1. When multiple different IDs each have duplicates (e.g., "001"×2 and "002"×2), all duplicate IDs should be reported
2. **Expected:** Test `ValidateExcelFileAsync_MultipleDuplicateIds_ReportsAllDuplicates` passes

## Failure Signals

- Any test failure in `dotnet test` output
- Test count != 71 (indicates tests were accidentally removed or added without intent)

## Not Proven By This UAT

- Performance characteristics of 3-column parsing at scale (large Excel files with many rows)
- Runtime behavior through the WPF UI (tests verify service-layer behavior only)
- The NullReferenceException for empty worksheets is a known pre-existing issue, not fixed in this slice

## Notes for Tester

This slice is purely additive test code. No production code was changed. The 12 new unit tests and 1 integration test provide coverage for boundary conditions of the 3-column Excel format feature built in S01. The full suite of 71 tests provides confidence that both new and existing functionality work correctly.
