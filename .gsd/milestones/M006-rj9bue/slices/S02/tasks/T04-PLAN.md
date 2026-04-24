---
estimated_steps: 22
estimated_files: 1
skills_used: []
---

# T04: Implement two-column (FD68) format replacement tests

## Steps

1. Create `Tests/E2ERegression/TwoColumnFormatTests.cs`

2. **Two-column format test (FD68)**:
   - `[Fact] FD68_TwoColumn_Replacement_Succeeds`
     - Parse `FD68 IVDR.xlsx` (两列格式, 59 关键词)
     - Process CE01 template (82 controls) with FD68 data
     - Assert result.IsSuccess
   - `[Theory] FD68_TwoColumn_KeywordsMatchData`
     - Process CE01 with FD68 data
     - Spot-check common keywords (43 shared between LD68/FD68):
       - `#产品名称#`→`Fluorescent Dye`, `#产品型号#`→`BH-FD68`, `#Basic UDI-DI#`→`69357407IBHS000017ED`
     - Verify replaced values differ from LD68 values (prove correct data source used)
   - `[Fact] FD68_TwoColumn_AnotherTemplate`
     - Process CE06-01 (49 controls) with FD68 data
     - Verify success and spot-check keywords

3. **Cross-format comparison**:
   - `[Fact] SameTemplate_DifferentDataSources_ProduceDifferentOutput`
     - Process CE01 with LD68 data → output A
     - Process CE01 with FD68 data → output B
     - Find a keyword like `#产品名称#` in both outputs
     - Assert A has `Lyse` and B has `Fluorescent Dye`
     - This proves the parser correctly distinguishes two-column vs three-column

## Inputs

- `test_data/2026年4月23日/FD68 IVDR.xlsx`
- `CE01/CE06-01 templates`

## Expected Output

- `Tests/E2ERegression/TwoColumnFormatTests.cs`

## Verification

dotnet test Tests/E2ERegression/ --filter TwoColumnFormat passes
