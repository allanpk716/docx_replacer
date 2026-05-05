# S04: 通用课题调研（Velopack/核心库/平台差异/打包分发）

**Goal:** 调研跨平台迁移的四个基础设施课题，产出四份独立调研文档：(1) Velopack 跨平台能力、(2) 核心依赖库兼容性、(3) 平台差异处理、(4) 打包分发方案。所有文档写入 docs/cross-platform-research/，供 S05 对比评估使用。
**Demo:** 四份基础设施调研文档

## Must-Haves

- `docs/cross-platform-research/velopack-cross-platform.md` 覆盖 Velopack 在 Windows/macOS/Linux 三平台的更新能力、工具链、局限性分析，≥8 个章节
- `docs/cross-platform-research/core-dependencies-compatibility.md` 覆盖 DocumentFormat.OpenXml 3.0.1、EPPlus 7.5.2、CommunityToolkit.Mvvm 8.4.0 等核心依赖的跨平台兼容性，≥8 个章节
- `docs/cross-platform-research/platform-differences.md` 覆盖文件对话框、拖放、路径处理、文件系统权限等平台差异及跨平台方案，≥8 个章节
- `docs/cross-platform-research/packaging-distribution.md` 覆盖 macOS dmg/notarization、Linux deb/AppImage、Windows 现有 Velopack 流程等打包分发方案，≥8 个章节
- 四份文档均无 TBD/TODO 占位符，均标注调研日期和信息来源

## Verification

- Run the task and slice verification checks for this slice.

## Tasks

- [ ] **T01: 调研 Velopack 跨平台能力并撰写 velopack-cross-platform.md** `est:1.5h`
  调研 Velopack 在 Windows、macOS、Linux 三平台上的自动更新能力。DocuFiller 当前使用 Velopack 0.0.1298（仅 Windows），需要了解跨平台迁移后 Velopack 能否继续作为统一更新框架。
  - Files: `docs/cross-platform-research/velopack-cross-platform.md`
  - Verify: bash -c 'FILE="docs/cross-platform-research/velopack-cross-platform.md" && test -f "$FILE" && grep -c "^## " "$FILE" | grep -q "^[8-9]\|[1-9][0-9]$" && ! grep -q "TBD\|TODO" "$FILE" && wc -w "$FILE" | awk "{if(\$1 >= 3000) exit 0; else exit 1}"'

- [ ] **T02: 调研核心依赖库跨平台兼容性并撰写 core-dependencies-compatibility.md** `est:1.5h`
  调研 DocuFiller 所有核心 NuGet 依赖的跨平台兼容性。重点分析 DocumentFormat.OpenXml 3.0.1、EPPlus 7.5.2、CommunityToolkit.Mvvm 8.4.0、Microsoft.Extensions.* 等库。
  - Files: `docs/cross-platform-research/core-dependencies-compatibility.md`
  - Verify: bash -c 'FILE="docs/cross-platform-research/core-dependencies-compatibility.md" && test -f "$FILE" && grep -c "^## " "$FILE" | grep -q "^[8-9]\|[1-9][0-9]$" && ! grep -q "TBD\|TODO" "$FILE" && wc -w "$FILE" | awk "{if(\$1 >= 3000) exit 0; else exit 1}"'

- [ ] **T03: 调研平台差异处理并撰写 platform-differences.md** `est:1.5h`
  调研 DocuFiller 跨平台迁移时需要处理的操作系统差异：文件对话框、拖放、路径处理、文件系统权限、注册表、进程管理等。
  - Files: `docs/cross-platform-research/platform-differences.md`
  - Verify: bash -c 'FILE="docs/cross-platform-research/platform-differences.md" && test -f "$FILE" && grep -c "^## " "$FILE" | grep -q "^[8-9]\|[1-9][0-9]$" && ! grep -q "TBD\|TODO" "$FILE" && wc -w "$FILE" | awk "{if(\$1 >= 3000) exit 0; else exit 1}"'

- [ ] **T04: 调研打包分发方案并撰写 packaging-distribution.md** `est:1.5h`
  调研 DocuFiller 在 macOS 和 Linux 上的打包分发方案：macOS dmg/notarization、Linux AppImage/deb、跨平台 CI/CD 等。
  - Files: `docs/cross-platform-research/packaging-distribution.md`
  - Verify: bash -c 'FILE="docs/cross-platform-research/packaging-distribution.md" && test -f "$FILE" && grep -c "^## " "$FILE" | grep -q "^[8-9]\|[1-9][0-9]$" && ! grep -q "TBD\|TODO" "$FILE" && wc -w "$FILE" | awk "{if(\$1 >= 3000) exit 0; else exit 1}"'

## Files Likely Touched

- docs/cross-platform-research/velopack-cross-platform.md
- docs/cross-platform-research/core-dependencies-compatibility.md
- docs/cross-platform-research/platform-differences.md
- docs/cross-platform-research/packaging-distribution.md
