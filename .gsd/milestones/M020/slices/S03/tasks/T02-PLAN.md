---
estimated_steps: 12
estimated_files: 1
skills_used: []
---

# T02: 迁移 ContentControlProcessor 到共享工具类并最终验证

将 ContentControlProcessor 中的 6 个重复私有方法替换为 OpenXmlHelper 调用，删除私有方法，验证编译和测试通过。

**ContentControlProcessor 迁移点：**
1. `ProcessContentControl` 中 `GetControlTag(control)` → `OpenXmlHelper.GetControlTag(control)`
2. `ProcessContentControl` 中 `ExtractExistingText(control)` → `OpenXmlHelper.ExtractExistingText(control)`
3. `ProcessContentControl` 中 `AddProcessingComment(...)` → `OpenXmlHelper.AddProcessingComment(..., _commentManager, _logger)`
4. `ProcessControlsInPart` 中 `GetControlTag(c)` → `OpenXmlHelper.GetControlTag(c)`
5. `ProcessControlsInPart` 中 `HasAncestorWithSameTag(x.Control, x.Tag!)` → `OpenXmlHelper.HasAncestorWithSameTag(x.Control, x.Tag!)`

**然后删除 CCP 中 6 个私有方法：** GetControlTag、ExtractExistingText、FindContentContainer、FindAllTargetRuns、AddProcessingComment、HasAncestorWithSameTag

**最终验证：**
- `dotnet build` 0 错误
- `dotnet test` 全部 256 通过
- grep 确认 6 个方法名在 DPS 和 CCP 源码中不存在私有方法定义（仅在 OpenXmlHelper 中存在）

## Inputs

- `Services/ContentControlProcessor.cs`
- `Utils/OpenXmlHelper.cs`

## Expected Output

- `Services/ContentControlProcessor.cs`

## Verification

dotnet build && dotnet test --verbosity minimal && grep -c 'private.*GetControlTag\|private.*ExtractExistingText\|private.*FindContentContainer\|private.*FindAllTargetRuns\|private.*AddProcessingComment\|private.*HasAncestorWithSameTag' Services/DocumentProcessorService.cs Services/ContentControlProcessor.cs
