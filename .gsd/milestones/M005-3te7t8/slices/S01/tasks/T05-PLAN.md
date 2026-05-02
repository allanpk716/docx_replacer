---
estimated_steps: 10
estimated_files: 1
skills_used: []
---

# T05: 端到端验证：CLI --help + inspect + GUI 回归

执行完整的端到端验证：

1. `dotnet build` — 编译成功（0 错误）
2. `dotnet test` — 所有 71 个现有测试通过
3. 准备一个含内容控件的测试 .docx 文件（在测试中已有模板可复用）
4. 执行 `DocuFiller.exe --help` — 验证输出为 JSONL 格式，每行可被 System.Text.Json 解析，包含三个子命令
5. 执行 `DocuFiller.exe inspect --template <测试模板>` — 验证输出 JSONL 包含正确的控件 tag/title/type/location
6. 执行 `DocuFiller.exe inspect` — 验证输出错误 JSONL（缺少 --template），exit code 1
7. 执行 `DocuFiller.exe --unknown-cmd` — 验证输出错误 JSONL（未知命令），exit code 1
8. 无参数启动 — 验证 GUI 正常弹窗（无控制台闪屏）
9. 验证所有 JSONL 输出行的 timestamp 字段为有效 ISO 8601 格式

## Inputs

- `所有 S01 产出文件`

## Expected Output

- `构建成功，测试通过，CLI 端到端验证通过`

## Verification

上述 9 项验证全部通过
