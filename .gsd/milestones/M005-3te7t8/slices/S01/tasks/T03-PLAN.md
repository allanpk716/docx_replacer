---
estimated_steps: 23
estimated_files: 2
skills_used: []
---

# T03: 实现 inspect 子命令

实现 inspect 子命令处理器：

**Cli/Commands/InspectCommand.cs**

```
async Task<int> ExecuteAsync(string[] args)
```

参数：
- `--template <path>` (必需) — 模板文件路径
- `--help` — 显示 inspect 子命令帮助

执行流程：
1. 验证 --template 参数存在且文件存在
2. 通过 DI 解析 IDocumentProcessor
3. 调用 GetContentControlsAsync(templatePath)
4. 对每个 ContentControlData 输出一行 JSONL：
   ```jsonl
   {"type":"control","tag":"...","title":"...","contentType":"Text","location":"Body"}
   ```
5. 最后输出汇总行：
   ```jsonl
   {"type":"summary","status":"success","totalControls":5}
   ```
6. 文件不存在或参数缺失时输出错误 JSONL 并返回 exit code 1

注册为命令名 `inspect`，在 CliRunner 中通过 ServiceProvider 解析。

ICommand 接口：`interface ICommand { string Name { get; } Task<int> ExecuteAsync(string[] args); }`

## Inputs

- `Services/Interfaces/IDocumentProcessor.cs`
- `Models/ContentControlData.cs`
- `Cli/JsonlOutput.cs`

## Expected Output

- `Cli/Commands/ICommand.cs`
- `Cli/Commands/InspectCommand.cs`

## Verification

使用测试模板执行 DocuFiller.exe inspect --template <path> 输出正确 JSONL 控件列表
