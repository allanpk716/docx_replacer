# S03: 测试修复和文档同步

**Goal:** 清理 S01/S02 移除功能后残留的测试数据文件、死代码、过时文档，确保 dotnet test 通过、Newtonsoft.Json 依赖移除、CLAUDE.md/README.md/docs/ 与当前代码一致。
**Demo:** dotnet test 全部通过，文档（CLAUDE.md、README.md）与代码一致

## Must-Haves

- ## Success Criteria
- [ ] `dotnet test` 全部通过（当前 71 个测试）
- [ ] `Newtonsoft.Json` 包从 `DocuFiller.csproj` 中移除，编译通过
- [ ] `Tests/Data/test-data.json` 已删除
- [ ] `Tests/Templates/README.md` 不再引用 JSON 数据文件
- [ ] `Tests/verify-templates.bat` 不再检查 JSON 数据文件
- [ ] `CLAUDE.md` 无 IDataParser/DataParserService/IExcelToWordConverter/IJsonEditorService/IUpdateService/Update/Tools/External/JsonKeywordItem/JsonProjectModel 相关内容
- [ ] `README.md` 无 JSON 数据源/转换器/更新服务/Tools/update-server 相关内容
- [ ] `docs/` 下过时文档已清理或更新

## Proof Level

- This slice proves: contract — tests pass, build succeeds, grep confirms no stale references

## Integration Closure

- Upstream surfaces consumed: Clean codebase from S01 (no update services) and S02 (no JSON/converter/Tools)
- New wiring introduced in this slice: none
- What remains before the milestone is truly usable end-to-end: nothing — this is the final slice

## Verification

- none

## Tasks

- [x] **T01: Clean stale test artifacts and remove dead code** `est:30m`
  Remove JSON test data file (test-data.json), update Templates/README.md and verify-templates.bat to remove JSON references, remove unused ValidateJsonFormat method from ValidationHelper.cs, and remove Newtonsoft.Json package from DocuFiller.csproj. Verify build and all 71 tests still pass.
  - Files: `Tests/Data/test-data.json`, `Tests/Templates/README.md`, `Tests/verify-templates.bat`, `Utils/ValidationHelper.cs`, `DocuFiller.csproj`
  - Verify: dotnet build --no-restore && dotnet test --no-build --verbosity minimal

- [x] **T02: Rewrite CLAUDE.md to reflect Excel-only clean codebase** `est:30m`
  Rewrite CLAUDE.md to remove all references to removed modules: IDataParser/DataParserService, IExcelToWordConverter/ExcelToWordConverterService, IJsonEditorService, IUpdateService/UpdateClientService, ConverterWindow, Tools/ directory, External/ directory, Update/ subdirectories, JsonKeywordItem/JsonProjectModel models, JSON data structure section. Update service layer table to only show 12 active services (down from 15). Update DI lifecycle table to remove IDataParser. Update data models table to remove JSON-related models. Update file structure to remove Tools/, External/, Update/ subdirectories. Update processing pipeline to show Excel-only. Remove JSON Data Structure section entirely.
  - Files: `CLAUDE.md`
  - Verify: grep -ciE 'IDataParser|DataParserService|IExcelToWordConverter|ExcelToWordConverterService|IJsonEditorService|IUpdateService|UpdateClientService|ConverterWindow|Tools/|External/|JsonKeywordItem|JsonProjectModel|JSON Data Structure' CLAUDE.md

- [x] **T03: Update README.md and clean stale docs/** `est:1h`
  Update README.md to remove all JSON data source references (dual-source → Excel-only), remove converter tool section, remove update-server reference, remove Tools/ from project structure, remove Newtonsoft.Json from tech table, remove JSON format instructions from usage section. Delete docs/EXTERNAL_SETUP.md, docs/VERSION_MANAGEMENT.md, docs/deployment-guide.md (all entirely about the removed update system). Update docs/excel-data-user-guide.md to remove JSON→Excel converter references. Update docs/DocuFiller产品需求文档.md to remove converter module and JSON dual-source references. Update docs/DocuFiller技术架构文档.md to remove converter/update/JSON service references. Delete docs/plans/2025-01-20-update-client-*.md and docs/plans/2025-01-21-version-management-*.md (plans for removed features).
  - Files: `README.md`, `docs/EXTERNAL_SETUP.md`, `docs/VERSION_MANAGEMENT.md`, `docs/deployment-guide.md`, `docs/excel-data-user-guide.md`, `docs/DocuFiller产品需求文档.md`, `docs/DocuFiller技术架构文档.md`, `docs/plans/2025-01-20-update-client-design-notes.md`, `docs/plans/2025-01-20-update-client-integration.md`, `docs/plans/2025-01-21-version-management-design.md`, `docs/plans/2025-01-21-version-management-implementation.md`
  - Verify: grep -ciE 'JSON|IDataParser|converter|转换|update-server|更新服务|Tools/' README.md && echo '---' && ! test -f docs/EXTERNAL_SETUP.md && ! test -f docs/VERSION_MANAGEMENT.md && ! test -f docs/deployment-guide.md

## Files Likely Touched

- Tests/Data/test-data.json
- Tests/Templates/README.md
- Tests/verify-templates.bat
- Utils/ValidationHelper.cs
- DocuFiller.csproj
- CLAUDE.md
- README.md
- docs/EXTERNAL_SETUP.md
- docs/VERSION_MANAGEMENT.md
- docs/deployment-guide.md
- docs/excel-data-user-guide.md
- docs/DocuFiller产品需求文档.md
- docs/DocuFiller技术架构文档.md
- docs/plans/2025-01-20-update-client-design-notes.md
- docs/plans/2025-01-20-update-client-integration.md
- docs/plans/2025-01-21-version-management-design.md
- docs/plans/2025-01-21-version-management-implementation.md
