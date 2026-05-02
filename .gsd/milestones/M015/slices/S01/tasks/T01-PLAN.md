---
estimated_steps: 1
estimated_files: 3
skills_used: []
---

# T01: Replace GithubSource with SimpleWebSource CDN URL and update tests

将 UpdateService 中两处 GithubSource 替换为 SimpleWebSource，使用 GitHub CDN 直连 URL（/releases/latest/download/），消除 API rate limit。更新相关测试断言和接口文档注释。

## Inputs

- `Services/UpdateService.cs`
- `Services/Interfaces/IUpdateService.cs`
- `Tests/UpdateServiceTests.cs`

## Expected Output

- `Services/UpdateService.cs`
- `Services/Interfaces/IUpdateService.cs`
- `Tests/UpdateServiceTests.cs`

## Verification

dotnet build && dotnet test && grep -r "GithubSource" --include="*.cs" . 应返回空
