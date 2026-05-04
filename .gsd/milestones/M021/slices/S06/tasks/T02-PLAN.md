---
estimated_steps: 29
estimated_files: 1
skills_used: []
---

# T02: Sync product requirements doc UI descriptions with actual code

Update `docs/DocuFiller产品需求文档.md` to reflect the actual UI after M021 refactoring (S01 FillVM + S02 CleanupVM + S05 auto-update). The document currently has several inaccuracies.

## Specific Changes

### §3.2 数据配置模块 — Page Elements table
- Change "数据类型显示（Excel/JSON）" → "数据类型显示（Excel 列格式：两列/三列）"
- JSON support was removed in M004

### §3.5 审核清理模块 — Function description
- Change "提供独立的清理窗口" → "提供主窗口审核清理选项卡和独立清理窗口两种入口"
- Add note: cleanup Tab in MainWindow shares CleanupViewModel with CleanupWindow
- Add note: Tab mode supports output directory (dual-mode cleanup), CleanupWindow works in-place

### §4.3 审核清理流程
- Update first step from "打开清理窗口" → "通过主窗口审核清理选项卡或独立清理窗口开始"

### §5.2 主界面布局 — table
- Change "数据类型显示（Excel/JSON）" → "数据类型显示（Excel 两列/三列格式）"
- Change "开始处理按钮（绿色大尺寸），暂停/恢复按钮，取消按钮" → "开始处理按钮、取消处理按钮、退出按钮"
- Add a new row for the StatusBar: "底部状态栏 | 版本号显示、处理进度消息、更新状态文本（可点击）、更新源设置按钮（齿轮图标）、检查更新按钮、新版本红点提示"
- Add a new row for the Tab navigation: "选项卡导航 | 关键词替换选项卡、审核清理选项卡"

### §5.3 清理窗口布局
- Rename section to "清理功能界面布局" (covers both Tab and Window)
- Add subsection for "主窗口审核清理选项卡": output directory selector, drag-drop area, file list with remove/clear buttons, progress bar, start cleanup button
- Keep existing CleanupWindow description as "独立清理窗口" subsection
- Note that Tab mode defaults output to Documents\DocuFiller输出\清理

## Must-Haves
- [ ] Zero references to JSON data source in UI description sections (§3.2, §5.2)
- [ ] Zero references to "暂停/恢复" buttons
- [ ] §5.2 includes StatusBar row with version, update status, check update button, settings gear
- [ ] §5.2 includes Tab navigation row
- [ ] §3.5 describes both Tab and Window cleanup entry points
- [ ] §5.3 describes both cleanup Tab layout and cleanup window layout
- [ ] No TBD/TODO markers

## Inputs

- `docs/DocuFiller产品需求文档.md`
- `MainWindow.xaml`
- `ViewModels/MainWindowViewModel.cs`
- `ViewModels/FillViewModel.cs`
- `DocuFiller/ViewModels/CleanupViewModel.cs`

## Expected Output

- `docs/DocuFiller产品需求文档.md`

## Verification

grep -c 'JSON' docs/DocuFiller产品需求文档.md | grep -q '^0$' || echo 'WARNING: JSON references remain'; grep -c '暂停' docs/DocuFiller产品需求文档.md | grep -q '^0$' || echo 'WARNING: 暂停 references remain'; grep -c 'StatusBar\|状态栏' docs/DocuFiller产品需求文档.md | grep -q '^[1-9]' && echo 'StatusBar section present' || echo 'MISSING StatusBar'; grep -c '选项卡导航\|审核清理选项卡' docs/DocuFiller产品需求文档.md | grep -q '^[1-9]' && echo 'Tab navigation present' || echo 'MISSING Tab navigation'
