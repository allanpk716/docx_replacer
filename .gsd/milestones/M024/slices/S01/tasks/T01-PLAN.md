---
estimated_steps: 27
estimated_files: 2
skills_used: []
---

# T01: Add ShowCheckingAnimation property and move Checking state before delay

在 UpdateStatusViewModel 中实现 ShowCheckingAnimation 计算属性，并将 InitializeAsync 中的 Checking 状态设置移到 Task.Delay 之前，确保启动后立刻显示动画。添加单元测试验证属性行为和状态转换。

## Steps
1. 在 `ViewModels/UpdateStatusViewModel.cs` 的 `InitializeAsync` 方法中，将 `CurrentUpdateStatus = UpdateStatus.Checking` 从 `InitializeUpdateStatusAsync` 内部移到 `Task.Delay(5000)` 之前（在 try 块内、await Task.Delay 之前）。注意：如果 updateService 为 null，InitializeUpdateStatusAsync 会直接 return，此时 Checking 状态不应该被设置（因为根本没有更新服务）。所以需要在设置前检查 `_updateService != null`。
2. 修改 `InitializeUpdateStatusAsync` 方法，移除开头的 `CurrentUpdateStatus = UpdateStatus.Checking;`（已移到 InitializeAsync）。
3. 添加 `ShowCheckingAnimation` 计算属性：`public bool ShowCheckingAnimation => CurrentUpdateStatus == UpdateStatus.Checking || IsCheckingUpdate;` 这覆盖自动检查（通过 CurrentUpdateStatus.Checking）和手动检查（通过 IsCheckingUpdate）两种场景。
4. 在 `OnCurrentUpdateStatusChanged` 中添加 `OnPropertyChanged(nameof(ShowCheckingAnimation));`
5. 在 `OnIsCheckingUpdateChanged` 中添加 `OnPropertyChanged(nameof(ShowCheckingAnimation));`
6. 在 `Tests/DocuFiller.Tests/UpdateStatusViewModelTests.cs` 中添加测试：
   - `ShowCheckingAnimation_IsFalse_WhenNone` — 默认状态返回 false
   - `ShowCheckingAnimation_IsTrue_WhenChecking` — Checking 状态返回 true
   - `ShowCheckingAnimation_IsTrue_WhenIsCheckingUpdate` — IsCheckingUpdate=true 时返回 true
   - `ShowCheckingAnimation_IsFalse_WhenUpToDate` — UpToDate 状态返回 false
   - `ShowCheckingAnimation_IsFalse_WhenError` — Error 状态返回 false

## Must-Haves
- [ ] ShowCheckingAnimation 属性在 Checking 或 IsCheckingUpdate 时返回 true
- [ ] InitializeAsync 在 Task.Delay 前设置 Checking（仅当 updateService 非 null）
- [ ] PropertyChanged 通知在两个 partial method 中触发
- [ ] 新增单元测试全部通过

## Verification
- `dotnet test --filter "FullyQualifiedName~ShowCheckingAnimation"` 通过
- `dotnet test` 全部通过（无回归）

## Inputs
- `ViewModels/UpdateStatusViewModel.cs` — 现有 ViewModel，需要修改 InitializeAsync 和添加 ShowCheckingAnimation 属性
- `Tests/DocuFiller.Tests/UpdateStatusViewModelTests.cs` — 现有测试文件，需要添加新测试

## Expected Output
- `ViewModels/UpdateStatusViewModel.cs` — 添加 ShowCheckingAnimation 属性，修改 InitializeAsync 时序，更新 partial 方法通知
- `Tests/DocuFiller.Tests/UpdateStatusViewModelTests.cs` — 添加 5 个 ShowCheckingAnimation 测试方法

## Inputs

- `ViewModels/UpdateStatusViewModel.cs`
- `Tests/DocuFiller.Tests/UpdateStatusViewModelTests.cs`

## Expected Output

- `ViewModels/UpdateStatusViewModel.cs`
- `Tests/DocuFiller.Tests/UpdateStatusViewModelTests.cs`

## Verification

dotnet test --filter "FullyQualifiedName~ShowCheckingAnimation"
