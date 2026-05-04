---
estimated_steps: 16
estimated_files: 2
skills_used: []
---

# T01: 创建 OpenXmlHelper 共享工具类并迁移 DocumentProcessorService

创建 Utils/OpenXmlHelper.cs 静态工具类，包含 6 个从 DocumentProcessorService 和 ContentControlProcessor 提取的重复方法。先迁移 DocumentProcessorService 到共享实现，删除其 6 个私有方法，验证编译和测试通过。

**提取的方法签名：**
- `static string? GetControlTag(SdtElement control)` — 纯方法，直接复制 DPS 版本
- `static string ExtractExistingText(SdtElement control)` — 纯方法，两版本相同
- `static OpenXmlElement? FindContentContainer(SdtElement control)` — 纯方法，统一为 null-coalescing 风格
- `static List<Run> FindAllTargetRuns(SdtElement control)` — 纯方法（移除 _logger 调试日志，改为注释说明调用者可自行记录）
- `static void AddProcessingComment(WordprocessingDocument document, SdtElement control, string tag, string newValue, string oldValue, ContentControlLocation location, CommentManager commentManager, ILogger logger)` — 需要 CommentManager 和 ILogger 参数
- `static bool HasAncestorWithSameTag(SdtElement control, string tag)` — 纯方法，统一使用 GetControlTag

**DocumentProcessorService 迁移点：**
1. `FillContentControlWithFormattedValue` 中 `GetControlTag(control)` → `OpenXmlHelper.GetControlTag(control)`
2. `FillContentControlWithFormattedValue` 中 `ExtractExistingText(control)` → `OpenXmlHelper.ExtractExistingText(control)`
3. `FillContentControlWithFormattedValue` 中 `AddProcessingComment(...)` → `OpenXmlHelper.AddProcessingComment(..., _commentManager, _logger)`
4. `FillFormattedContentStandard` 中 `FindContentContainer(control)` → `OpenXmlHelper.FindContentContainer(control)`
5. `GetAllContentControls` 中 `GetControlTag(control)` → `OpenXmlHelper.GetControlTag(control)`
6. `GetAllContentControls` 中 `HasAncestorWithSameTag(control, tag)` → `OpenXmlHelper.HasAncestorWithSameTag(control, tag)`

**然后删除 DPS 中 6 个私有方法：** GetControlTag、ExtractExistingText、FindContentContainer、FindAllTargetRuns、AddProcessingComment、HasAncestorWithSameTag

## Inputs

- `Services/DocumentProcessorService.cs`
- `Services/ContentControlProcessor.cs`

## Expected Output

- `Utils/OpenXmlHelper.cs`
- `Services/DocumentProcessorService.cs`

## Verification

dotnet build && dotnet test --no-build --verbosity minimal
