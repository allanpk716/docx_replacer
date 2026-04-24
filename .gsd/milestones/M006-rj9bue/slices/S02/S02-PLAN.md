# S02: 两列格式 + 表格/富文本/页眉页脚/批注验证

**Goal:** 基于 S01 基础设施，添加两列格式测试（FD68 IVDR.xlsx）、表格结构验证、富文本格式验证、页眉页脚替换验证、批注追踪验证。覆盖全部 5 个验证维度。
**Demo:** 运行 dotnet test --filter E2ERegression，全部 5 个验证维度通过。FD68 (两列) 和 LD68 (三列) 均验证。表格结构完整，富文本上标保留，页眉页脚替换正确，批注追踪正常。

## Must-Haves

- FD68 IVDR.xlsx (两列格式 59 关键词) 正确解析，替换后控件值与 Excel 数据匹配
- 表格结构完整：替换前后 TableRow/TableCell 数量不变
- 富文本上标保留：LD68 Excel 中 3 个上标单元格在输出文档中有 VerticalTextAlignment=Superscript
- 页眉页脚替换正确：有控件的 header/footer 文本与 Excel 数据匹配
- 批注追踪正常：正文区域添加了对应批注
- dotnet test 全部通过（108 现有 + 全部 E2E）

## Proof Level

- This slice proves: executable

## Integration Closure

复用 S01 的 ServiceFactory 和 TestDataHelper。TestDataHelper 已暴露 FD68ExcelPath 属性。

## Verification

- 表格结构断言失败时输出 before/after 行列数对比。页眉页脚断言失败时输出实际文本内容。批注断言失败时输出已找到的批注列表。

## Tasks

- [x] **T04: Implement two-column (FD68) format replacement tests** `est:45 min`
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
  - Files: `Tests/E2ERegression/TwoColumnFormatTests.cs`
  - Verify: dotnet test Tests/E2ERegression/ --filter TwoColumnFormat passes

- [x] **T05: Implement table structure verification tests** `est:30 min`
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
  - Files: `Tests/E2ERegression/TableStructureTests.cs`
  - Verify: dotnet test Tests/E2ERegression/ --filter TableStructure passes

- [x] **T06: Implement rich text format verification tests** `est:30 min`
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
  - Files: `Tests/E2ERegression/RichTextFormatTests.cs`
  - Verify: dotnet test Tests/E2ERegression/ --filter RichTextFormat passes

- [x] **T07: Implement header/footer + comment tracking tests + final verification** `est:45 min`
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
  - Files: `Tests/E2ERegression/HeaderFooterTests.cs`, `Tests/E2ERegression/CommentTrackingTests.cs`
  - Verify: dotnet test — all tests pass (108 existing + all E2E)

## Files Likely Touched

- Tests/E2ERegression/TwoColumnFormatTests.cs
- Tests/E2ERegression/TableStructureTests.cs
- Tests/E2ERegression/RichTextFormatTests.cs
- Tests/E2ERegression/HeaderFooterTests.cs
- Tests/E2ERegression/CommentTrackingTests.cs
