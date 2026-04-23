---
phase: M001-a60bo7
phase_name: Excel 行 ID 列支持
project: DocuFiller
generated: 2026-04-23T04:24:13Z
counts:
  decisions: 2
  lessons: 2
  patterns: 2
  surprises: 2
missing_artifacts: []
---

### Decisions

- **D001: Format detection via keyword pattern heuristic** — Read first non-empty row's first column; if it matches `#xxx#` pattern, treat as 2-column mode, otherwise 3-column mode. Zero config, minimal intrusion, leverages existing keyword format convention.
  Source: M001-a60bo7-CONTEXT.md/Architectural Decisions

- **D002: DetectExcelFormat encapsulated as private method** — Format detection is an internal implementation detail of `ExcelDataParserService`, not exposed via `IExcelDataParser` interface. Keeps the interface stable and callers unaware of format differences.
  Source: M001-a60bo7-CONTEXT.md/Architectural Decisions

### Lessons

- **ParseExcelFileAsync throws NullReferenceException for empty worksheets** — When `worksheet.Dimension` is null (completely empty sheet), the method crashes rather than returning an empty dictionary. This is a pre-existing issue not introduced by the 3-column feature, but should be guarded against.
  Source: S02-SUMMARY.md/Deviations

- **EPPlus LicenseContext must be set before Excel operations** — `ExcelPackage.LicenseContext = LicenseContext.NonCommercial` is required. If forgotten, tests or runtime will fail with a license exception. This was already handled in the codebase but is worth remembering for any future Excel-related work.
  Source: M001-a60bo7-CONTEXT.md/Technical Constraints

### Patterns

- **Format detection as private method with internal enum** — `DetectExcelFormat()` returns an internal `ExcelFormat` enum (TwoColumn/ThreeColumn) scoped entirely within `ExcelDataParserService`. Both Parse and Validate independently call it, making the approach thread-safe and self-contained.
  Source: S01-SUMMARY.md/patterns_established

- **Edge case test naming convention: {Method}_{Scenario}_{ExpectedBehavior}** — Produces self-documenting test names that serve as both specification and regression safety net (e.g., `ValidateExcelFileAsync_ThreeColumnFormat_DetectsDuplicateIds`).
  Source: S02-SUMMARY.md/patterns_established

### Surprises

- **T01 in S01 completed all planned T02 implementation work** — The feature was fully implemented in a single task, making T02 a pure verification pass. Task estimation for similar private-method additions may be lower than expected.
  Source: S01-SUMMARY.md/Deviations

- **Empty file behavior differs between Parse and Validate** — `ParseExcelFileAsync` throws NullReferenceException for empty files while `ValidateExcelFileAsync` gracefully reports errors. This asymmetry is pre-existing but surfaced during edge case testing.
  Source: S02-SUMMARY.md/Known Limitations
