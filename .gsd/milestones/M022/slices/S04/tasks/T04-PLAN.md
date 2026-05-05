---
estimated_steps: 12
estimated_files: 1
skills_used: []
---

# T04: 调研打包分发方案并撰写 packaging-distribution.md

**Slice:** S04 — 通用课题调研（Velopack/核心库/平台差异/打包分发）
**Milestone:** M022

## Description

调研 DocuFiller 在 macOS 和 Linux 上的打包分发方案。DocuFiller 当前在 Windows 上使用 Velopack 产出 Setup.exe + Portable.zip（通过 build-internal.bat 调用 vpk pack），GitHub Actions 自动化发布流程已就绪。跨平台后需要为每个平台提供合适的安装体验和自更新能力。

调研内容必须覆盖：
1. 调研概述（DocuFiller 当前 Windows 打包分发流程回顾）
2. macOS 打包方案（.app bundle 目录结构、Info.plist 配置、图标资源）
3. macOS 分发格式（dmg 制作：create-dmg/node-appdmg、zip 分发、Homebrew Cask）
4. macOS 代码签名与公证（Developer ID 证书、codesign 命令、notarytool/notaryship、Gatekeeper 兼容、Hardened Runtime）
5. Linux 打包方案（AppImage 制作流程：linuxdeploy/appimagetool、deb 包结构、rpm 包、Flatpak、Snap）
6. Linux 分发渠道（GitHub Releases、包仓库 PPA/OBS、Snap Store、Flathub）
7. 跨平台 CI/CD 集成（GitHub Actions 多平台 runner、矩阵构建、统一版本号、产物上传）
8. 跨平台自更新方案（Velopack 跨平台 + Sparkle for macOS + AppImageUpdate for Linux 的统一策略）
9. 对 DocuFiller 的建议（推荐的打包分发组合、CI/CD 流水线改造方案）
10. 优缺点总结
11. 调研日期与信息来源

关键调研方向：macOS 代码签名和公证是硬性要求——未签名的应用会被 Gatekeeper 阻止运行，这需要 Apple Developer 账号（$99/年）。Linux AppImage 是最简单的分发格式（单文件、免安装），但 Velopack 对 AppImage 的支持程度需要验证。

## Steps

1. 回顾 DocuFiller 当前 Windows 打包流程（Velopack + GitHub Actions）
2. 使用 web 搜索调研 macOS .app bundle 结构和 dmg 制作
3. 调研 macOS 代码签名和公证流程（codesign、notarytool、Developer ID）
4. 调研 Linux AppImage 制作流程和相关工具
5. 调研 Linux deb/rpm 包格式和分发渠道
6. 调研 GitHub Actions 多平台构建（macOS runner、Linux runner）
7. 调研跨平台自更新方案（Sparkle、AppImageUpdate）
8. 整理推荐方案和实施建议
9. 撰写完整调研文档

## Must-Haves

- [ ] 覆盖 macOS dmg/notarization 完整流程
- [ ] 覆盖 Linux AppImage 和 deb 打包方案
- [ ] 包含 CI/CD 多平台构建建议
- [ ] 包含跨平台自更新策略
- [ ] 包含对 DocuFiller 的具体实施建议
- [ ] 无 TBD/TODO 占位符

## Verification

- bash -c 'FILE="docs/cross-platform-research/packaging-distribution.md" && test -f "$FILE" && grep -c "^## " "$FILE" | grep -q "^[8-9]\|[1-9][0-9]$" && ! grep -q "TBD\|TODO" "$FILE" && wc -w "$FILE" | awk "{if(\$1 >= 3000) exit 0; else exit 1}"'

## Inputs

- `DocuFiller.csproj` — 项目框架信息

## Expected Output

- `docs/cross-platform-research/packaging-distribution.md` — 打包分发方案调研文档
