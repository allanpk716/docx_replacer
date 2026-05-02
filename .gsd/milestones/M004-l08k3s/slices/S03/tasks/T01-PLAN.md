---
estimated_steps: 1
estimated_files: 5
skills_used: []
---

# T01: Clean stale test artifacts and remove dead code

Remove JSON test data file (test-data.json), update Templates/README.md and verify-templates.bat to remove JSON references, remove unused ValidateJsonFormat method from ValidationHelper.cs, and remove Newtonsoft.Json package from DocuFiller.csproj. Verify build and all 71 tests still pass.

## Inputs

- `Tests/Data/test-data.json`
- `Tests/Templates/README.md`
- `Tests/verify-templates.bat`
- `Utils/ValidationHelper.cs`
- `DocuFiller.csproj`

## Expected Output

- `Utils/ValidationHelper.cs`
- `DocuFiller.csproj`
- `Tests/Templates/README.md`
- `Tests/verify-templates.bat`

## Verification

dotnet build --no-restore && dotnet test --no-build --verbosity minimal
