---
estimated_steps: 5
estimated_files: 2
skills_used: []
---

# T02: 验证编译和测试，确认无残留 ExplicitChannel 引用

编译通过后运行全量测试，验证 UpdateServiceTests 和其他测试全部通过。步骤：
1. dotnet build 确认无编译错误
2. dotnet test 运行全量测试
3. 如果有测试因 ExplicitChannel 移除而失败，更新测试期望值
4. grep 确认代码中无残留的 ExplicitChannel / AllowVersionDowngrade 引用

## Inputs

- `Services/UpdateService.cs`

## Expected Output

- `测试全部通过`

## Verification

dotnet test 全部通过，无编译错误
