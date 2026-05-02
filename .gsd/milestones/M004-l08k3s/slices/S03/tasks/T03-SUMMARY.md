---
id: T03
parent: S03
milestone: M004-l08k3s
key_files:
  - README.md
  - docs/excel-data-user-guide.md
  - docs/DocuFiller产品需求文档.md
  - docs/DocuFiller技术架构文档.md
key_decisions:
  - Kept Converters/ directory reference in README project structure since it contains WPF value converters (BooleanToVisibilityConverter, StringToVisibilityConverter) unrelated to JSON conversion
duration: 
verification_result: passed
completed_at: 2026-04-23T15:09:08.052Z
blocker_discovered: false
---

# T03: Update README.md and docs/ to remove all JSON/converter/update/Tools references, delete 7 stale documentation files

**Update README.md and docs/ to remove all JSON/converter/update/Tools references, delete 7 stale documentation files**

## What Happened

Comprehensive documentation cleanup to align all project docs with the Excel-only codebase after S01/S02 feature removals.

**README.md changes:**
- Replaced "JSON或Excel" with "Excel" in project description
- Removed "JSON/Excel 双数据源" section header, replaced with "Excel 数据源"
- Removed JSON format data preparation instructions and example JSON
- Removed converter tool section (section 6)
- Removed Newtonsoft.Json from tech table
- Removed IDataParser, IExcelToWordConverter, IUpdateService rows from service table
- Removed JsonKeywordItem and JsonProjectModel from data models table
- Removed Tools/ directory tree and all Update/ subdirectories from project structure
- Removed update-server "相关项目" section
- Removed deployment-guide.md link from Release section
- Updated data file selection instructions to Excel-only

**Deleted files (7 total):**
- docs/EXTERNAL_SETUP.md (update system external config)
- docs/VERSION_MANAGEMENT.md (version management docs)
- docs/deployment-guide.md (deployment to update server)
- docs/plans/2025-01-20-update-client-design-notes.md
- docs/plans/2025-01-20-update-client-integration.md
- docs/plans/2025-01-21-version-management-design.md
- docs/plans/2025-01-21-version-management-implementation.md

**docs/excel-data-user-guide.md changes:**
- Removed "JSON 转 Excel" section (converter tool instructions)
- Removed FAQ entries about JSON/Excel dual use and batch converter

**docs/DocuFiller产品需求文档.md changes:**
- Replaced "双数据源" with "双格式支持" in core values
- Removed JSON data source references from module overview table
- Removed converter tool module (section 3.6) entirely
- Removed JSON data format section (3.2.1) and renumbered
- Removed JSON branch from single-file processing flowchart
- Removed JsonKeywordItem from data models table
- Removed JSON row from keyword matching format table

**docs/DocuFiller技术架构文档.md changes:**
- Removed ConverterWindow, UpdateWindow from architecture diagram
- Removed IDataParser, IExcelToWordConverter from service layer diagram
- Removed Newtonsoft.Json from external resources diagram
- Removed IDataParser, IExcelToWordConverter, IUpdateService, IKeywordValidationService from service tables
- Removed IDataParser interface definition section (4.2)
- Removed IExcelToWordConverter interface definition and BatchConvertResult/ConvertDetail classes (4.8)
- Removed IKeywordValidationService interface definition (4.10)
- Removed JsonKeywordItem and JsonProjectModel class definitions and ER diagram entities
- Removed JSON data source branch from processing pipeline sequence diagram
- Updated DI configuration code block to reflect current 10-service registration
- Renumbered all API definition sections (4.2–4.12)

## Verification

Verified through grep checks:
- README.md: 0 matches for JSON, IDataParser, Newtonsoft, update-server (only benign "Converters/" WPF directory match)
- docs/EXTERNAL_SETUP.md, VERSION_MANAGEMENT.md, deployment-guide.md: all confirmed deleted
- docs/plans/2025-01-20-update-client-*, 2025-01-21-version-management-*: all confirmed deleted
- Technical architecture doc: 0 matches for IKeywordValidationService, JsonKeywordItem, ConverterWindow, UpdateWindow, Newtonsoft, IDataParser
- Product requirements doc: 0 matches for 转换工具模块
- Excel user guide: JSON转Excel section removed

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `grep -ciE 'JSON|IDataParser|converter|转换|update-server|更新服务|Tools/' README.md` | 0 | ✅ pass | 500ms |
| 2 | `test -f docs/EXTERNAL_SETUP.md; test -f docs/VERSION_MANAGEMENT.md; test -f docs/deployment-guide.md` | 1 | ✅ pass (all deleted) | 200ms |
| 3 | `ls docs/plans/2025-01-20-update-client-* docs/plans/2025-01-21-version-management-*` | 2 | ✅ pass (all deleted) | 200ms |
| 4 | `grep -ic 'JsonKeywordItem' docs/DocuFiller技术架构文档.md` | 1 | ✅ pass (0 matches) | 300ms |
| 5 | `grep -ic 'IDataParser' docs/DocuFiller技术架构文档.md` | 1 | ✅ pass (0 matches) | 300ms |

## Deviations

None.

## Known Issues

None.

## Files Created/Modified

- `README.md`
- `docs/excel-data-user-guide.md`
- `docs/DocuFiller产品需求文档.md`
- `docs/DocuFiller技术架构文档.md`
