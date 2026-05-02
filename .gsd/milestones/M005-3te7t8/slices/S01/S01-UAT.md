# S01: CLI 框架 + inspect 子命令 — UAT

**Milestone:** M005-3te7t8
**Written:** 2026-04-23T16:00:39.520Z

# UAT: S01 — CLI 框架 + inspect 子命令

## 前提条件

- Windows 10/11 环境
- 已构建项目：`dotnet build -c Debug`
- 有一个包含内容控件的 .docx 测试模板

## 测试用例

### TC-01: --help 全局帮助

**步骤：**
1. 打开 cmd 或 PowerShell
2. 执行 `DocuFiller.exe --help`

**预期结果：**
- 输出 5 行 JSONL
- 第 1 行：`{"type":"help","name":"DocuFiller",...,"description":"Word文档批量填充工具"}`
- 第 2-4 行：三个子命令（fill、cleanup、inspect），每个包含 name、description、usage、options
- 第 5 行：`{"type":"examples","items":[...]}`
- 每行可被 `System.Text.Json.JsonSerializer.Deserialize<>` 解析
- Exit code = 0

### TC-02: --help 别名 -h

**步骤：**
1. 执行 `DocuFiller.exe -h`

**预期结果：** 与 TC-01 完全一致

### TC-03: 子命令级 --help

**步骤：**
1. 执行 `DocuFiller.exe inspect --help`

**预期结果：**
- 输出 1 行 JSONL
- 包含 `"type":"command","name":"inspect","options":[{"name":"--template","required":true,...}]`
- Exit code = 0

### TC-04: inspect 正常执行

**步骤：**
1. 准备一个包含内容控件的 .docx 文件
2. 执行 `DocuFiller.exe inspect --template <path>`

**预期结果：**
- 每个内容控件输出 1 行 `{"type":"control","status":"success","data":{"tag":"...","title":"...","contentType":"...","location":"..."}}`
- 最后 1 行汇总：`{"type":"summary","status":"success","data":{"totalControls":N}}`
- 每行包含有效的 ISO 8601 timestamp
- Exit code = 0

### TC-05: inspect 缺少 --template

**步骤：**
1. 执行 `DocuFiller.exe inspect`

**预期结果：**
- 输出 1 行 `{"type":"error","status":"error","data":{"message":"...--template...","code":"MISSING_ARGUMENT"}}`
- Exit code = 1

### TC-06: inspect 文件不存在

**步骤：**
1. 执行 `DocuFiller.exe inspect --template nonexistent.docx`

**预期结果：**
- 输出错误 JSONL，code 为 `FILE_NOT_FOUND`
- Exit code = 1

### TC-07: 未知子命令

**步骤：**
1. 执行 `DocuFiller.exe foobar`

**预期结果：**
- 输出错误 JSONL，code 为 `UNKNOWN_COMMAND`
- Exit code = 1

### TC-08: 未知全局参数

**步骤：**
1. 执行 `DocuFiller.exe --bogus`

**预期结果：**
- 输出错误 JSONL，code 为 `UNKNOWN_ARGUMENT`
- Exit code = 1

### TC-09: --version

**步骤：**
1. 执行 `DocuFiller.exe --version`

**预期结果：**
- 输出 `{"type":"version","version":"..."}`
- Exit code = 0

### TC-10: 无参数启动 GUI

**步骤：**
1. 双击 DocuFiller.exe（或在文件管理器中直接运行）

**预期结果：**
- WPF GUI 窗口正常显示
- 不弹出控制台窗口
- 不输出控制台文本

### TC-11: JSONL 格式严格验证

**步骤：**
1. 执行 `DocuFiller.exe --help > help.txt`
2. 对 help.txt 每行运行 JSON 解析器

**预期结果：**
- 每行都是合法 JSON
- 无多余空行
- 无非 JSON 输出（如 logger 调试信息）

### TC-12: ISO 8601 timestamp 验证

**步骤：**
1. 执行 `DocuFiller.exe inspect --template <path> 2>&1`
2. 提取每行的 `timestamp` 字段
3. 验证符合 ISO 8601 格式（包含 T 分隔符和时区偏移）

**预期结果：**
- 所有 timestamp 均有效
- 格式如 `2026-04-23T15:59:12.1287035+00:00`
