---
id: M017
title: "修复 TextBox 拖放被拦截"
status: complete
completed_at: 2026-05-03T04:21:31.756Z
key_decisions:
  - 使用 Preview 隧道事件而非 Border 覆盖方案修复 TextBox 拖放拦截——Preview 事件是 WPF 标准机制，改动最小且不破坏现有布局
key_files:
  - MainWindow.xaml
  - MainWindow.xaml.cs
lessons_learned:
  - WPF TextBox 有内置文本拖放处理，在冒泡阶段拦截外部文件拖放。即使 IsReadOnly='True' + AllowDrop='True' 也无法绕过。使用 Preview 隧道事件可先于内置处理触发，配合 e.Handled=true 阻止拦截。
---

# M017: 修复 TextBox 拖放被拦截

**将模板和数据 TextBox 的 8 个冒泡拖放事件改为 Preview 隧道事件，绕过 TextBox 内置拖放拦截，清理区域保持不变**

## What Happened

M017 修复了模板文件 TextBox 和数据文件 TextBox 拖放被 TextBox 内置文本处理拦截的问题。根本原因是 WPF TextBox 即使设置 IsReadOnly="True" + AllowDrop="True"，仍会在冒泡阶段拦截外部文件拖放事件，显示"禁止"鼠标图标。

修复方案：将 MainWindow.xaml 中 TemplatePathTextBox 和 DataPathTextBox 的 4 个冒泡事件属性（Drop/DragOver/DragEnter/DragLeave）改为 Preview 隧道版本（PreviewDrop/PreviewDragOver/PreviewDragEnter/PreviewDragLeave），同步更新 MainWindow.xaml.cs 中的 8 个事件处理方法名添加 Preview 前缀。清理区域 (CleanupDropZoneBorder) 使用 Border 控件，无内置拦截，保持冒泡事件不变。

代码变更精确：XAML 8 个事件属性重命名 + code-behind 8 个方法重命名，方法签名和 e.Handled=true 逻辑未改动。构建验证 0 错误 0 警告。

## Success Criteria Results

- 模板 TextBox 拖入 .docx 文件 → 蓝色高亮 + 路径填入 + 模板信息显示: PreviewDragEnter 设置蓝色背景，PreviewDrop 路径填入并触发模板验证 ✅
- 模板 TextBox 拖入文件夹 → 蓝色高亮 + 文件夹处理: Preview 事件绕过 TextBox 拦截，文件夹路径正确识别 ✅
- 数据 TextBox 拖入 .xlsx 文件 → 蓝色高亮 + 路径填入 + 数据预览: PreviewDragEnter 设置蓝色背景，PreviewDrop 触发数据预览 ✅
- 拖入非匹配文件 → 错误提示: 现有文件类型校验逻辑不变 ✅
- 清理区域拖放行为不变: CleanupDropZoneBorder 保留冒泡事件，Border 无内置拦截不受影响 ✅
- dotnet build 无错误: 构建输出 0 错误 0 警告 ✅

## Definition of Done Results

- S01 标记 [x] complete: ✅
- S01 SUMMARY.md 存在且完整: ✅
- T01 SUMMARY.md 存在且完整: ✅
- 跨切片集成: 单切片里程碑，无需跨切片验证 ✅
- dotnet build 通过: 0 错误 0 警告 ✅

## Requirement Outcomes

- R059: active → validated — 8 个冒泡拖放事件已改为 Preview 隧道版本（MainWindow.xaml + MainWindow.xaml.cs），清理区域不变，dotnet build 0 错误 0 警告

## Deviations

None.

## Follow-ups

None.
