# S06: CLAUDE.md 删除 + 产品需求文档同步

**Goal:** 删除 CLAUDE.md（D050），更新 docs/DocuFiller产品需求文档.md 的 UI 描述与 M021 重构后的实际代码一致
**Demo:** CLAUDE.md 不存在；产品需求文档 UI 描述与实际代码一致

## Must-Haves

- CLAUDE.md 文件不存在
- 产品需求文档 §5.2 主界面布局表反映实际 UI：无 JSON 引用、无暂停/恢复、包含 StatusBar（版本号、更新状态、检查更新按钮、设置齿轮）
- 产品需求文档 §3.5 / §4.3 / §5.3 描述清理功能同时存在于主窗口 Tab 和独立窗口两个入口
- grep 确认产品需求文档无 "JSON" 引用（数据配置上下文除外）、无 "暂停" 引用
- dotnet build 无错误（删除 CLAUDE.md 不影响构建，但验证完整性）

## Verification

- Run the task and slice verification checks for this slice.

## Tasks

- [ ] **T01: Delete CLAUDE.md** `est:10m`
  Delete the CLAUDE.md file from the project root per decision D050 (不再维护 CLAUDE.md, made by human, not revisable). The product requirements doc and README.md are now the sole project documentation.
  - Files: `CLAUDE.md`
  - Verify: test ! -f CLAUDE.md && echo 'CLAUDE.md deleted' || echo 'STILL EXISTS'; grep -r 'CLAUDE.md' --include='*.cs' --include='*.csproj' --include='*.md' --include='*.bat' --include='*.json' . | grep -v '.gsd/' | grep -v 'node_modules' || echo 'No CLAUDE.md references'

- [ ] **T02: Sync product requirements doc UI descriptions with actual code** `est:30m`
  Update `docs/DocuFiller产品需求文档.md` to reflect the actual UI after M021 refactoring (S01 FillVM + S02 CleanupVM + S05 auto-update). The document currently has several inaccuracies.
  - Files: `docs/DocuFiller产品需求文档.md`
  - Verify: grep -c 'JSON' docs/DocuFiller产品需求文档.md | grep -q '^0$' || echo 'WARNING: JSON references remain'; grep -c '暂停' docs/DocuFiller产品需求文档.md | grep -q '^0$' || echo 'WARNING: 暂停 references remain'; grep -c 'StatusBar\|状态栏' docs/DocuFiller产品需求文档.md | grep -q '^[1-9]' && echo 'StatusBar section present' || echo 'MISSING StatusBar'; grep -c '选项卡导航\|审核清理选项卡' docs/DocuFiller产品需求文档.md | grep -q '^[1-9]' && echo 'Tab navigation present' || echo 'MISSING Tab navigation'

## Files Likely Touched

- CLAUDE.md
- docs/DocuFiller产品需求文档.md
