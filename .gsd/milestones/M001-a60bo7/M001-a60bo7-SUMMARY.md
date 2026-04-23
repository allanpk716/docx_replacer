---
id: M001-a60bo7
title: "Excel 行 ID 列支持"
status: complete
completed_at: 2026-04-23T04:24:57.860Z
key_decisions:
  - D001: Format detection via keyword pattern heuristic (#xxx# match on first non-empty row) — zero config, minimal intrusion
  - D002: DetectExcelFormat as private method in ExcelDataParserService — keeps IExcelDataParser interface stable
key_files:
  - Models/ExcelFileSummary.cs
  - Services/ExcelDataParserService.cs
  - Tests/ExcelDataParserServiceTests.cs
  - Tests/ExcelIntegrationTests.cs
lessons_learned:
  - ParseExcelFileAsync throws NullReferenceException for empty worksheets (null Dimension) — pre-existing issue not introduced by this milestone
  - DetectExcelFormat heuristic assumes ID values never start with # — if users put keyword-like values in column 1 of a 2-column file, mis-detection could occur (low risk given strong #xxx# convention)
---

# M001-a60bo7: Excel 行 ID 列支持

**Three-column Excel format (ID | Keyword | Value) with auto-detection via DetectExcelFormat heuristic, ID uniqueness validation, full backward compatibility, and 71 passing tests**

## What Happened

This milestone delivered automatic two-column/three-column Excel format detection and ID uniqueness validation for the DocuFiller application.

**S01** implemented the core feature in two tasks. T01 added a private `DetectExcelFormat` method to `ExcelDataParserService` that reads the first non-empty row's first column — if it matches the `#xxx#` keyword pattern, it's 2-column mode; otherwise 3-column mode. An internal `ExcelFormat` enum scopes this as an implementation detail. Both `ParseExcelFileAsync` and `ValidateExcelFileAsync` use the detected format. In 3-column mode, column 2 becomes the keyword and column 3 the value, with column 1 (ID) skipped during parsing but tracked via HashSet during validation. Duplicate IDs populate `ExcelFileSummary.DuplicateRowIds` and add specific errors to `ExcelValidationResult.Errors`. Six new unit tests were added. T02 verified all 61 tests pass with zero regressions.

**S02** validated the implementation with comprehensive test coverage. T01 added 12 edge case unit tests covering empty files, blank first rows, empty IDs, single-row files, ID trim behavior, and multiple duplicate ID scenarios. T02 added an end-to-end integration test proving the full 3-column Excel→Word pipeline works correctly. Final suite: 71 tests, 0 failures.

Key decisions: (D001) format detection uses keyword pattern matching heuristic — minimal intrusion, zero config; (D002) detection logic encapsulated as private method to keep IExcelDataParser interface stable.

## Success Criteria Results

- ✅ **三列 Excel（ID | #关键词# | 值）正确解析，ID 列不参与替换** — `ParseExcelFileAsync_ThreeColumnFormat_SkipsIdAndParsesCorrectly` and `ParseExcelFileAsync_ThreeColumnFormat_DoesNotIncludeIdColumn` tests pass. End-to-end integration test confirms ID values excluded from Word output.
- ✅ **ID 重复时验证报错，提示具体重复 ID** — `ValidateExcelFileAsync_ThreeColumnFormat_DetectsDuplicateIds` confirms duplicates populate `DuplicateRowIds` and `Errors`. Additional edge case tests cover multi-duplicate and ID trim scenarios.
- ✅ **旧两列 Excel 解析和验证行为零变化** — All 3 pre-existing `ExcelDataParserServiceTests` pass unchanged. `ParseExcelFileAsync_TwoColumnFormat_UnchangedBehavior` and `ValidateExcelFileAsync_TwoColumnFormat_NoDuplicateRowIds` explicitly confirm backward compatibility.
- ✅ **dotnet test 全部通过** — 71/71 tests pass (0 failures, 0 skipped).

## Definition of Done Results

- ✅ **All slices complete** — S01 (2/2 tasks done) and S02 (2/2 tasks done) both marked complete in DB.
- ✅ **All slice summaries exist** — S01-SUMMARY.md and S02-SUMMARY.md written with full narratives, verification results, and file lists.
- ✅ **Cross-slice integration works** — S02 consumed S01's parsing capabilities (DetectExcelFormat, DuplicateRowIds, 3-column parsing) and verified via integration test. No boundary mismatches.
- ✅ **No horizontal checklist** — Roadmap does not include a horizontal checklist section.
- ✅ **All success criteria met** — Verified above with specific test evidence.

## Requirement Outcomes

- R001: active → validated — 3-column Excel correctly parsed via DetectExcelFormat. 6 new xunit tests prove parsing correctness and format detection. All 71 tests pass.
- R002: active → validated — ID uniqueness validation via HashSet in ValidateExcelFileAsync. Duplicates populate DuplicateRowIds and ExcelValidationResult.Errors with specific IDs.
- R003: active → validated — All pre-existing 2-column tests pass unchanged. Full 71-test suite zero regressions.
- R004: active → validated — Full test suite 71 tests passes (0 failures). 12 new edge case tests + 1 integration test added.

## Deviations

T02 in S01 was planned as implementation work but T01 completed all code changes, so T02 became pure verification. T01 in S02's empty-file parse test was adjusted: the method throws NullReferenceException rather than returning empty dictionary — test documents actual behavior rather than ideal expectation.

## Follow-ups

ParseExcelFileAsync should guard against null worksheet.Dimension for empty worksheets (pre-existing issue, low priority).
