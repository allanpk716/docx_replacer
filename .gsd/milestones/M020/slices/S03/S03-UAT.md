# S03: 消除 DocumentProcessorService 和 ContentControlProcessor 重复代码 — UAT

**Milestone:** M020
**Written:** 2026-05-04T00:53:44.231Z

## UAT: S03 消除重复代码

**UAT Type**: Contract verification (编译通过 + 全量测试通过)
**Not Proven By This UAT**: 无。本 slice 纯重构，不改变外部行为，通过编译和测试套件证明行为不变。

### 前置条件
- 工作目录在 `docx_replacer` 根目录
- .NET 8 SDK 已安装

### TC-01: 编译验证
1. 运行 `dotnet build` — 预期：0 错误，0 警告（除常规 CS 匹配警告）
2. 确认输出中无 error 级别消息

### TC-02: 全量测试
1. 运行 `dotnet test --verbosity minimal` — 预期：256 测试全部通过（229 + 27）
2. 确认 0 failed, 0 skipped

### TC-03: 零残留重复方法
1. 运行 `grep -rn "private.*GetControlTag\|private.*ExtractExistingText\|private.*FindContentContainer\|private.*FindAllTargetRuns\|private.*AddProcessingComment\|private.*HasAncestorWithSameTag" Services/DocumentProcessorService.cs Services/ContentControlProcessor.cs`
2. 预期：0 匹配

### TC-04: 共享工具类完整性
1. 确认 `Utils/OpenXmlHelper.cs` 文件存在
2. 确认包含 6 个 public static 方法：GetControlTag, ExtractExistingText, FindContentContainer, FindAllTargetRuns, AddProcessingComment, HasAncestorWithSameTag

### TC-05: 引用正确性
1. 在 DPS 中搜索 `OpenXmlHelper.` — 预期：10 个调用点
2. 在 CCP 中搜索 `OpenXmlHelper.` — 预期：5 个调用点
