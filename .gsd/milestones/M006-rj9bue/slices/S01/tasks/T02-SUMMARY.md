---
id: T02
parent: S01
milestone: M006-rj9bue
key_files:
  - (none)
key_decisions:
  - (none)
duration: 
verification_result: untested
completed_at: 2026-04-24T00:10:08.058Z
blocker_discovered: false
---

# T02: Implemented version-compatible ServiceFactory, TestDataHelper, and 8 infrastructure smoke tests — all pass

**Implemented version-compatible ServiceFactory, TestDataHelper, and 8 infrastructure smoke tests — all pass**

## What Happened

Implemented ServiceFactory using ServiceCollection DI with conditional IDataParser registration. The factory uses AppDomain.CurrentDomain.GetAssemblies() to detect if IDataParser/DataParserService types exist at runtime. On M004+ code (no IDataParser), it skips registration. On d81cd00 (has IDataParser), it registers the implementation. DI auto-resolves DocumentProcessorService constructor parameters in both cases.

Implemented TestDataHelper with upward navigation from AppContext.BaseDirectory to find test_data/2026年4月23日/. Exposes LD68ExcelPath, FD68ExcelPath, TemplateDirectory, and helper methods for specific templates (CE01, CE06-01, CE00).

Infrastructure tests verify: ServiceFactory builds processor (8/8), both Excel files found and parseable, 43 templates discovered, LD68 parses as three-column (73+ keywords, #产品名称#=Lyse), FD68 parses as two-column (58+ keywords, #产品名称#=Fluorescent Dye), 30+ common keywords between the two.

## Verification

dotnet test Tests/E2ERegression/ --filter Infrastructure — 8 passed, 0 failed

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| — | No verification commands discovered | — | — | — |

## Deviations

None.

## Known Issues

None.

## Files Created/Modified

None.
