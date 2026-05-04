# S03: DragDropBehavior 提取

**Goal:** 将 MainWindow.xaml.cs 中 13 个拖放事件处理器（~422 行）提取为可复用的 FileDragDrop AttachedProperty Behavior，MainWindow.xaml.cs 仅保留 Window_PreviewDragOver（窗口级激活），拖放功能行为不变。
**Demo:** MainWindow.xaml.cs 拖放事件处理器从 13 个降至 1 个（Window 级别），拖放功能正常

## Must-Haves

- FileDragDrop Behavior 支持三种文件过滤器：DocxOrFolder（模板）、ExcelFile（数据）、DocxFile（清理）
- TextBox 使用 Preview 隧道事件绕过内置拖放，Border 使用冒泡事件
- 拖放视觉反馈行为不变（TextBox 蓝色边框 #2196F3 thickness 2 + 浅蓝背景；Border 蓝色边框 thickness 3 + 浅蓝背景）
- MainWindow.xaml.cs 行数 ≤ 130 行
- dotnet build 零错误，dotnet test 全部通过

## Proof Level

- This slice proves: contract + build verification
- Real runtime required: no（WPF AttachedProperty 编译通过即证明事件注册和绑定正确）
- Human/UAT required: no

## Verification

- `dotnet build DocuFiller.csproj --no-restore` → 0 errors
- `dotnet test --no-restore --verbosity minimal` → all pass
- `wc -l MainWindow.xaml.cs` → ≤ 130 lines
- `grep -c "private.*void.*Drag\|private.*void.*Drop" MainWindow.xaml.cs` → 1（仅 Window_PreviewDragOver）
- `test -f Behaviors/FileDragDrop.cs` → 0

## Integration Closure

- Upstream surfaces consumed: FillViewModel（HandleSingleFileDropAsync/HandleFolderDropAsync/DataPath setter）、CleanupViewModel（AddFiles/AddFolder）
- New wiring introduced in this slice: XAML xmlns 引用 Behavior 命名空间，3 个目标的 AttachedProperty 绑定
- What remains before the milestone is truly usable end-to-end: nothing — slice is self-contained

## Tasks

- [ ] **T01: Create FileDragDrop Behavior + ViewModel DropCommands** `est:2h`
  - Why: 核心实现——将重复的拖放逻辑抽象为可配置的 AttachedProperty，同时在 ViewModel 添加 DropCommand 供 Behavior 绑定
  - Files: `Behaviors/FileDragDrop.cs`, `ViewModels/FillViewModel.cs`, `DocuFiller/ViewModels/CleanupViewModel.cs`
  - Do: 创建 FileDragDrop 类（AttachedProperty 模式），包含 IsEnabled/Filter/DropCommand 三个附加属性。FileFilter 枚举支持 DocxOrFolder/ExcelFile/DocxFile。自动检测目标类型选择 Preview/冒泡事件。在 FillViewModel 添加 [RelayCommand] TemplateDropCommand 和 DataDropCommand。在 CleanupViewModel 添加 [RelayCommand] DropFilesCommand。
  - Verify: `dotnet build DocuFiller.csproj --no-restore` → 0 errors
  - Done when: FileDragDrop.cs 编译通过，ViewModel 新命令编译通过

- [ ] **T02: Wire XAML to FileDragDrop Behavior, delete code-behind handlers** `est:1h`
  - Why: 将 XAML 事件绑定切换到 AttachedProperty，删除 code-behind 中的 13 个拖放处理器和辅助方法
  - Files: `MainWindow.xaml`, `MainWindow.xaml.cs`
  - Do: MainWindow.xaml 添加 xmlns:behaviors，3 个目标的 12 个事件替换为 AttachedProperty 绑定。MainWindow.xaml.cs 删除 3 个 #region 块（~422 行拖放代码）和辅助方法，仅保留 Window_PreviewDragOver。
  - Verify: `dotnet build DocuFiller.csproj --no-restore && dotnet test --no-restore --verbosity minimal` → build 0 errors, all tests pass
  - Done when: MainWindow.xaml.cs ≤ 130 行，拖放事件处理器仅剩 Window_PreviewDragOver

## Files Likely Touched

- `Behaviors/FileDragDrop.cs`
- `ViewModels/FillViewModel.cs`
- `DocuFiller/ViewModels/CleanupViewModel.cs`
- `MainWindow.xaml`
- `MainWindow.xaml.cs`
