# S03: 消除 DocumentProcessorService 和 ContentControlProcessor 重复代码

**Goal:** 将 DocumentProcessorService 和 ContentControlProcessor 之间 6 个重复方法（GetControlTag、ExtractExistingText、FindContentContainer、FindAllTargetRuns、AddProcessingComment、HasAncestorWithSameTag）提取到共享静态工具类 Utils/OpenXmlHelper.cs，两个服务类改为引用共享实现，删除各自私有副本。重构后 dotnet build 0 错误，dotnet test 256 测试全部通过，grep 确认零残留重复实现。
**Demo:** 7 个重复方法提取到共享工具类 OpenXmlHelper，两个服务类引用共享实现

## Must-Haves

- `Utils/OpenXmlHelper.cs` 存在，包含 6 个 public static 方法
- DocumentProcessorService 中 6 个重复私有方法已删除，改为调用 OpenXmlHelper
- ContentControlProcessor 中 6 个重复私有方法已删除，改为调用 OpenXmlHelper
- dotnet build 0 错误
- dotnet test 全部 256 测试通过（229 + 27）
- grep 确认 GetControlTag、ExtractExistingText、FindContentContainer、FindAllTargetRuns、AddProcessingComment、HasAncestorWithSameTag 在 DPS 和 CCP 源码中无私有方法定义

## Proof Level

- This slice proves: contract — 提取的是内部工具方法，通过编译通过和全部现有测试通过证明行为不变

## Integration Closure

- 上游服务: DocumentProcessorService 和 ContentControlProcessor 注入 CommentManager、ISafeTextReplacer、ISafeFormattedContentReplacer 等依赖不变
- 新引入的共享类: OpenXmlHelper 为 static 工具类，不需要 DI 注册
- 剩余工作: 无。本 slice 完成后 M020-S03 目标达成

## Verification

- Run the task and slice verification checks for this slice.

## Tasks

- [ ] **T01: 创建 OpenXmlHelper 共享工具类并迁移 DocumentProcessorService** `est:45m`
  创建 Utils/OpenXmlHelper.cs 静态工具类，包含 6 个从 DocumentProcessorService 和 ContentControlProcessor 提取的重复方法。先迁移 DocumentProcessorService 到共享实现，删除其 6 个私有方法，验证编译和测试通过。
  - Files: `Utils/OpenXmlHelper.cs`, `Services/DocumentProcessorService.cs`
  - Verify: dotnet build && dotnet test --no-build --verbosity minimal

- [ ] **T02: 迁移 ContentControlProcessor 到共享工具类并最终验证** `est:30m`
  将 ContentControlProcessor 中的 6 个重复私有方法替换为 OpenXmlHelper 调用，删除私有方法，验证编译和测试通过。
  - Files: `Services/ContentControlProcessor.cs`
  - Verify: dotnet build && dotnet test --verbosity minimal && grep -c 'private.*GetControlTag\|private.*ExtractExistingText\|private.*FindContentContainer\|private.*FindAllTargetRuns\|private.*AddProcessingComment\|private.*HasAncestorWithSameTag' Services/DocumentProcessorService.cs Services/ContentControlProcessor.cs

## Files Likely Touched

- Utils/OpenXmlHelper.cs
- Services/DocumentProcessorService.cs
- Services/ContentControlProcessor.cs
