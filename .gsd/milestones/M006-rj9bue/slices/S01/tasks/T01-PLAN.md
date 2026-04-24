---
estimated_steps: 6
estimated_files: 2
skills_used: []
---

# T01: Create E2E test project scaffold

## Steps
1. Create `Tests/E2ERegression/E2ERegression.csproj` with xUnit, DocumentFormat.OpenXml, EPPlus, Microsoft.Extensions.DependencyInjection/Logging packages
2. Use source file linking pattern (same as existing DocuFiller.Tests.csproj) for core services and models
3. Add conditional `<Compile Include>` for deleted files: DataParserService.cs, IDataParser.cs with `Condition="Exists(...)"`
4. Add the project to DocuFiller.sln
5. Run `dotnet build Tests/E2ERegression/` — must succeed with 0 errors

## Inputs

- `Tests/DocuFiller.Tests.csproj (pattern reference)`
- `DocuFiller.sln`

## Expected Output

- `Tests/E2ERegression/E2ERegression.csproj`
- `Updated DocuFiller.sln`

## Verification

dotnet build Tests/E2ERegression/ succeeds with 0 errors
