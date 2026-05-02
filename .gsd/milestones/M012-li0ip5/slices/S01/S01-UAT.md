# S01: 主界面布局紧凑化 — UAT

**Milestone:** M012-li0ip5
**Written:** 2026-05-02T00:53:39.442Z

# S01: 主界面布局紧凑化 — UAT

**Milestone:** M012-li0ip5
**Written:** 2026-05-02

## UAT Type

- UAT mode: live-runtime
- Why this mode is sufficient: This slice is a pure UI layout change — compacting an existing WPF window. Verification requires visual inspection of the running application at 1366x768 resolution to confirm no scrolling is needed.

## Preconditions

- DocuFiller 已编译（dotnet build 通过）
- 显示器分辨率为 1366x768（或使用系统缩放模拟）

## Smoke Test

1. 启动 DocuFiller GUI（无命令行参数）
2. **Expected:** 窗口尺寸约 900x550，两个 Tab（关键词替换、审核清理）标题可见，窗口在 1366x768 屏幕内完整显示

## Test Cases

### 1. 窗口尺寸和最小尺寸

1. 启动应用，观察窗口初始大小
2. 尝试缩小窗口至最小
3. **Expected:** 窗口默认约 900x550，最小可缩至 800x500，不能再小

### 2. 关键词替换 Tab 无 GroupBox

1. 切换到"关键词替换"Tab
2. 观察布局结构
3. **Expected:** 无 GroupBox 边框，使用文字标签 + 分隔线替代；布局为紧凑的行式排列

### 3. 拖放功能正常（模板文件路径）

1. 从资源管理器拖拽一个 .docx 文件到"模板文件"路径文本框
2. **Expected:** 拖入时文本框边框变色（视觉反馈），松开后文件路径填入文本框

### 4. 拖放功能正常（数据文件路径）

1. 从资源管理器拖拽一个 .xlsx 文件到"数据文件"路径文本框
2. **Expected:** 拖入时文本框边框变色，松开后文件路径填入文本框

### 5. 浏览按钮功能

1. 点击"模板文件"旁的"浏览"按钮
2. **Expected:** 打开文件选择对话框
3. 点击"数据文件"旁的"浏览"按钮
4. **Expected:** 打开文件选择对话框

### 6. 审核清理 Tab 无 GroupBox

1. 切换到"审核清理"Tab
2. 观察布局结构
3. **Expected:** 无 GroupBox，输出目录为内联行布局，字号与 Tab 1 一致（12-14px）

### 7. 审核清理拖放区域

1. 从资源管理器拖拽文件到审核清理 Tab 的拖放区域
2. **Expected:** 拖入时边框变色，松开后文件添加到列表

### 8. 两个 Tab 视觉风格一致

1. 在两个 Tab 之间切换
2. **Expected:** 字号、间距、按钮大小一致，整体感觉为统一设计

### 9. 1366x768 下无需滚动

1. 将窗口放到 1366x768 分辨率的屏幕上
2. 分别检查两个 Tab 的所有内容
3. **Expected:** 所有控件完整可见，无需滚动

## Edge Cases

### 窗口拖拽到最小尺寸

1. 将窗口缩至 800x500
2. **Expected:** 控件不完全被裁剪，核心操作区仍可用（可能需要窗口最大化查看全部）

## Failure Signals

- 编译错误（dotnet build 失败）
- 窗口尺寸仍为 1400x900
- 出现 GroupBox 边框
- 拖放文件到路径文本框无反应
- Tab 切换后布局错乱或控件重叠

## Not Proven By This UAT

- 窗口未聚焦时拖放功能（R054）— 这是 S02 的范围
- 实际的文档填充和清理功能端到端流程（本 slice 只改 UI 布局）

## Notes for Tester

- 拖放视觉反馈在 Tab 1 是 TextBox 边框变色，在 Tab 2 是 Border 区域变色（不同的控件类型，但效果类似）
- 操作按钮区域使用了 DockPanel 底部固定布局，两个 Tab 按钮始终在窗口底部
