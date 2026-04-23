---
estimated_steps: 1
estimated_files: 1
skills_used: []
---

# T02: Rewrite CLAUDE.md to reflect Excel-only clean codebase

Rewrite CLAUDE.md to remove all references to removed modules: IDataParser/DataParserService, IExcelToWordConverter/ExcelToWordConverterService, IJsonEditorService, IUpdateService/UpdateClientService, ConverterWindow, Tools/ directory, External/ directory, Update/ subdirectories, JsonKeywordItem/JsonProjectModel models, JSON data structure section. Update service layer table to only show 12 active services (down from 15). Update DI lifecycle table to remove IDataParser. Update data models table to remove JSON-related models. Update file structure to remove Tools/, External/, Update/ subdirectories. Update processing pipeline to show Excel-only. Remove JSON Data Structure section entirely.

## Inputs

- `CLAUDE.md`

## Expected Output

- `CLAUDE.md`

## Verification

grep -ciE 'IDataParser|DataParserService|IExcelToWordConverter|ExcelToWordConverterService|IJsonEditorService|IUpdateService|UpdateClientService|ConverterWindow|Tools/|External/|JsonKeywordItem|JsonProjectModel|JSON Data Structure' CLAUDE.md
