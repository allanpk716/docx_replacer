# S03: d81cd00 基准跨版本验证

**Goal:** 验证 E2E 回归测试在 d81cd00 基准版本（含 IDataParser，9 参数构造函数）和当前里程碑分支（不含 IDataParser，8 参数构造函数）上均能构建通过并全部测试通过，证明跨版本兼容性。
**Demo:** Checkout d81cd00 → 构建 E2E 测试 → 测试通过 → 切回里程碑分支 → 测试仍通过。

## Must-Haves

- ## Success Criteria
- [ ] d81cd00 版本上 E2E 测试项目构建成功（ServiceFactory 正确注册 IDataParser）
- [ ] d81cd00 版本上所有 15 个 E2E 测试通过
- [ ] 切回 milestone/M006-rj9bue 后，全部 123 个测试通过（108 现有 + 15 E2E）
- [ ] 跨版本验证过程中无代码修改，仅通过 git checkout 切换源文件

## Proof Level

- This slice proves: - This slice proves: integration — the E2E test infrastructure works across two code versions with different constructor signatures
- Real runtime required: yes — actual dotnet build and test execution on both versions
- Human/UAT required: no

## Integration Closure

- Upstream surfaces consumed: S01 E2E test project (ServiceFactory, TestDataHelper, all test files), d81cd00 source tree, test_data/2026年4月23日/
- New wiring introduced in this slice: none — this is pure verification, no code changes
- What remains before the milestone is truly usable end-to-end: nothing — this is the final verification slice

## Verification

- Not provided.

## Tasks

- [x] **T01: 在 d81cd00 基准版本上构建并运行 E2E 测试** `est:30m`
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
  - Files: `Tests/E2ERegression/E2ERegression.csproj`, `Tests/E2ERegression/ServiceFactory.cs`, `Tests/E2ERegression/TestDataHelper.cs`
  - Verify: dotnet test Tests/E2ERegression/E2ERegression.csproj --no-build --verbosity normal

- [x] **T02: 切回里程碑分支并验证测试仍通过** `est:15m`
  从 d81cd00 切回 milestone/M006-rj9bue 分支 → 构建全部项目 → 运行 dotnet test → 确认所有测试（108 现有 + 15 E2E）通过。

## Steps

1. `git checkout milestone/M006-rj9bue` — 切回里程碑分支
2. `dotnet build` — 构建整个解决方案，确认无编译错误
3. `dotnet test --verbosity normal` — 运行所有测试（包括现有 108 个 + E2E 15 个 = 123 个）
4. 确认 123 个测试全部通过，0 失败
5. 如果测试失败，分析是否是 d81cd00 checkout 引起的残留问题（如 obj/bin 缓存），必要时执行 `dotnet clean` 后重新构建

## Must-Haves

- [ ] 里程碑分支上 dotnet build 成功
- [ ] 全部 123 个测试通过（108 现有 + 15 E2E）
- [ ] 工作树状态干净，无残留编译产物冲突

## Verification

- `dotnet test --verbosity normal` 返回 0 exit code，123 passed, 0 failed

## Inputs

- `Tests/E2ERegression/E2ERegression.csproj` — E2E 项目（在切回后仍存在，因为是 tracked 文件）
- `Tests/DocuFiller.Tests.csproj` — 现有测试项目

## Expected Output

- `Tests/E2ERegression/E2ERegression.csproj` — 验证通过（无修改）
- `Tests/DocuFiller.Tests.csproj` — 验证通过（无修改）
  - Files: `Tests/E2ERegression/E2ERegression.csproj`, `Tests/DocuFiller.Tests.csproj`, `DocuFiller.sln`
  - Verify: dotnet test --verbosity normal

## Files Likely Touched

- Tests/E2ERegression/E2ERegression.csproj
- Tests/E2ERegression/ServiceFactory.cs
- Tests/E2ERegression/TestDataHelper.cs
- Tests/DocuFiller.Tests.csproj
- DocuFiller.sln
