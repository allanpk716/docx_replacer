---
estimated_steps: 23
estimated_files: 2
skills_used: []
---

# T07: Implement header/footer + comment tracking tests + final verification

## Steps

1. Create `Tests/E2ERegression/HeaderFooterTests.cs`

2. **Header/footer verification**:
   - `[Fact] LD68_HeaderControls_Replaced`
     - Process CE01 template (confirmed has header controls [H])
     - Open output, read HeaderParts
     - Find content controls in header, verify text matches Excel data
     - Verify header no longer contains old placeholder text
   - `[Fact] LD68_FooterControls_Replaced`
     - Same for footer parts in CE01
   - `[Fact] LD68_CE00_HeaderFooter_Replaced`
     - CE00 Overview also has H/F controls — verify them too

3. Create `Tests/E2ERegression/CommentTrackingTests.cs`

4. **Comment tracking verification**:
   - `[Fact] LD68_BodyComments_Added`
     - Process CE01 with LD68 data
     - Open output, check for CommentsPart
     - Verify at least some comments exist in body area
     - Comment text should reference replaced values
   - `[Fact] LD68_HeaderFooter_NoComments`
     - Verify header/footer sections do NOT contain comments
     - Only body area should have comment tracking

5. Run **full** `dotnet test` — all 108 existing + all E2E pass

## Inputs

- `Services/ContentControlProcessor.cs`
- `Services/CommentManager.cs`
- `CE01/CE00 templates (both have H/F controls)`

## Expected Output

- `Tests/E2ERegression/HeaderFooterTests.cs`
- `Tests/E2ERegression/CommentTrackingTests.cs`

## Verification

dotnet test — all tests pass (108 existing + all E2E)
