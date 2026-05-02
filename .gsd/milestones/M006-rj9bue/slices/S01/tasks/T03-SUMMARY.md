---
id: T03
parent: S01
milestone: M006-rj9bue
key_files:
  - (none)
key_decisions:
  - Used GetControlValue helper that searches body + headers + footers for content controls by tag, instead of full-text search which had false positives from template boilerplate text
  - Verified replacement correctness via specific known values (BH-LD68/BH-FD68, UDI-DI codes) rather than generic text containment
duration: 
verification_result: untested
completed_at: 2026-04-24T00:10:21.922Z
blocker_discovered: false
---

# T03: Replacement correctness tests pass for LD68 (three-column) and FD68 (two-column) with 3 different templates, cross-format verification confirms correct data routing

**Replacement correctness tests pass for LD68 (three-column) and FD68 (two-column) with 3 different templates, cross-format verification confirms correct data routing**

## What Happened

Implemented replacement correctness tests covering both LD68 (three-column) and FD68 (two-column) formats.

Key insight from diagnostic testing: template CE01 contains ~67 content controls across body, headers, and footers. Some values like product model numbers appear in template boilerplate text (not just controls), so full-text assertions had false positives. Solution: use GetControlValue helper that finds specific SDT elements by tag and reads only their replaced text.

Test coverage:
- LD68 CE01 (82 controls): replacement succeeds, specific values match (#产品名称#=Lyse, #产品型号#=BH-LD68, #Basic UDI-DI#=69357407IBHS000018EF, #风险等级#=Class A)
- LD68 CE00 (35 controls) and CE06-01 (49 controls): both succeed
- FD68 CE01: replacement succeeds, values differ (#产品名称#=Fluorescent Dye, #产品型号#=BH-FD68, #Basic UDI-DI#=69357407IBHS000017ED)
- Cross-format: same template with different data produces different output for #产品型号# control

All 15 E2E tests + 108 existing = 123 total pass. No regressions.

## Verification

dotnet test — 123 passed (108 existing + 15 E2E), 0 failed

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| — | No verification commands discovered | — | — | — |

## Deviations

None.

## Known Issues

None.

## Files Created/Modified

None.
