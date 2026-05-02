---
estimated_steps: 19
estimated_files: 3
skills_used: []
---

# T02: Implement ServiceFactory + TestDataHelper + smoke tests

## Steps
1. Create `Tests/E2ERegression/ServiceFactory.cs`:
   - Use `ServiceCollection` DI container
   - Register all known services (IFileService, IExcelDataParser, ContentControlProcessor, CommentManager, ISafeTextReplacer, ISafeFormattedContentReplacer, IProgressReporter, etc.)
   - Conditional IDataParser registration via `AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes()).FirstOrDefault(t => t.Name == "IDataParser")`
   - Register `IServiceProvider` as self
   - Register `IDocumentProcessor` → `DocumentProcessorService` last
   - Include `CreateProcessor()` method returning fully constructed `DocumentProcessorService`
2. Create `Tests/E2ERegression/TestDataHelper.cs`:
   - Navigate up from assembly location to find `test_data/2026年4月23日/` directory
   - Also support finding via .sln file as anchor
   - Expose properties: TestDataDirectory, ExcelFilePath, TemplateDirectory
   - Method to list available docx templates recursively
3. Write smoke test: `ServiceFactory_CreatesProcessor_Successfully`
   - Verify DI container builds without error
   - Verify `DocumentProcessorService` instance is not null
4. Write test data discovery test: `TestDataHelper_FindsRealData_Successfully`
   - Verify Excel file exists and can be parsed
   - Verify at least 3 docx templates are found

## Inputs

- `Services/DocumentProcessorService.cs (constructor signature)`
- `App.xaml.cs (DI registration pattern)`
- `Services/Interfaces/IExcelDataParser.cs`

## Expected Output

- `Tests/E2ERegression/ServiceFactory.cs`
- `Tests/E2ERegression/TestDataHelper.cs`
- `Tests/E2ERegression/InfrastructureTests.cs`

## Verification

dotnet test Tests/E2ERegression/ --filter ServiceFactory|TestDataHelper passes
