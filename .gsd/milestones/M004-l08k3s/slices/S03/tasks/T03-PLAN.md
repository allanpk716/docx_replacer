---
estimated_steps: 1
estimated_files: 11
skills_used: []
---

# T03: Update README.md and clean stale docs/

Update README.md to remove all JSON data source references (dual-source → Excel-only), remove converter tool section, remove update-server reference, remove Tools/ from project structure, remove Newtonsoft.Json from tech table, remove JSON format instructions from usage section. Delete docs/EXTERNAL_SETUP.md, docs/VERSION_MANAGEMENT.md, docs/deployment-guide.md (all entirely about the removed update system). Update docs/excel-data-user-guide.md to remove JSON→Excel converter references. Update docs/DocuFiller产品需求文档.md to remove converter module and JSON dual-source references. Update docs/DocuFiller技术架构文档.md to remove converter/update/JSON service references. Delete docs/plans/2025-01-20-update-client-*.md and docs/plans/2025-01-21-version-management-*.md (plans for removed features).

## Inputs

- `README.md`
- `docs/EXTERNAL_SETUP.md`
- `docs/VERSION_MANAGEMENT.md`
- `docs/deployment-guide.md`
- `docs/excel-data-user-guide.md`
- `docs/DocuFiller产品需求文档.md`
- `docs/DocuFiller技术架构文档.md`

## Expected Output

- `README.md`
- `docs/excel-data-user-guide.md`
- `docs/DocuFiller产品需求文档.md`
- `docs/DocuFiller技术架构文档.md`

## Verification

grep -ciE 'JSON|IDataParser|converter|转换|update-server|更新服务|Tools/' README.md && echo '---' && ! test -f docs/EXTERNAL_SETUP.md && ! test -f docs/VERSION_MANAGEMENT.md && ! test -f docs/deployment-guide.md
