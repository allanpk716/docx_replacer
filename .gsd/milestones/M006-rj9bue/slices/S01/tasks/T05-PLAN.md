---
estimated_steps: 13
estimated_files: 2
skills_used: []
---

# T05: Implement header/footer + comment tracking tests + full verification

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

## Inputs

- `Services/ContentControlProcessor.cs`
- `Services/CommentManager.cs`
- `Services/DocumentProcessorService.cs`

## Expected Output

- `Tests/E2ERegression/HeaderFooterTests.cs`
- `Tests/E2ERegression/CommentTrackingTests.cs`

## Verification

dotnet test — all tests pass (108 existing + new E2E tests)
