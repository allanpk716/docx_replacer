---
estimated_steps: 12
estimated_files: 1
skills_used: []
---

# T05: Implement table structure verification tests

## Steps

1. Create `Tests/E2ERegression/TableStructureTests.cs`

2. **Table structure verification**:
   - `[Fact] LD68_TableStructure_Preserved`
     - Read original CE01 template, count all TableRow and TableCell elements
     - Process CE01 with LD68 data
     - Read output document, count TableRow and TableCell
     - Assert counts match
   - `[Fact] LD68_CE0601_TableStructure_Preserved`
     - Same test with CE06-01 template (also has many controls + tables)
     - This covers a different document structure
   - On failure: output template name, before/after TableRow count, before/after TableCell count

## Inputs

- `CE01 template (82 controls, has tables)`
- `CE06-01 template (49 controls, has tables)`

## Expected Output

- `Tests/E2ERegression/TableStructureTests.cs`

## Verification

dotnet test Tests/E2ERegression/ --filter TableStructure passes
