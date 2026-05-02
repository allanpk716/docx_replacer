---
estimated_steps: 19
estimated_files: 3
skills_used: []
---

# T02: 切回里程碑分支并验证测试仍通过

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

## Inputs

- `Tests/E2ERegression/E2ERegression.csproj`
- `Tests/DocuFiller.Tests.csproj`
- `DocuFiller.sln`

## Expected Output

- `Tests/E2ERegression/E2ERegression.csproj`
- `Tests/DocuFiller.Tests.csproj`

## Verification

dotnet test --verbosity normal
