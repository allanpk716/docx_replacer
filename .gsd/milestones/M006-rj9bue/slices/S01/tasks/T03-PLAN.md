---
estimated_steps: 16
estimated_files: 1
skills_used: []
---

# T03: Scan real templates + implement replacement correctness tests

## Steps
1. Scan templates using `DocumentProcessorService.GetContentControlsAsync` to identify:
   - Templates with the most content controls (good for replacement correctness)
   - Templates with table content controls (SdtCell inside TableCell)
   - Templates with header/footer content controls
   - Log findings for test selection
2. Select 3-5 representative templates covering different Chapters
3. Create `Tests/E2ERegression/ReplacementCorrectnessTests.cs`:
   - Parse Excel data using ExcelDataParserService
   - Process selected templates via ServiceFactory.CreateProcessor().ProcessDocumentWithFormattedDataAsync()
   - For each template: verify output file exists, result.IsSuccess is true
   - Pick 3-5 content controls per template and verify their text matches Excel data
   - On failure: output template name, control tag, expected text, actual text
4. Create `Tests/E2ERegression/ReplacementCorrectnessTests.cs` (same file):
   - Verify that content control values are NOT the original placeholder text
   - Verify total replacement count matches expectations

## Inputs

- `test_data/2026年4月23日/LD68 IVDR.xlsx`
- `test_data/2026年4月23日/*.docx templates`
- `Services/DocumentProcessorService.cs`

## Expected Output

- `Tests/E2ERegression/ReplacementCorrectnessTests.cs`

## Verification

dotnet test Tests/E2ERegression/ --filter ReplacementCorrectness passes
