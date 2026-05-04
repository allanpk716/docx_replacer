# S02: 死代码清理

**Goal:** 安全删除 ContentControlProcessor 中 6 个无引用的死方法和 DocumentProcessorService 中 2 段死代码（ProcessSingleDocumentAsync、GenerateOutputFileNameWithTimestamp），同步更新接口定义和所有受影响的测试代码，确保 dotnet build 0 错误、dotnet test 全部通过。
**Demo:** ContentControlProcessor 中 5 个死方法和 DPS 中 3 段死代码被安全移除

## Must-Haves

- ContentControlProcessor 中 ReplaceContentInContainer、ReplaceTextDirectly、FindTargetRun、CreateParagraphWithFormattedText(string)、CreateFormattedRuns、CreateFormattedTextElements 6 个方法已删除
- DocumentProcessorService 中 ProcessSingleDocumentAsync 和 GenerateOutputFileNameWithTimestamp 已删除
- IDocumentProcessor 接口不再包含 ProcessSingleDocumentAsync 方法签名
- HeaderFooterCommentIntegrationTests 中 3 个测试已改用 ProcessDocumentWithFormattedDataAsync
- CommandValidationTests 中 StubDocumentProcessor 已移除 ProcessSingleDocumentAsync 桩方法
- dotnet build 0 错误，dotnet test 全部通过，无回归

## Proof Level

- This slice proves: contract — 死代码删除后，通过编译和全部测试验证行为不变。grep 确认目标方法名不再出现。

## Integration Closure

- Upstream surfaces consumed: IDocumentProcessor 接口（被 FillCommand、MainWindowViewModel 使用）
- New wiring introduced: 无新接线，纯粹删除
- What remains: S03 将进一步提取重复方法到 OpenXmlHelper

## Verification

- Run the task and slice verification checks for this slice.

## Tasks

- [ ] **T01: 删除 ContentControlProcessor 中 6 个死方法** `est:30m`
  ContentControlProcessor 中有 6 个 private 方法从未被任何活跃代码调用（仅在彼此间形成调用链），属于早期替换逻辑被 SafeTextReplacer 替代后的遗留代码：
  - Files: `Services/ContentControlProcessor.cs`
  - Verify: grep -c 'ReplaceContentInContainer\|ReplaceTextDirectly\|FindTargetRun\|CreateFormattedRuns\|CreateFormattedTextElements' Services/ContentControlProcessor.cs 返回 0；dotnet build 0 错误；dotnet test 全部通过

- [ ] **T02: 删除 ProcessSingleDocumentAsync 和 GenerateOutputFileNameWithTimestamp，更新接口和测试** `est:1h`
  DocumentProcessorService 中有两段死代码：
  - Files: `Services/DocumentProcessorService.cs`, `Services/Interfaces/IDocumentProcessor.cs`, `Tests/Integration/HeaderFooterCommentIntegrationTests.cs`, `Tests/DocuFiller.Tests/Cli/CommandValidationTests.cs`
  - Verify: grep -rn 'ProcessSingleDocumentAsync\|GenerateOutputFileNameWithTimestamp' --include='*.cs' 排除 .gsd 目录返回 0；dotnet build 0 错误；dotnet test 全部通过

## Files Likely Touched

- Services/ContentControlProcessor.cs
- Services/DocumentProcessorService.cs
- Services/Interfaces/IDocumentProcessor.cs
- Tests/Integration/HeaderFooterCommentIntegrationTests.cs
- Tests/DocuFiller.Tests/Cli/CommandValidationTests.cs
