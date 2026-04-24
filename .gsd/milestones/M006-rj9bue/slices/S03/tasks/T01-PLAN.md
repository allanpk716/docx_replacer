---
estimated_steps: 23
estimated_files: 3
skills_used: []
---

# T01: 在 d81cd00 基准版本上构建并运行 E2E 测试

Checkout d81cd00 → 构建 E2E 测试项目 → 运行 dotnet test → 所有 E2E 测试通过。

## Steps

1. `git checkout --detach d81cd00` — 将工作树切换到基准版本
2. `dotnet restore Tests/E2ERegression/E2ERegression.csproj` — 恢复 E2E 项目依赖
3. `dotnet build Tests/E2ERegression/E2ERegression.csproj` — 构建项目（注意：用 csproj 而非 sln，因为 d81cd00 的 sln 不包含 E2E 项目）
4. `dotnet test Tests/E2ERegression/E2ERegression.csproj --no-build --verbosity normal` — 运行所有 E2E 测试
5. 确认所有 15 个 E2E 测试通过（8 基础设施 + 7 替换正确性）
6. 如果构建失败，检查是否因缺少类型/接口。ServiceFactory 通过 DI 条件注册 IDataParser，csproj 通过 Condition="Exists(...)" 条件包含已删除文件——两者在 d81cd00 上都会激活（因为 DataParserService.cs 和 IDataParser.cs 文件存在）
7. 如果测试失败，分析输出定位具体失败的测试和原因

## Must-Haves

- [ ] d81cd00 上 dotnet build E2E 项目成功
- [ ] d81cd00 上所有 15 个 E2E 测试通过
- [ ] ServiceFactory 的条件 IDataParser 注册在 d81cd00 上正常工作（9 参数构造函数）

## Verification

- `dotnet test Tests/E2ERegression/E2ERegression.csproj --no-build --verbosity normal` 返回 0 exit code，15 passed, 0 failed

## Inputs

- `Tests/E2ERegression/E2ERegression.csproj` — E2E 项目文件，含条件源文件链接
- `Tests/E2ERegression/ServiceFactory.cs` — DI 服务工厂，条件注册 IDataParser
- `Tests/E2ERegression/TestDataHelper.cs` — 测试数据路径发现

## Expected Output

- `Tests/E2ERegression/E2ERegression.csproj` — 验证通过（无修改）

## Observability Impact

- 诊断信号：dotnet test 的详细输出（--verbosity normal）显示每个测试的名称、耗时和通过/失败状态。如果失败，输出包含异常信息和堆栈跟踪，可定位到具体的版本兼容问题。

## Inputs

- `Tests/E2ERegression/E2ERegression.csproj`
- `Tests/E2ERegression/ServiceFactory.cs`
- `Tests/E2ERegression/TestDataHelper.cs`

## Expected Output

- `Tests/E2ERegression/E2ERegression.csproj`

## Verification

dotnet test Tests/E2ERegression/E2ERegression.csproj --no-build --verbosity normal
