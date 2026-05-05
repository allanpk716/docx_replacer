# S03: 纯文献调研（Avalonia/Blazor Hybrid/Web/MAUI） — UAT

**Milestone:** M022
**Written:** 2026-05-04T16:50:13.297Z

# S03: 纯文献调研（Avalonia/Blazor Hybrid/Web/MAUI）— UAT

**Milestone:** M022
**Written:** 2026-05-04

## UAT Type

- UAT mode: artifact-driven
- Why this mode is sufficient: 本 slice 产出为纯文本调研文档，无运行时代码。验证重点在于文档完整性、格式一致性和内容质量。

## Preconditions

- docs/cross-platform-research/ 目录存在
- 四份调研文档已生成

## Smoke Test

打开任意一份调研文档，确认包含调研日期、信息来源、成熟度评估等关键章节。

## Test Cases

### 1. 文档完整性检查

1. 检查四份文档均存在：avalonia-research.md、blazor-hybrid-research.md、web-app-research.md、maui-research.md
2. **Expected:** 四个文件均存在于 docs/cross-platform-research/ 目录下

### 2. 章节覆盖检查

1. 对每份文档统计 `^## ` 二级标题数量
2. **Expected:** 每份文档 ≥ 8 个二级章节

### 3. 内容完成度检查

1. 对每份文档搜索 TBD 和 TODO 关键词
2. **Expected:** 无匹配结果（无占位符）

### 4. 字数要求检查

1. 对每份文档统计英文单词数（wc -w）
2. **Expected:** 每份文档 ≥ 3000 字

### 5. 章节结构一致性

1. 对比四份文档的二级标题结构
2. **Expected:** 均包含技术概述、DocuFiller 适配性、跨平台支持、优缺点、成熟度评估等核心章节

### 6. 信息来源标注

1. 检查每份文档末尾是否有信息来源章节
2. **Expected:** 每份文档均有明确的调研日期和参考来源列表

## Edge Cases

### 文档编码
1. 以 UTF-8 编码打开所有文档
2. **Expected:** 中文和英文内容均正常显示，无乱码

## Failure Signals

- 文件不存在或路径错误
- 章节数少于 8 个
- 包含 TBD/TODO 占位符
- 字数不足 3000
- 文档间格式差异过大

## Not Proven By This UAT

- 文档中技术信息的时效性和准确性（信息可能随时间变化）
- 跨方案对比的公正性（由 S05 总结评估覆盖）
- 调研结论在实际项目中的适用性（需要 PoC 验证）

## Notes for Tester

- 四份文档以 electron-net-research.md（S01 产出）为格式参考
- 综合评分排序：Avalonia (4.3/5) > Blazor Hybrid (3.7/5) > Web (3.0/5) > MAUI (2.8/5)
- 所有文档均为纯文献调研，不涉及代码实现
