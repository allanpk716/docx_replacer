---
id: T01
parent: S01
milestone: M020
key_files:
  - Services/FileService.cs
  - Tests/DocuFiller.Tests/Services/DocumentProcessorServiceIntegrationTests.cs
  - Tests/ExcelDataParserServiceTests.cs
  - Tests/Integration/HeaderFooterCommentIntegrationTests.cs
key_decisions:
  - (none)
duration: 
verification_result: passed
completed_at: 2026-05-03T17:28:25.338Z
blocker_discovered: false
---

# T01: FileService 添加 ILogger 依赖，4 个裸 catch 块改为结构化日志记录（含文件路径上下文）

**FileService 添加 ILogger 依赖，4 个裸 catch 块改为结构化日志记录（含文件路径上下文）**

## What Happened

FileService 原有 4 个方法（EnsureDirectoryExists、CopyFileAsync、DeleteFile、WriteFileContentAsync）使用裸 catch 块静默吞掉异常，完全没有 ILogger 依赖。实现变更：

1. 添加 `ILogger<FileService>` 构造函数参数和私有 `_logger` 字段
2. 添加 `using Microsoft.Extensions.Logging` 引用
3. 4 个 catch 块从 `catch { return false; }` 改为 `catch (Exception ex) { _logger.LogError(ex, "...", pathArgs...); return false; }`，每条日志包含异常对象和文件路径的结构化参数
4. 接口 IFileService 不变，DI 容器已通过 `AddSingleton(typeof(ILogger<>), typeof(Logger<>))` 自动注入，App.xaml.cs 无需修改
5. 更新 5 处测试代码中的 `new FileService()` 为 `new FileService(new NullLogger<FileService>())`，添加 Abstractions using

所有 249 个现有测试通过，无回归。

## Verification

1. `dotnet build` 通过，0 错误
2. `grep -c 'LogError' Services/FileService.cs` 返回 4（4 个 catch 块均添加了日志）
3. `dotnet test` 全部 249 测试通过（222 + 27），无失败

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `dotnet build -v q` | 0 | ✅ pass | 1990ms |
| 2 | `grep -c 'LogError' Services/FileService.cs` | 0 | ✅ pass (4 LogError calls) | 50ms |
| 3 | `dotnet test --no-build -v q` | 0 | ✅ pass (222+27=249 tests) | 16000ms |

## Deviations

None.

## Known Issues

None.

## Files Created/Modified

- `Services/FileService.cs`
- `Tests/DocuFiller.Tests/Services/DocumentProcessorServiceIntegrationTests.cs`
- `Tests/ExcelDataParserServiceTests.cs`
- `Tests/Integration/HeaderFooterCommentIntegrationTests.cs`
