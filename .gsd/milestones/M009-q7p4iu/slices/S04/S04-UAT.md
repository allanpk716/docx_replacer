# S04: CLI update 命令 + JSONL 更新提醒 — UAT

**Milestone:** M009-q7p4iu
**Written:** 2026-04-26T11:37:32.832Z

# S04: CLI update 命令 + JSONL 更新提醒 — UAT

**Milestone:** M009-q7p4iu
**Written:** 2026-04-26

## UAT Type

- UAT mode: artifact-driven
- Why this mode is sufficient: CLI output是确定性的 JSONL 格式，可通过单元测试和命令行执行直接验证。下载/重启流程依赖 Velopack 运行时和真实安装环境，在测试环境中验证错误处理路径即可。

## Preconditions

- DocuFiller.exe 已构建（dotnet build -c Release）
- CLI 模式可执行（直接运行 .exe 非 dotnet run）

## Smoke Test

运行 `DocuFiller.exe update --help`，确认输出包含 update 命令帮助信息（type=command, name=update）。

## Test Cases

### 1. update 命令帮助输出

1. 运行 `DocuFiller.exe update --help`
2. **Expected:** 输出 JSONL 行包含 `{"type":"command","name":"update",...}` 帮助信息

### 2. update 无 --yes 输出版本信息

1. 运行 `DocuFiller.exe update`
2. **Expected:** 输出 JSONL 行包含 type=update、currentVersion、latestVersion、hasUpdate、isInstalled、updateSourceType 字段

### 3. update --yes 便携版错误

1. 在便携版环境下运行 `DocuFiller.exe update --yes`
2. **Expected:** 输出 error JSONL，code=PORTABLE_NOT_SUPPORTED，message 包含"便携版不支持自动更新"

### 4. update --yes 无新版本

1. 在已是最新版本的安装版环境下运行 `DocuFiller.exe update --yes`
2. **Expected:** 输出 summary 行"当前已是最新版本"，exit code 0

### 5. post-command 更新提醒（有新版本）

1. 在有新版本可用时运行 `DocuFiller.exe inspect --template some.docx`（成功场景）
2. **Expected:** inspect 正常输出后，末尾追加 type=update JSONL 行（reminder=true, latestVersion 字段存在）

### 6. post-command 无更新时不追加

1. 在已是最新版本时运行 `DocuFiller.exe inspect --template some.docx`（成功场景）
2. **Expected:** 只有 inspect 正常输出，末尾无 type=update 行

### 7. post-command 失败时不追加

1. 在有新版本但 inspect 命令执行失败时（例如模板文件不存在）
2. **Expected:** 只有 error 输出，无 type=update 行

### 8. 全局帮助包含 update 命令

1. 运行 `DocuFiller.exe --help`
2. **Expected:** 输出中包含 update 命令描述

## Edge Cases

### IUpdateService 未注册

1. 在 IUpdateService 未注入的环境中运行任意 CLI 命令
2. **Expected:** 命令正常执行，不报错，不追加 update 行

### 更新检查网络异常

1. 在无法访问更新服务器时运行 `DocuFiller.exe update`
2. **Expected:** 输出 error JSONL，code=UPDATE_CHECK_ERROR，exit code 非 0

### update 子命令不双重输出

1. 运行 `DocuFiller.exe update`
2. **Expected:** 只输出 update 命令自身的 JSONL，不会因 post-command hook 再追加一条 update 行

## Failure Signals

- dotnet build 出现编译错误
- dotnet test 新增或已有测试失败
- update --help 输出不包含 update 命令
- post-command hook 抛出未处理异常导致原命令 exitCode 被改变

## Not Proven By This UAT

- --yes 下载和重启流程（需要真实安装版 + 新版本 Release 环境）
- 进度回调 JSONL 在真实下载中的表现
- CLI 其他命令（fill/cleanup）的 post-command 提醒（测试使用 inspect 命令验证）

## Notes for Tester

- CLI 输出使用 AttachConsole(-1) P/Invoke，`dotnet run` 可能无法正确捕获 stdout，应直接运行构建产物 .exe
- 测试使用便携版时可验证 PORTABLE_NOT_SUPPORTED 错误路径
- post-command 提醒的设计意图是"不干扰 JSONL 解析器"，确保仅在命令成功且有新版本时追加
