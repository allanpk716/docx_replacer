---
estimated_steps: 22
estimated_files: 1
skills_used: []
---

# T01: 删除 ContentControlProcessor 中 6 个死方法

ContentControlProcessor 中有 6 个 private 方法从未被任何活跃代码调用（仅在彼此间形成调用链），属于早期替换逻辑被 SafeTextReplacer 替代后的遗留代码：

1. `ReplaceContentInContainer` (line ~249) — 旧的内容替换方法，已被 ProcessContentReplacement → _safeTextReplacer 替代
2. `ReplaceTextDirectly` (line ~277) — 旧的直接文本替换方法，同上
3. `FindTargetRun` (line ~348) — 单个 Run 查找，活跃代码使用 FindAllTargetRuns
4. `CreateParagraphWithFormattedText(string)` (line ~403) — 仅被 ReplaceContentInContainer 调用
5. `CreateFormattedRuns(string)` (line ~417) — 仅被上述死方法调用
6. `CreateFormattedTextElements` (line ~458) — 仅被 ReplaceTextDirectly 调用

**关键注意**：`FindContentContainer` 方法虽然也被 ReplaceContentInContainer 调用，但同时被活跃方法 `FindAllTargetRuns` 使用，**不能删除**。

## Steps

1. 打开 `Services/ContentControlProcessor.cs`
2. 删除 `ReplaceContentInContainer` 方法（约 line 249-270）
3. 删除 `ReplaceTextDirectly` 方法（约 line 277-310）
4. 删除 `FindTargetRun` 方法（约 line 348-375）
5. 删除 `CreateParagraphWithFormattedText(string text)` 方法（约 line 403-415）
6. 删除 `CreateFormattedRuns(string text)` 方法（约 line 417-455）
7. 删除 `CreateFormattedTextElements(string text)` 方法（约 line 458-495）
8. 运行 `dotnet build` 和 `dotnet test` 确认无回归

## Must-Haves

- [ ] 6 个方法全部删除，无残留
- [ ] FindContentContainer 和 FindAllTargetRuns 保持不变
- [ ] dotnet build 0 错误
- [ ] dotnet test 全部通过

## Inputs

- `Services/ContentControlProcessor.cs`

## Expected Output

- `Services/ContentControlProcessor.cs`

## Verification

grep -c 'ReplaceContentInContainer\|ReplaceTextDirectly\|FindTargetRun\|CreateFormattedRuns\|CreateFormattedTextElements' Services/ContentControlProcessor.cs 返回 0；dotnet build 0 错误；dotnet test 全部通过
