# S03: DragDropBehavior 提取 — UAT

**Milestone:** M021
**Written:** 2026-05-04T11:17:34.837Z

# UAT: S03 FileDragDrop Behavior

## UAT Type
Build-verification + contract validation. Behavior 通过 XAML 绑定编译验证，编译通过即证明事件注册和 Command 绑定正确。运行时拖放行为需手动 GUI 验证。

## Not Proven By This UAT
- 运行时 GUI 拖放交互（需要手动启动应用拖放文件验证）
- 跨显示器/高 DPI 拖放场景

## Test Cases

### TC-01: 编译验证 — FileDragDrop Behavior 编译通过
**前置条件：** 工作树包含 S01 + S02 + S03 所有改动
**步骤：**
1. 运行 `dotnet build DocuFiller.csproj`
**预期结果：** 0 错误，0 警告

### TC-02: 编译验证 — XAML 绑定正确
**前置条件：** TC-01 通过
**步骤：**
1. 检查 MainWindow.xaml 中 TemplatePathTextBox 有 `behaviors:FileDragDrop.IsEnabled="True"` + `Filter="DocxOrFolder"` + `DropCommand="{Binding TemplateDropCommand}"`
2. 检查 DataPathTextBox 有 `Filter="ExcelFile"` + `DropCommand="{Binding DataDropCommand}"`
3. 检查 CleanupDropZoneBorder 有 `Filter="DocxFile"` + `DropCommand="{Binding DropFilesCommand}"`
**预期结果：** 3 个目标元素均有完整的 FileDragDrop 绑定

### TC-03: 代码清理 — MainWindow.xaml.cs 仅保留 Window 级拖放处理器
**前置条件：** TC-01 通过
**步骤：**
1. 搜索 MainWindow.xaml.cs 中所有 Drag 相关方法
2. 统计文件行数
**预期结果：** 仅 Window_PreviewDragOver 存在（1 个方法），文件 ≤130 行

### TC-04: 回归测试 — 全部测试通过
**前置条件：** TC-01 通过
**步骤：**
1. 运行 `dotnet test --verbosity minimal`
**预期结果：** 253 单元测试 + 27 E2E 测试全部通过，0 失败

### TC-05: Behavior 结构验证（手动 GUI）
**前置条件：** 应用编译成功
**步骤：**
1. 启动应用
2. 拖拽 .docx 文件到模板路径 TextBox → 应高亮并接受
3. 拖拽 .xlsx 文件到数据路径 TextBox → 应高亮并接受
4. 拖拽 .docx 文件到清理区域 Border → 应高亮并接受
5. 拖拽非目标文件类型 → 应不高亮且不接受
6. 拖拽文件经过窗口边缘 → 窗口自动激活（Window_PreviewDragOver）
**预期结果：** 所有拖放行为与重构前一致
