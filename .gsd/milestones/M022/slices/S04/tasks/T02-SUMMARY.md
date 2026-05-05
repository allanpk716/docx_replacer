---
id: T02
parent: S04
milestone: M022
key_files:
  - docs/cross-platform-research/core-dependencies-compatibility.md
key_decisions:
  - 确认所有核心 NuGet 依赖均支持 net8.0 跨平台运行，迁移方案在依赖层面无不可逾越的障碍
  - 建议移除 System.Configuration.ConfigurationManager 遗留依赖，统一使用 Microsoft.Extensions.Configuration
  - 建议将 Dispatcher 调用抽象为 IUiThreadInvoker 接口以解耦服务层与 UI 框架
duration: 
verification_result: passed
completed_at: 2026-05-04T17:05:11.359Z
blocker_discovered: false
---

# T02: 调研完成 DocuFiller 全部 16 个 NuGet 依赖的跨平台兼容性，确认核心业务库（OpenXml + EPPlus）均为纯托管实现，可直接在 net8.0 上运行

**调研完成 DocuFiller 全部 16 个 NuGet 依赖的跨平台兼容性，确认核心业务库（OpenXml + EPPlus）均为纯托管实现，可直接在 net8.0 上运行**

## What Happened

对 DocuFiller.csproj 中的全部 NuGet 依赖进行了跨平台兼容性调研。通过 NuGet Gallery 元数据确认每个包的目标框架支持，通过 web 搜索获取各库的跨平台实现细节，通过代码扫描识别 Services/ 层中的 Windows 特定 API 使用。

核心发现：
1. DocumentFormat.OpenXml 3.0.1 — 纯 C# 托管实现，目标 .NET Standard 2.0，全平台兼容，无 COM 互操作、无 Windows API、无原生库
2. EPPlus 7.5.2 — 纯托管实现，目标 .NET 6.0+，全平台兼容，官方提供 Linux Docker 示例验证跨平台能力
3. CommunityToolkit.Mvvm 8.4.0 — 源代码生成器在各平台行为一致，Avalonia/MAUI 官方推荐
4. Microsoft.Extensions.* 系列 — 全部以 net8.0 为目标，纯托管，ASP.NET Core 生产环境大规模验证
5. Services/ 层 15/17 个服务文件零 Windows 依赖，可直接复用
6. 唯一需要适配的 3 处：ConsoleHelper（kernel32.dll P/Invoke）、ProgressReporterService（WPF Dispatcher）、App.xaml.cs 中 1 处 System.Configuration.ConfigurationManager 使用

调研文档按照 avalonia-research.md 的格式撰写，包含 12 个编号章节、目录、表格对比和信息来源列表。

## Verification

验证命令通过：文件存在、13 个 ## 章节（≥8）、无 TBD/TODO、3119 字（≥3000）。

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `bash -c 'FILE="docs/cross-platform-research/core-dependencies-compatibility.md" && test -f "$FILE" && grep -c "^## " "$FILE" | grep -q "^[8-9]\|[1-9][0-9]$" && ! grep -q "TBD\|TODO" "$FILE" && wc -w "$FILE" | awk "{if(\$1 >= 3000) exit 0; else exit 1}"'` | 0 | ✅ pass | 500ms |

## Deviations

None.

## Known Issues

None.

## Files Created/Modified

- `docs/cross-platform-research/core-dependencies-compatibility.md`
