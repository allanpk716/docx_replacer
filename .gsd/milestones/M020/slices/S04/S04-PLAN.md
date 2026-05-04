# S04: CLAUDE.md 和文档准确性更新

**Goal:** 更新 CLAUDE.md 使其与当前代码库完全一致：添加 update CLI 子命令、IUpdateService 服务、完整错误码表、准确文件结构
**Demo:** CLAUDE.md 包含 update 子命令、完整错误码表、准确文件结构和 IUpdateService 服务描述

## Must-Haves

- Service Layer Architecture 表包含 11 个服务接口（含 IUpdateService）
- CLI 子命令表包含 update 命令及其参数
- 错误码表包含 UPDATE_CHECK_ERROR 和 UPDATE_DOWNLOAD_ERROR
- JSONL 输出类型表包含 update 和 reminder 类型
- 文件结构反映实际目录（Utils/OpenXmlHelper.cs、update-server/、Resources/、DocuFiller/Views/UpdateSettings*）
- DI 生命周期表包含 IUpdateService
- 所有 CLAUDE.md 中提到的文件路径在代码库中实际存在

## Verification

- Run the task and slice verification checks for this slice.

## Tasks

- [ ] **T01: Update CLAUDE.md service layer, CLI section, and DI table** `est:30m`
  Update the functional accuracy sections of CLAUDE.md:
  - Files: `CLAUDE.md`
  - Verify: grep -c 'IUpdateService' CLAUDE.md returns >= 3 and grep -c 'UPDATE_CHECK_ERROR' CLAUDE.md returns >= 1 and grep -c 'update' CLAUDE.md returns >= 5 and grep '11 个服务接口' CLAUDE.md succeeds

- [ ] **T02: Update CLAUDE.md file structure and OpenXML references** `est:30m`
  Update the structural accuracy sections of CLAUDE.md:
  - Files: `CLAUDE.md`
  - Verify: grep -c 'OpenXmlHelper' CLAUDE.md returns >= 2 and grep -c 'OpenXmlTableCellHelper' CLAUDE.md returns 0 and grep -c 'update-server' CLAUDE.md returns >= 1 and grep -c '11 个服务接口' CLAUDE.md returns >= 1 and grep -c 'UpdateCommand' CLAUDE.md returns >= 1

## Files Likely Touched

- CLAUDE.md
