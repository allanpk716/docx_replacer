# S04: 通用课题调研（Velopack/核心库/平台差异/打包分发） — UAT

**Milestone:** M022
**Written:** 2026-05-04T17:20:48.549Z

# S04: 通用课题调研（Velopack/核心库/平台差异/打包分发） — UAT

**Milestone:** M022
**Written:** 2025-05-05

## UAT Type

- UAT mode: artifact-driven
- Why this mode is sufficient: S04 产出的是四份静态调研文档，无运行时代码。验证重点在于文件存在性、内容完整性、格式一致性和信息覆盖度。

## Preconditions

- docs/cross-platform-research/ 目录已创建
- S03 产出的调研文档（avalonia-research.md 等）可作为格式参考基准

## Smoke Test

打开 docs/cross-platform-research/ 目录，确认四份新文件（velopack-cross-platform.md、core-dependencies-compatibility.md、platform-differences.md、packaging-distribution.md）存在且文件大小合理（>25KB）。

## Test Cases

### 1. 文件存在性与大小验证

1. 检查 docs/cross-platform-research/ 下是否存在以下四份文件
2. 确认每份文件大小 > 25KB
3. **Expected:** 四份文件均存在，大小均在 25-50KB 范围

### 2. 章节结构验证

1. 对每份文档执行 `grep -c "^## "`
2. 确认每份文档的 ## 级标题数量 ≥ 8
3. **Expected:** velopack-cross-platform.md (13)、core-dependencies-compatibility.md (13)、platform-differences.md (13)、packaging-distribution.md (14)

### 3. 内容质量验证

1. 对每份文档搜索 TBD 和 TODO 关键字
2. 确认零匹配
3. **Expected:** 所有文档均无 TBD/TODO 标记

### 4. Velopack 调研覆盖度

1. 打开 velopack-cross-platform.md
2. 确认包含以下关键内容：Windows 现状分析、macOS 支持现状、Linux 支持现状、vpk CLI 工具链、增量更新分析、局限性讨论、DocuFiller 建议
3. **Expected:** 所有课题均有专门章节覆盖

### 5. 核心依赖兼容性覆盖度

1. 打开 core-dependencies-compatibility.md
2. 确认覆盖 DocumentFormat.OpenXml、EPPlus、CommunityToolkit.Mvvm、Microsoft.Extensions.DependencyInjection/Logging
3. 确认包含每个库的 net8.0 支持情况和平台特定限制分析
4. **Expected:** 核心业务库（OpenXml、EPPlus）确认纯托管实现、全平台兼容

### 6. 平台差异覆盖度

1. 打开 platform-differences.md
2. 确认覆盖六大差异领域：文件对话框、拖放、路径处理、文件系统权限、注册表、进程管理
3. 确认包含 DocuFiller 源码中具体代码点的修改清单
4. **Expected:** 六大差异领域均有章节覆盖，代码点清单具体到文件名和行号

### 7. 打包分发覆盖度

1. 打开 packaging-distribution.md
2. 确认覆盖 macOS 打包（dmg、签名、公证）、Linux 打包（AppImage、deb、rpm）、CI/CD 流水线、自更新机制
3. **Expected:** macOS 和 Linux 的主要打包格式均有分析，CI/CD 方案包含 GitHub Actions matrix 策略

### 8. 格式一致性

1. 对比 S03 产出的 avalonia-research.md 与 S04 四份文档
2. 确认格式一致：中文撰写、日期标注、目录、编号章节、信息来源
3. **Expected:** S04 文档格式与 S03 文档无明显差异

## Edge Cases

### 文档间信息一致性
1. 交叉检查 T01 Velopack 调研与 T04 打包分发中关于 Velopack 的描述
2. 交叉检查 T02 依赖兼容性与 T03 平台差异中关于 Services/ 层的分析
3. **Expected:** 文档间信息一致，无矛盾结论

## Failure Signals

- 任一文件缺失或大小 < 10KB（内容不足）
- 文档包含大量 TBD/TODO（调研未完成）
- 核心业务库（OpenXml、EPPlus）未覆盖或结论为"不兼容"
- 文档格式与 S03 产出明显不一致

## Not Proven By This UAT

- 调研信息的时效性（第三方库和工具的更新状态可能随时间变化）
- 实际跨平台构建和运行验证（留待 S05 和 PoC 阶段）
- macOS 代码签名和公证的实际操作流程（需要 Apple Developer 账号）
- Linux 各发行版的实际打包测试

## Notes for Tester

- 四份文档均为中文撰写，约 30-42KB，阅读每份约需 10-15 分钟
- 建议先阅读 T02（核心依赖兼容性）和 T03（平台差异），这两份对 S05 评估最关键
- T01 和 T04 存在信息重叠（都涉及 Velopack），这是预期的——T01 侧重更新能力，T04 侧重打包流程
