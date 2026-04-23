---
id: T03
parent: S02
milestone: M004-l08k3s
key_files:
  - DocuFiller.csproj
  - DocuFiller.sln
key_decisions:
  - (none)
duration: 
verification_result: passed
completed_at: 2026-04-23T14:43:40.179Z
blocker_discovered: false
---

# T03: Delete Tools directory (10 diagnostic tool projects) and remove from solution and csproj

**Delete Tools directory (10 diagnostic tool projects) and remove from solution and csproj**

## What Happened

Deleted the entire Tools/ directory containing 10 diagnostic tool subdirectories (CompareDocumentStructure, ControlRelationshipAnalyzer, DeepDiagnostic, DiagnoseTableStructure, E2ETest, ExcelFormattedTestGenerator, ExcelToWordVerifier, StepByStepSimulator, TableCellTest, TableStructureAnalyzer). Removed Tools and ExcelToWordVerifier exclusion lines from DocuFiller.csproj (these were Compile Remove / EmbeddedResource Remove / None Remove entries for directories that no longer exist). Cleaned DocuFiller.sln by removing all 10 Tools project entries, the Tools solution folder, and all corresponding ProjectConfigurationPlatforms and NestedProjects sections — leaving only DocuFiller and DocuFiller.Tests projects. Build passes with 0 errors and 0 warnings. All 71 tests pass. Comprehensive grep confirms zero residual references to any Tools project names or the Tools directory path.

## Verification

1. dotnet build — 0 errors, 0 warnings (both DocuFiller and DocuFiller.Tests)
2. dotnet test — 71 passed, 0 failed
3. test ! -d Tools — confirmed Tools directory deleted
4. grep for Tools/ Tools\ references in source files — 0 matches
5. grep for all 10 tool project names — 0 matches
6. All slice-level verification checks pass: DataFileType enum is Excel-only, no JSON fallback in ProcessDocumentsAsync, no "工具" tab in MainWindow

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet build --no-restore` | 0 | ✅ pass (0 errors, 0 warnings) | 810ms |
| 2 | `dotnet test --no-build --verbosity minimal` | 0 | ✅ pass (71/71 tests) | 784ms |
| 3 | `test ! -d Tools` | 0 | ✅ pass (Tools directory deleted) | 10ms |
| 4 | `grep -ri 'Tools[/\\\\]' source files` | 1 | ✅ pass (0 matches) | 100ms |
| 5 | `grep -ri 'ExcelToWordVerifier|ExcelFormattedTestGenerator|CompareDocumentStructure|ControlRelationshipAnalyzer|DeepDiagnostic|DiagnoseTableStructure|E2ETest|StepByStepSimulator|TableCellTest|TableStructureAnalyzer' source files` | 1 | ✅ pass (0 matches) | 120ms |

## Deviations

The task plan did not mention cleaning DocuFiller.sln — it only mentioned DocuFiller.csproj. However, the solution file referenced all 10 Tools projects, causing MSB3202 errors during dotnet build. Removing these entries was necessary to achieve the "0 errors" verification bar.

## Known Issues

None.

## Files Created/Modified

- `DocuFiller.csproj`
- `DocuFiller.sln`
