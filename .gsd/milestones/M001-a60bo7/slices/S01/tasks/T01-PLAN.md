---
estimated_steps: 1
estimated_files: 2
skills_used: []
---

# T01: Add DuplicateRowIds model field and 3-column format detection + parsing

Add DuplicateRowIds field to ExcelFileSummary model. Add private DetectExcelFormat method to ExcelDataParserService that reads the first non-empty row's first column — if it matches #xxx# it's 2-column mode, otherwise 3-column. Modify ParseExcelFileAsync to use the detected format: in 3-column mode read col 2 as keyword and col 3 as value, skipping col 1 (ID). Ensure 2-column mode behavior is unchanged.

## Inputs

- `Models/ExcelFileSummary.cs`
- `Services/ExcelDataParserService.cs`
- `Services/Interfaces/IExcelDataParser.cs`
- `Models/ExcelValidationResult.cs`
- `Models/FormattedCellValue.cs`
- `Models/TextFragment.cs`

## Expected Output

- `Models/ExcelFileSummary.cs`
- `Services/ExcelDataParserService.cs`

## Verification

dotnet test --filter "ExcelDataParserServiceTests" --no-build does not exist yet; instead run: dotnet build && dotnet test --filter "FullyQualifiedName~ExcelDataParserServiceTests"
