# S06: CLAUDE.md 删除 + 产品需求文档同步 — UAT

**Milestone:** M021
**Written:** 2026-05-04T11:54:07.015Z

# UAT: S06 — CLAUDE.md 删除 + 产品需求文档同步

## UAT Type
Documentation-only verification (no runtime behavior changes).

## Preconditions
- Project builds successfully (`dotnet build` passes)
- All prior S01–S05 slices are complete

## Test Cases

### TC-01: CLAUDE.md deleted
1. Check project root directory
2. **Expected**: `CLAUDE.md` file does not exist
3. **Evidence**: `ls CLAUDE.md` returns "No such file"

### TC-02: No stale JSON references in product doc
1. Search `docs/DocuFiller产品需求文档.md` for "JSON"
2. **Expected**: Zero matches (data config context excepted — verified zero total)
3. **Evidence**: `grep -c 'JSON' docs/DocuFiller产品需求文档.md` returns 0

### TC-03: No stale pause/resume references
1. Search product doc for "暂停"
2. **Expected**: Zero matches
3. **Evidence**: `grep -c '暂停' docs/DocuFiller产品需求文档.md` returns 0

### TC-04: StatusBar documented
1. Search product doc for "状态栏" or "StatusBar"
2. **Expected**: At least 1 match in §5.2 主界面布局
3. **Evidence**: `grep -cE 'StatusBar|状态栏' docs/DocuFiller产品需求文档.md` returns ≥1

### TC-05: Tab navigation documented
1. Search product doc for "选项卡导航" or "审核清理选项卡"
2. **Expected**: Multiple matches across §5.2 and §5.3
3. **Evidence**: `grep -cE '选项卡导航|审核清理选项卡' docs/DocuFiller产品需求文档.md` returns ≥1

### TC-06: Cleanup dual-entry-point documented
1. Read §3.5 and §5.3 of product doc
2. **Expected**: Both main window Tab and independent window entry points described
3. **Evidence**: "审核清理选项卡" and "独立清理窗口" both present

### TC-07: No incomplete markers
1. Search product doc for TBD/TODO
2. **Expected**: Zero matches
3. **Evidence**: `grep -ciE 'TBD|TODO' docs/DocuFiller产品需求文档.md` returns 0

### TC-08: Build integrity
1. Run `dotnet build DocuFiller.csproj`
2. **Expected**: 0 errors, 0 warnings
3. **Evidence**: Build output shows "0 个错误"

## Not Proven By This UAT
- Actual GUI visual fidelity (documentation accuracy vs rendered UI) — requires manual visual comparison
- Product doc completeness for future features not yet implemented

