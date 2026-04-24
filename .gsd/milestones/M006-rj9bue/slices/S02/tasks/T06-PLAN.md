---
estimated_steps: 15
estimated_files: 1
skills_used: []
---

# T06: Implement rich text format verification tests

## Steps

1. Create `Tests/E2ERegression/RichTextFormatTests.cs`

2. **Rich text verification (LD68 has 3 superscript cells)**:
   - `[Fact] LD68_RichText_Superscript_Preserved`
     - Process CE01 template with LD68 data (which has 3 superscript cells)
     - Scan output document for Run elements with VerticalTextAlignment
     - Assert at least 1 Run has VerticalPositionValues.Superscript
     - Verify the surrounding text matches known superscript content (e.g., `10^9` pattern)
   - `[Fact] LD68_RichText_SpecificValues`
     - Known superscript content in LD68: `Absorption peak wavelength: 634±10`, appearance section, `WBC count ≥ 0.2×10^9/L`
     - Find the `×109` or similar pattern in output
     - Verify the `9` (or `10`) has superscript formatting
   - `[Fact] FD68_NoRichText_PlainValuesCorrect`
     - FD68 has no rich text — verify all values are plain text
     - No VerticalTextAlignment elements in output for FD68-specific keywords

## Inputs

- `LD68 IVDR.xlsx (3 superscript cells)`
- `CE01 template`

## Expected Output

- `Tests/E2ERegression/RichTextFormatTests.cs`

## Verification

dotnet test Tests/E2ERegression/ --filter RichTextFormat passes
