# S03: 测试修复和文档同步 — UAT

**Milestone:** M004-l08k3s
**Written:** 2026-04-23T15:10:59.761Z

## UAT Type
- UAT mode: artifact-driven
- Why this mode is sufficient: This slice is a cleanup/documentation task with no runtime behavior changes. Verification is fully automatable via build, test, and grep checks.

## Preconditions
- S01 and S02 feature removals are complete (confirmed by prior slice completion)
- Codebase builds and all tests pass before S03 changes

## Smoke Test
1. Run `dotnet build` — expect 0 errors, no warnings about missing packages
2. Run `dotnet test` — expect all 71 tests to pass

## Test Cases

### 1. Build succeeds without Newtonsoft.Json
1. Run `dotnet build`
2. **Expected:** Build succeeds with 0 errors; no reference to Newtonsoft.Json in output

### 2. All tests pass
1. Run `dotnet test --verbosity minimal`
2. **Expected:** 71 passed, 0 failed, 0 skipped

### 3. No stale test data files remain
1. Check `Tests/Data/test-data.json` exists
2. **Expected:** File does not exist (deleted)

### 4. CLAUDE.md has no removed module references
1. Run `grep -ciE 'IDataParser[^<]|IExcelToWordConverter|IJsonEditorService|IUpdateService|UpdateClientService|ConverterWindow|JsonKeywordItem|JsonProjectModel|JSON Data Structure' CLAUDE.md`
2. **Expected:** 0 matches

### 5. README.md has no JSON/converter/update references
1. Run `grep -ciE 'JSON|IDataParser|converter|update-server|更新服务' README.md`
2. **Expected:** 0 matches

### 6. Obsolete docs are deleted
1. Check existence of: docs/EXTERNAL_SETUP.md, docs/VERSION_MANAGEMENT.md, docs/deployment-guide.md
2. Check existence of: docs/plans/2025-01-20-update-client-design-notes.md, docs/plans/2025-01-20-update-client-integration.md, docs/plans/2025-01-21-version-management-design.md, docs/plans/2025-01-21-version-management-implementation.md
3. **Expected:** All 7 files do not exist

### 7. No Newtonsoft.Json references in source
1. Run `grep -r "Newtonsoft" --include=*.cs --include=*.csproj`
2. **Expected:** 0 matches

## Edge Cases

### CLAUDE.md IExcelDataParser false positive
1. Run `grep -ciE 'DataParserService' CLAUDE.md`
2. **Expected:** May return matches for ExcelDataParserService (current active service) — this is correct, not a stale reference

### README.md Converters/ directory reference
1. Run `grep -ciE 'converter' README.md`
2. **Expected:** May return matches for WPF Converters/ directory — this is correct, not a stale converter tool reference

## Failure Signals
- dotnet build fails (missing package reference or broken code)
- dotnet test shows failures (broken test after file deletion)
- grep finds references to IDataParser, IJsonEditorService, IUpdateService in CLAUDE.md
- grep finds JSON data source instructions in README.md
- Obsolete doc files still exist on disk

## Not Proven By This UAT
- Runtime application behavior (no code changes to runtime logic)
- End-to-end document processing workflow (covered by existing integration tests)

## Notes for Tester
- The 2 CLAUDE.md grep hits from the plan's verification regex are known false positives from IExcelDataParser/ExcelDataParserService (active services)
- The Converters/ directory in project structure is WPF value converters, not the removed JSON→Excel converter tool
- This is the final slice of M004 — after S03, the codebase should be fully clean with Excel as the only data source
