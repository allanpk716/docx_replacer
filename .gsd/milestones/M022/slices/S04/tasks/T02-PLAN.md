---
estimated_steps: 12
estimated_files: 1
skills_used: []
---

# T02: 调研核心依赖库跨平台兼容性并撰写 core-dependencies-compatibility.md

**Slice:** S04 — 通用课题调研（Velopack/核心库/平台差异/打包分发）
**Milestone:** M022

## Description

调研 DocuFiller 所有核心 NuGet 依赖的跨平台兼容性。DocuFiller 当前目标框架为 net8.0-windows（WPF），迁移到跨平台 UI 框架后需要确认核心业务逻辑库能否在 net8.0（无 Windows 后缀）上运行。

需要调研的核心依赖（来自 DocuFiller.csproj）：
- DocumentFormat.OpenXml 3.0.1 — Word 文档操作的核心库
- EPPlus 7.5.2 — Excel 解析库
- CommunityToolkit.Mvvm 8.4.0 — MVVM 源代码生成器
- Microsoft.Extensions.DependencyInjection 8.0.0 — DI 容器
- Microsoft.Extensions.Logging 8.0.0 + Console + Debug — 日志
- Microsoft.Extensions.Configuration 8.0.0 + Json + Binder + EnvironmentVariables + Xml — 配置
- Microsoft.Extensions.Http 8.0.0 — HTTP 客户端
- Microsoft.Extensions.Options.ConfigurationExtensions 8.0.0 — Options 模式
- System.Configuration.ConfigurationManager 8.0.0 — 兼容旧配置
- Velopack 0.0.1298 — 自动更新（T01 已单独调研）

调研内容必须覆盖：
1. 调研概述（DocuFiller 依赖全景图、分类）
2. DocumentFormat.OpenXml 跨平台兼容性（是否支持 net8.0、是否有 Windows 特定依赖、性能特征）
3. EPPlus 跨平台兼容性（net8.0 支持、非 Windows 平台限制、许可证影响）
4. Microsoft.Extensions.* 系列跨平台兼容性（DI、Logging、Configuration、Http、Options）
5. CommunityToolkit.Mvvm 跨平台兼容性（源代码生成器在各平台的行为）
6. Services/ 层的服务接口分析（哪些依赖 WPF/Windows API、哪些纯粹跨平台）
7. 潜在问题与风险（反射依赖、原生库、平台特定 API）
8. 替代方案（如有不兼容的库，列出替代选择）
9. 对 DocuFiller 的建议（迁移策略、优先处理哪些依赖）
10. 优缺点总结
11. 调研日期与信息来源

关键调研方向：DocumentFormat.OpenXml 和 EPPlus 是 DocuFiller 的核心业务依赖（文档处理 + 数据解析），如果它们不能跨平台，整个迁移方案就不可行。需要特别关注：(1) OpenXml SDK 是否使用 COM 互操作或 Windows 特定 API；(2) EPPlus 的许可证（Polyform Noncommercial）是否影响跨平台部署；(3) Services/ 层中是否有隐含的 Windows 依赖。

## Steps

1. 读取 DocuFiller.csproj 获取完整依赖列表和版本号
2. 使用 web 搜索逐个调研核心 NuGet 包的跨平台兼容性
3. 重点关注 DocumentFormat.OpenXml — 查看 NuGet 页面、GitHub 仓库、文档确认 net8.0 支持
4. 重点关注 EPPlus — 查看许可证、跨平台支持、已知限制
5. 调研 Microsoft.Extensions.* 的跨平台能力（已知是跨平台的，但需确认具体版本）
6. 分析 DocuFiller Services/ 层的 Windows 依赖（通过阅读已有调研文档中的分析）
7. 整理各依赖的兼容性结论和风险等级
8. 撰写完整调研文档

## Must-Haves

- [ ] 覆盖 DocuFiller.csproj 中的所有核心 NuGet 依赖
- [ ] 每个依赖有明确的跨平台兼容性结论（兼容/部分兼容/不兼容）
- [ ] 包含对 Services/ 层 Windows 依赖的分析
- [ ] 包含替代方案建议
- [ ] 无 TBD/TODO 占位符

## Verification

- bash -c 'FILE="docs/cross-platform-research/core-dependencies-compatibility.md" && test -f "$FILE" && grep -c "^## " "$FILE" | grep -q "^[8-9]\|[1-9][0-9]$" && ! grep -q "TBD\|TODO" "$FILE" && wc -w "$FILE" | awk "{if(\$1 >= 3000) exit 0; else exit 1}"'

## Inputs

- `DocuFiller.csproj` — 所有 NuGet 依赖的名称和版本号

## Expected Output

- `docs/cross-platform-research/core-dependencies-compatibility.md` — 核心依赖库兼容性调研文档
