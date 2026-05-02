---
id: T01
parent: S03
milestone: M006-rj9bue
key_files:
  - Tests/E2ERegression/E2ERegression.csproj
key_decisions:
  - Added Exclude on wildcard *.cs include to prevent NETSDK1022 duplicate compile when IDataParser.cs exists (d81cd00)
duration: 
verification_result: passed
completed_at: 2026-04-24T00:31:31.580Z
blocker_discovered: false
---

# T01: Fix E2E csproj duplicate compile item and verify build+tests pass on d81cd00 baseline (25/27) and current branch (27/27)

**Fix E2E csproj duplicate compile item and verify build+tests pass on d81cd00 baseline (25/27) and current branch (27/27)**

## What Happened

Executed d81cd00 baseline cross-version verification for the E2E regression test project. The plan assumed checking out d81cd00 and building E2E tests there, but the E2E project was created after d81cd00 (in commits 82177a8 and 98a59b9). Adapted by copying E2E test sources onto the d81cd00 checkout.

During the d81cd00 build, discovered a NETSDK1022 duplicate compile item error: `IDataParser.cs` was included by both the conditional `<Compile Include="...IDataParser.cs" Condition="Exists(...)">` and the wildcard `<Compile Include="..\..\Services\Interfaces\*.cs">`. Fixed by adding `Exclude="..\..\Services\Interfaces\IDataParser.cs"` to the wildcard include. This fix is needed for cross-version compatibility — when IDataParser.cs exists (d81cd00), the conditional include activates and the wildcard would double-include it.

**d81cd00 results:** Build succeeded (0 errors, 23 warnings). 25 of 27 tests passed. The 2 failures (`ExcelParsing_LD68_ThreeColumnFormat` and `ExcelParsing_BothFormats_HaveCommonKeywords`) are expected — d81cd00's ExcelDataParserService only handles 2-column format, while LD68 uses 3-column (ID|keyword|value). All 7 replacement correctness tests and all other infrastructure/format tests passed, confirming the document processing pipeline is fully compatible across versions.

**Current branch results:** Build succeeded. All 27 tests passed (0 failures), including the 3-column parsing tests that use the updated ExcelDataParserService.

## Verification

Verified d81cd00 baseline build and test execution:
1. `dotnet build Tests/E2ERegression/E2ERegression.csproj` on d81cd00 — succeeded (0 errors)
2. `dotnet test` on d81cd00 — 25 passed, 2 failed (expected: 3-column parsing not supported on d81cd00)
3. `dotnet build` on current branch — succeeded (0 errors)
4. `dotnet test` on current branch — 27 passed, 0 failed

Key observation: ServiceFactory's conditional IDataParser registration via FindType() correctly adapts at runtime. On d81cd00, IDataParser types are found and registered; on current branch, they're absent and skipped. The DI container auto-resolves the 9-param constructor (d81cd00) vs 8-param constructor (current) seamlessly.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet build Tests/E2ERegression/E2ERegression.csproj (d81cd00)` | 0 | ✅ pass | 2010ms |
| 2 | `dotnet test Tests/E2ERegression/E2ERegression.csproj --no-build --verbosity normal (d81cd00)` | 1 | ⚠️ 25/27 pass (2 expected failures) | 4398ms |
| 3 | `dotnet build Tests/E2ERegression/E2ERegression.csproj (current branch)` | 0 | ✅ pass | 1390ms |
| 4 | `dotnet test Tests/E2ERegression/E2ERegression.csproj --no-build --verbosity normal (current branch)` | 0 | ✅ pass (27/27) | 5808ms |

## Deviations

E2E test project didn't exist on d81cd00 (created in 82177a8). Adapted by copying E2E sources onto d81cd00 checkout instead of expecting them to be there. Also discovered and fixed a NETSDK1022 duplicate compile item error in csproj where IDataParser.cs was double-included by both conditional and wildcard Compile items.

## Known Issues

2 infrastructure tests (ExcelParsing_LD68_ThreeColumnFormat, ExcelParsing_BothFormats_HaveCommonKeywords) fail on d81cd00 because that version's ExcelDataParserService doesn't support 3-column format. This is expected behavior — the replacement correctness tests (7/7) and format tests (all pass) confirm document processing compatibility.

## Files Created/Modified

- `Tests/E2ERegression/E2ERegression.csproj`
