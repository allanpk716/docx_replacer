# S01: E2E 测试基础设施 + 全维度验证测试

**Goal:** 创建独立 xUnit E2E 回归测试项目，实现版本兼容 ServiceFactory（DI + 条件类型注册），编写覆盖替换正确性、表格结构、富文本格式、页眉页脚、批注追踪 5 个维度的测试用例，在当前代码上全部通过。
**Demo:** 运行 dotnet test --filter E2ERegression，所有新增测试通过。用 LD68 IVDR.xlsx + 3-5 个代表性模板验证输出文档正确性。

## Must-Haves

- E2E 测试项目编译通过并添加到解决方案\nServiceFactory 成功构建 DocumentProcessorService 并执行替换\n至少 3 个不同 Chapter 的模板替换成功\n5 个验证维度各有至少 1 个测试用例通过\ndotnet test 全部通过（108 现有 + 新增 E2E）

## Proof Level

- This slice proves: executable

## Integration Closure

ServiceFactory 和 TestDataHelper 作为共享基础设施供所有测试类使用。E2E 测试项目独立于现有测试项目，通过源文件链接引用主项目代码。

## Verification

- 测试失败时输出详细诊断信息（哪个模板、哪个字段、期望值 vs 实际值）。ServiceFactory 在无法解析服务类型时抛出清晰的异常。

## Tasks

- [ ] **T01: Create E2E test project scaffold** `est:30 min`
  ## Steps
1. Create `Tests/E2ERegression/E2ERegression.csproj` with xUnit, DocumentFormat.OpenXml, EPPlus, Microsoft.Extensions.DependencyInjection/Logging packages
2. Use source file linking pattern (same as existing DocuFiller.Tests.csproj) for core services and models
3. Add conditional `<Compile Include>` for deleted files: DataParserService.cs, IDataParser.cs with `Condition="Exists(...)"`
4. Add the project to DocuFiller.sln
5. Run `dotnet build Tests/E2ERegression/` — must succeed with 0 errors
  - Files: `Tests/E2ERegression/E2ERegression.csproj`, `DocuFiller.sln`
  - Verify: dotnet build Tests/E2ERegression/ succeeds with 0 errors

- [ ] **T02: Implement ServiceFactory + TestDataHelper + smoke tests** `est:45 min`
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
  - Files: `Tests/E2ERegression/ServiceFactory.cs`, `Tests/E2ERegression/TestDataHelper.cs`, `Tests/E2ERegression/InfrastructureTests.cs`
  - Verify: dotnet test Tests/E2ERegression/ --filter ServiceFactory|TestDataHelper passes

- [ ] **T03: Scan real templates + implement replacement correctness tests** `est:45 min`
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
  - Files: `Tests/E2ERegression/ReplacementCorrectnessTests.cs`
  - Verify: dotnet test Tests/E2ERegression/ --filter ReplacementCorrectness passes

- [ ] **T04: Implement table structure + rich text format verification tests** `est:45 min`
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
  - Files: `Tests/E2ERegression/TableStructureTests.cs`, `Tests/E2ERegression/RichTextFormatTests.cs`
  - Verify: dotnet test Tests/E2ERegression/ --filter TableStructure|RichTextFormat passes

- [ ] **T05: Implement header/footer + comment tracking tests + full verification** `est:45 min`
  ## Steps
1. Create `Tests/E2ERegression/HeaderFooterTests.cs`:
   - Process a template with header/footer content controls (identify via scan in T03)
   - Verify header/footer parts contain correct replaced text
   - Compare original header/footer text with output text
   - Verify no old placeholder text remains in headers/footers
2. Create `Tests/E2ERegression/CommentTrackingTests.cs`:
   - Process a template and verify body-area comments are added
   - Open output document and check for CommentsPart
   - Verify comment text references the replaced content
   - Verify comments are NOT added in header/footer areas (only body)
3. Run full `dotnet test` to verify all tests pass (108 existing + new E2E)
4. Verify no regressions in existing tests
  - Files: `Tests/E2ERegression/HeaderFooterTests.cs`, `Tests/E2ERegression/CommentTrackingTests.cs`
  - Verify: dotnet test — all tests pass (108 existing + new E2E tests)

## Files Likely Touched

- Tests/E2ERegression/E2ERegression.csproj
- DocuFiller.sln
- Tests/E2ERegression/ServiceFactory.cs
- Tests/E2ERegression/TestDataHelper.cs
- Tests/E2ERegression/InfrastructureTests.cs
- Tests/E2ERegression/ReplacementCorrectnessTests.cs
- Tests/E2ERegression/TableStructureTests.cs
- Tests/E2ERegression/RichTextFormatTests.cs
- Tests/E2ERegression/HeaderFooterTests.cs
- Tests/E2ERegression/CommentTrackingTests.cs
