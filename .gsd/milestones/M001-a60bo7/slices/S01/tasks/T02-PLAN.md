---
estimated_steps: 1
estimated_files: 2
skills_used: []
---

# T02: Add ID uniqueness validation and basic 3-column tests

Modify ValidateExcelFileAsync to: (1) use DetectExcelFormat to determine format, (2) in 3-column mode track IDs from column 1 in a HashSet, (3) detect duplicates and populate ExcelFileSummary.DuplicateRowIds + add errors to ExcelValidationResult, (4) validate keywords from column 2 and values from column 3. Then add inline xunit tests in ExcelDataParserServiceTests.cs: test 3-col parsing returns correct keyword-value pairs (ID column skipped), test format detection distinguishes 2-col vs 3-col, test ID duplicates produce validation errors with specific IDs. Verify all existing tests still pass.

## Inputs

- `Services/ExcelDataParserService.cs`
- `Tests/ExcelDataParserServiceTests.cs`
- `Models/ExcelFileSummary.cs`
- `Models/ExcelValidationResult.cs`
- `Models/FormattedCellValue.cs`
- `Services/Interfaces/IExcelDataParser.cs`
- `Services/Interfaces/IFileService.cs`
- `Services/FileService.cs`

## Expected Output

- `Services/ExcelDataParserService.cs`
- `Tests/ExcelDataParserServiceTests.cs`

## Verification

dotnet test --filter "FullyQualifiedName~ExcelDataParserServiceTests"
