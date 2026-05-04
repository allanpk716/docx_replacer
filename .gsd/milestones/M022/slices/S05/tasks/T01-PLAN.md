---
estimated_steps: 13
estimated_files: 11
skills_used: []
---

# T01: 撰写全方案横向对比评估文档 comparison-and-recommendation.md

阅读 S01-S04 产出的全部 10 份调研文档，提取各方案的关键评估数据（评分、TRL、SWOT、迁移成本），撰写一份综合横向对比评估文档。文档需包含：执行摘要、评估方法论、方案概览、多维度对比分析表（技术可行性/迁移成本/性能/跨平台覆盖/生态成熟度/Velopack兼容性/包体积/社区活跃度）、加权综合评分、SWOT 矩阵汇总、推荐排序（1-6）及理由、风险评估、迁移路线图建议、信息来源。使用中文撰写，参照已有调研文档的格式风格。

各方案已提取的关键评估数据（来自源文档，供参考）:
- Avalonia: 综合评分 4.3/5，整体 TRL 7，XAML 高度兼容，Velopack 无缝集成，自包含 60-90MB，29K+ GitHub Stars
- Blazor Hybrid: 综合评分 3.7/5，整体 TRL 7（WPF 渐进迁移），Linux 依赖社区项目 TRL 4，可渐进迁移但 UI 仍需重写
- 纯 Web 应用: 综合评分 3.0/5，整体 TRL 5，文件系统访问是核心阻碍，后端服务层 100% 可复用
- .NET MAUI: 综合评分 2.8/5，整体 TRL 5，官方不支持 Linux 是致命短板
- Electron.NET: 整体 TRL 6，PoC 已验证 IPC/SSE/文件对话框，社区衰退风险，包体积 120-180MB
- Tauri + .NET: 整体 TRL 6，PoC 已验证 sidecar/SSE/原生对话框，性能优于 Electron，社区活跃但双进程复杂度高

基础设施调研关键发现（来自 S04）:
- Velopack: 可统一三平台更新，Linux 需 NFPM 补充
- 核心依赖: 全部 16 个 NuGet 包为纯托管实现，支持跨平台
- 平台差异: 文件对话框 5 处需重写，拖放 1 处需迁移，路径处理已跨平台
- 打包: 推荐 AppImage (Linux 首选) + create-dmg (macOS) + Velopack (Windows 已有)

## Inputs

- `docs/cross-platform-research/electron-net-research.md`
- `docs/cross-platform-research/tauri-dotnet-research.md`
- `docs/cross-platform-research/avalonia-research.md`
- `docs/cross-platform-research/blazor-hybrid-research.md`
- `docs/cross-platform-research/web-app-research.md`
- `docs/cross-platform-research/maui-research.md`
- `docs/cross-platform-research/velopack-cross-platform.md`
- `docs/cross-platform-research/core-dependencies-compatibility.md`
- `docs/cross-platform-research/platform-differences.md`
- `docs/cross-platform-research/packaging-distribution.md`

## Expected Output

- `docs/cross-platform-research/comparison-and-recommendation.md`

## Verification

bash -c 'f="docs/cross-platform-research/comparison-and-recommendation.md" && test -f "$f" && echo "File exists" && grep -c "^## " "$f" | xargs -I{} bash -c "[ {} -ge 10 ] && echo \"Sections: {} (>=10 OK)\" || echo \"FAIL: only {} sections\"" && ! grep -q "TBD\|TODO" "$f" && echo "No TBD/TODO" && wc -c < "$f" | xargs -I{} bash -c "[ {} -ge 10000 ] && echo \"Size: {} bytes (>=10K OK)\" || echo \"FAIL: only {} bytes\"" && grep -c "Avalonia" "$f" | xargs -I{} echo "Avalonia refs: {}" && grep -c "推荐排序\|综合评分\|SWOT" "$f" | xargs -I{} echo "Key sections: {} refs"'
