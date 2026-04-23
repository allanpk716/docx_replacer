---
id: S01
parent: M001-a60bo7
milestone: M001-a60bo7
provides:
  - ["ExcelDataParserService 三列解析能力", "ExcelFileSummary.DuplicateRowIds 字段", "DetectExcelFormat 私有方法", "6 个新增单元测试"]
requires:
  []
affects:
  []
key_files:
  - ["Models/ExcelFileSummary.cs", "Services/ExcelDataParserService.cs", "Tests/ExcelDataParserServiceTests.cs"]
key_decisions:
  - ["Used internal ExcelFormat enum (TwoColumn/ThreeColumn) scoped to ExcelDataParserService rather than public enum", "DetectExcelFormat heuristic: first non-empty row's first column matches #xxx# = 2-col, otherwise = 3-col"]
patterns_established:
  - ["Format detection as private method with internal enum — keeps implementation detail encapsulated", "Both Parse and Validate methods independently call DetectExcelFormat — self-contained, thread-safe"]
observability_surfaces:
  - none
drill_down_paths:
  - [".gsd/milestones/M001-a60bo7/slices/S01/tasks/T01-SUMMARY.md", ".gsd/milestones/M001-a60bo7/slices/S01/tasks/T02-SUMMARY.md"]
duration: ""
verification_result: passed
completed_at: 2026-04-23T04:07:00.786Z
blocker_discovered: false
---

# S01: 三列格式解析与 ID 唯一性校验

**Implemented 3-column Excel format auto-detection with ID uniqueness validation; all 61 tests pass with zero regressions**

## What Happened

This slice delivered automatic two-column/three-column Excel format detection and ID uniqueness validation in a single task cycle (T01 did all implementation; T02 verified).

The core change is a private `DetectExcelFormat` method in `ExcelDataParserService` that reads the first non-empty row's first column — if it matches the `#xxx#` keyword pattern, it's 2-column mode; otherwise it's 3-column mode. An internal `ExcelFormat` enum (TwoColumn/ThreeColumn) scopes this as an implementation detail within the service.

Both `ParseExcelFileAsync` and `ValidateExcelFileAsync` use the detected format. In 3-column mode, column 2 becomes the keyword and column 3 the value, with column 1 (ID) skipped during parsing but tracked via HashSet during validation. Duplicate IDs populate `ExcelFileSummary.DuplicateRowIds` and add specific errors to `ExcelValidationResult.Errors`.

A `DuplicateRowIds` property was added to the `ExcelFileSummary` model. Six new xunit tests cover all new scenarios. All 3 pre-existing tests pass unchanged, confirming zero regressions in 2-column behavior.

## Verification

Full test suite verification: 9/9 ExcelDataParserServiceTests pass (3 existing + 6 new), 61/61 total tests pass. Build completes with 0 errors. Specific test coverage:
- ParseExcelFileAsync_ThreeColumnFormat_SkipsIdAndParsesCorrectly: 3-col parsing correctness
- ParseExcelFileAsync_ThreeColumnFormat_DoesNotIncludeIdColumn: ID column exclusion
- ValidateExcelFileAsync_ThreeColumnFormat_PassesValidation: 3-col validation pass
- ValidateExcelFileAsync_ThreeColumnFormat_DetectsDuplicateIds: duplicate ID detection
- ValidateExcelFileAsync_TwoColumnFormat_NoDuplicateRowIds: 2-col no duplicate tracking
- ParseExcelFileAsync_TwoColumnFormat_UnchangedBehavior: 2-col backward compatibility

## Requirements Advanced

None.

## Requirements Validated

- R001 — 3-column Excel correctly parsed via DetectExcelFormat. 6 new xunit tests cover parsing correctness and format detection. All 61 tests pass.
- R002 — ID uniqueness validation via HashSet in ValidateExcelFileAsync. Duplicates populate DuplicateRowIds and ExcelValidationResult.Errors. Test ValidateExcelFileAsync_ThreeColumnFormat_DetectsDuplicateIds confirms.
- R003 — All 3 pre-existing tests pass unchanged. Full 61-test suite zero regressions. 2-column parsing and validation behavior completely unchanged.

## New Requirements Surfaced

None.

## Requirements Invalidated or Re-scoped

None.

## Operational Readiness

None.

## Deviations

T02's planned implementation work was already completed in T01, so T02 became a pure verification task with no code changes.

## Known Limitations

["DetectExcelFormat heuristic assumes ID column values never start with # — if a user puts a keyword-like value in column 1 of a 2-column file, it could be mis-detected as 3-column", "No integration-level test with real .xlsx files yet — S02 may add more comprehensive coverage"]

## Follow-ups

None.

## Files Created/Modified

None.
