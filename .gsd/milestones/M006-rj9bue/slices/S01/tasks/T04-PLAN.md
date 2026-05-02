---
estimated_steps: 14
estimated_files: 2
skills_used: []
---

# T04: Implement table structure + rich text format verification tests

## Steps
1. Create `Tests/E2ERegression/TableStructureTests.cs`:
   - Process a template with table content controls
   - Before processing: count TableRow and TableCell elements in original template
   - After processing: count TableRow and TableCell in output document
   - Assert counts are equal (table structure not destroyed)
   - On failure: output before/after counts per table
2. Create `Tests/E2ERegression/RichTextFormatTests.cs`:
   - Process a template with formatted data (superscript/subscript)
   - Verify VerticalTextAlignment elements exist with correct values (Superscript/Subscript)
   - Scan output for Run elements with VerticalTextAlignment
   - Verify at least some formatting is preserved (the Excel data may contain formatted cells)
   - If no rich text found in real data, verify the mechanism works with manually crafted FormattedCellValue
3. Both test classes use ServiceFactory and TestDataHelper from T02

## Inputs

- `Services/SafeTextReplacer.cs`
- `Services/SafeFormattedContentReplacer.cs`
- `Utils/OpenXmlTableCellHelper.cs`

## Expected Output

- `Tests/E2ERegression/TableStructureTests.cs`
- `Tests/E2ERegression/RichTextFormatTests.cs`

## Verification

dotnet test Tests/E2ERegression/ --filter TableStructure|RichTextFormat passes
