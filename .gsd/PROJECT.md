# DocuFiller

## What This Is

DocuFiller 是一个基于 .NET 8 + WPF 的桌面应用程序，提供 Word 文档批量填充（Excel 数据源→内容控件替换）、富文本格式替换（上标/下标）、批注追踪、审核清理四大功能。支持 GUI（WPF）和 CLI（JSONL 输出）双模式。通过 Velopack 实现自动更新，支持内网 Go 服务器和 GitHub Releases 双更新源。

## Core Value

让用户用 Excel 数据批量填充 Word 模板中的内容控件，生成格式正确的文档，全程可见、可控、可回溯。

## Current State

- GUI + CLI 双模式完整可用
- Excel 两列/三列格式自动检测
- 正文/页眉/页脚内容控件替换 + 批注追踪
- 文档清理（去批注、解包内容控件）
- Velopack 自动更新（安装版 + 便携版：内网 Go + GitHub Releases 双更新源）
- 内网 Go 更新服务器（update-server/）
- E2E 更新测试脚本（安装版 + 便携版：本地 HTTP + Go 服务器）

## Architecture / Key Patterns

- MVVM + 依赖注入（Microsoft.Extensions.DependencyInjection）
- Singleton 服务为主，清理服务/窗口为 Transient
- OpenXML SDK 操作 Word 文档
- EPPlus 操作 Excel
- Velopack 管理安装/更新/便携版打包
- CLI 通过 AttachConsole(-1) P/Invoke 输出到父控制台
- JSONL 统一输出格式
- 持久化配置：`%USERPROFILE%\.docx_replacer\update-config.json` 独立于安装目录

## Capability Contract

See `.gsd/REQUIREMENTS.md` for the explicit capability contract, requirement status, and coverage mapping.

## Milestone Sequence

- [x] M001-M018: 历史里程碑（核心功能、CLI、更新系统、UI 优化、便携版更新支持等）
