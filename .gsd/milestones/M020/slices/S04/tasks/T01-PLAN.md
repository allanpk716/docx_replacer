---
estimated_steps: 11
estimated_files: 1
skills_used: []
---

# T01: Update CLAUDE.md service layer, CLI section, and DI table

Update the functional accuracy sections of CLAUDE.md:

1. **Service Layer Architecture table header**: Change "10 个服务接口 + 2 个处理器" to "11 个服务接口 + 2 个处理器"
2. **Add IUpdateService row** to the service table: `IUpdateService` / `UpdateService` / Velopack 更新管理（检查更新、下载、应用更新、源切换）
3. **Add UpdateCommand row** to CLI 组件表: `UpdateCommand` / 检查并安装应用更新（--yes 自动下载重启）
4. **Add update subcommand** to CLI 子命令表: `update` / 检查并安装应用更新 / — / `--yes`
5. **Add error codes** to 错误码表: `UPDATE_CHECK_ERROR` (更新检查失败) and `UPDATE_DOWNLOAD_ERROR` (更新下载失败)
6. **Add JSONL output types**: `update` (success, update command output: currentVersion, latestVersion, hasUpdate, isInstalled, updateSourceType) and `reminder` (success, post-command update reminder: latestVersion, message)
7. **Add update usage examples** to 使用示例 section
8. **Add IUpdateService** to DI 生命周期选择表 (Singleton)
9. **Update 非接口核心处理器 section**: Add OpenXmlHelper row for shared utility extracted from S03

All changes are to `CLAUDE.md` only.

## Inputs

- `CLAUDE.md`
- `Services/Interfaces/IUpdateService.cs`
- `Cli/Commands/UpdateCommand.cs`
- `Cli/CliRunner.cs`
- `Cli/JsonlOutput.cs`
- `Utils/OpenXmlHelper.cs`

## Expected Output

- `CLAUDE.md`

## Verification

grep -c 'IUpdateService' CLAUDE.md returns >= 3 and grep -c 'UPDATE_CHECK_ERROR' CLAUDE.md returns >= 1 and grep -c 'update' CLAUDE.md returns >= 5 and grep '11 个服务接口' CLAUDE.md succeeds
