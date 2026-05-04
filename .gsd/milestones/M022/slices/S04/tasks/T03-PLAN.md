---
estimated_steps: 12
estimated_files: 1
skills_used: []
---

# T03: 调研平台差异处理并撰写 platform-differences.md

**Slice:** S04 — 通用课题调研（Velopack/核心库/平台差异/打包分发）
**Milestone:** M022

## Description

调研 DocuFiller 跨平台迁移时需要处理的操作系统差异。DocuFiller 当前深度绑定 Windows（WPF、Win32 文件对话框、Windows 路径约定），迁移到任何跨平台 UI 框架都需要处理这些差异。

调研内容必须覆盖：
1. 调研概述（DocuFiller 中 Windows 特定功能的全景梳理）
2. 文件对话框差异（WPF OpenFileDialog/SaveFileDialog vs 各框架的跨平台方案、文件过滤器语法差异、多文件选择）
3. 拖放功能差异（WPF DragDrop vs 跨平台拖放、各框架的拖放 API、文件拖放在 Linux/macOS 的行为）
4. 路径处理差异（Windows 反斜杠 vs Unix 正斜杠、Path 类的跨平台行为、绝对/相对路径、UNC 路径）
5. 文件系统差异（应用数据目录：Windows %APPDATA% vs macOS ~/Library/Application Support vs Linux ~/.config、XDG Base Directory 规范、临时目录、文件权限模型）
6. 进程与系统管理（Process.Start() 在各平台的行为差异、Shell execute vs 直接执行、环境变量差异）
7. 文件关联与 MIME 类型（Windows 文件关联 vs Linux MIME types vs macOS UTI）
8. 各跨平台 UI 框架的抽象层（Avalonia StorageProvider、Tauri dialog API、Electron dialog API）
9. 对 DocuFiller 的建议（哪些差异需要代码适配、推荐的平台抽象策略）
10. 优缺点总结
11. 调研日期与信息来源

关键调研方向：文件对话框和拖放是 DocuFiller 用户交互的核心（选择 Word 模板 + Excel 数据文件），需要确认各 UI 框架在这些场景下的 API 完备性和行为一致性。路径处理影响配置文件和数据文件的存储位置，需要遵循各平台的约定。

## Steps

1. 梳理 DocuFiller 中所有 Windows 特定 API 的使用位置（参考已有调研文档中的分析）
2. 使用 web 搜索调研文件对话框在各跨平台 UI 框架中的 API
3. 调研拖放功能在 Linux/macOS 上的实现差异
4. 调研 .NET Path 类在跨平台场景下的行为
5. 调研 XDG Base Directory 规范和 macOS 应用数据目录约定
6. 调研各框架的平台抽象层（Avalonia IStorageProvider 等）
7. 整理各差异的处理方案和推荐策略
8. 撰写完整调研文档

## Must-Haves

- [ ] 覆盖文件对话框、拖放、路径处理三大核心差异
- [ ] 包含各跨平台 UI 框架的对应方案
- [ ] 包含文件系统权限和目录规范的分析
- [ ] 包含对 DocuFiller 的具体适配建议
- [ ] 无 TBD/TODO 占位符

## Verification

- bash -c 'FILE="docs/cross-platform-research/platform-differences.md" && test -f "$FILE" && grep -c "^## " "$FILE" | grep -q "^[8-9]\|[1-9][0-9]$" && ! grep -q "TBD\|TODO" "$FILE" && wc -w "$FILE" | awk "{if(\$1 >= 3000) exit 0; else exit 1}"'

## Inputs

- `DocuFiller.csproj` — 项目框架和依赖信息

## Expected Output

- `docs/cross-platform-research/platform-differences.md` — 平台差异处理调研文档
