# DocuFiller 跨平台打包分发方案调研报告

> **调研日期**: 2026-05  
> **调研范围**: DocuFiller 从 Windows-only 迁移到跨平台（macOS + Linux）后的打包、签名、分发与自更新机制  
> **基于**: Velopack 官方文档、Apple 开发者文档、AppImage 规范、各包管理器官方文档、社区实践  
> **版本**: 1.0

---

## 目录

1. [调研概述](#1-调研概述)
2. [macOS 打包方案](#2-macos-打包方案)
3. [macOS 代码签名与公证](#3-macos-代码签名与公证)
4. [macOS 分发渠道](#4-macos-分发渠道)
5. [Linux 打包方案](#5-linux-打包方案)
6. [Linux 分发渠道](#6-linux-分发渠道)
7. [Velopack 跨平台打包集成](#7-velopack-跨平台打包集成)
8. [跨平台 CI/CD 构建](#8-跨平台-cicd-构建)
9. [统一版本号管理](#9-统一版本号管理)
10. [各平台自更新机制](#10-各平台自更新机制)
11. [对 DocuFiller 的建议方案](#11-对-docufiller-的建议方案)
12. [优缺点总结](#12-优缺点总结)
13. [调研日期与信息来源](#13-调研日期与信息来源)

---

## 1. 调研概述

### 1.1 调研背景

DocuFiller 当前使用 Velopack 在 Windows 上产出 `Setup.exe`（安装程序）和 `Portable.zip`（便携版）。跨平台迁移后，需要为 macOS 和 Linux 提供原生的安装体验：

| 平台 | 用户期望 | 当前 DocuFiller 方案 |
|------|---------|---------------------|
| **Windows** | Setup.exe / .msi 安装，或 Portable.zip | ✅ 已实现（Velopack） |
| **macOS** | .app bundle + .dmg 安装镜像，支持拖放到 Applications | ❌ 待实现 |
| **Linux** | AppImage（通用）、.deb（Debian/Ubuntu）、.rpm（Fedora/openSUSE） | ❌ 待实现 |

### 1.2 调研目标

1. 确定 macOS 和 Linux 上最佳的打包格式和制作工具
2. 明确代码签名和公证（macOS）的流程与成本
3. 评估各分发渠道的覆盖范围和维护成本
4. 设计统一的 CI/CD 构建流水线
5. 确定跨平台版本号管理和自更新策略

### 1.3 DocuFiller 当前打包配置

| 配置项 | 当前值 |
|--------|--------|
| **项目 SDK** | `Microsoft.NET.Sdk` |
| **目标框架** | `net8.0-windows` |
| **输出类型** | `WinExe` |
| **版本号** | `1.10.1`（SemVer 兼容） |
| **自动更新** | Velopack 0.0.1298 |

迁移后目标框架将变为 `net8.0`（或 `net8.0-desktop`），去掉 `-windows` 后缀，并使用所选跨平台 UI 框架（Avalonia/Tauri 等）的打包工具链。

---

## 2. macOS 打包方案

### 2.1 .app Bundle 结构

macOS 应用以 `.app` bundle 形式分发。Bundle 本质上是一个具有特定目录结构的文件夹，Finder 将其显示为单个图标：

```
DocuFiller.app/
├── Contents/
│   ├── Info.plist          # 应用元数据（名称、版本、权限声明）
│   ├── MacOS/
│   │   └── DocuFiller      # 主可执行文件
│   ├── Resources/
│   │   ├── AppIcon.icns    # 应用图标（ICNS 格式）
│   │   └── ...              # 其他资源文件
│   ├── Frameworks/          # 嵌入的 .NET 运行时和依赖库
│   │   ├── libhostfxr.dylib
│   │   ├── libcoreclr.dylib
│   │   └── ...
│   └── _CodeSignature/      # 代码签名（codesign 生成）
│       └── CodeResources
```

#### 2.1.1 Info.plist 关键字段

```xml
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN"
  "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleName</key>
    <string>DocuFiller</string>
    <key>CFBundleDisplayName</key>
    <string>DocuFiller</string>
    <key>CFBundleIdentifier</key>
    <string>com.docufiller.app</string>
    <key>CFBundleVersion</key>
    <string>1.10.1</string>
    <key>CFBundleShortVersionString</key>
    <string>1.10.1</string>
    <key>CFBundleExecutable</key>
    <string>DocuFiller</string>
    <key>CFBundleIconFile</key>
    <string>AppIcon</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>LSMinimumSystemVersion</key>
    <string>12.0</string>
    <key>NSHighResolutionCapable</key>
    <true/>
    <key>CFBundleDocumentTypes</key>
    <array>
        <dict>
            <key>CFBundleTypeName</key>
            <string>Word Document</string>
            <key>CFBundleTypeExtensions</key>
            <array><string>docx</string></array>
            <key>CFBundleTypeRole</key>
            <string>Editor</string>
        </dict>
    </array>
</dict>
</plist>
```

#### 2.1.2 .NET 发布为 .app Bundle

使用 `dotnet publish` 时可通过以下方式生成 bundle：

```bash
# 方式 1：直接发布，然后手动包装为 .app
dotnet publish -c Release -r osx-x64 -o publish
# 然后用脚本创建 .app 目录结构

# 方式 2：Velopack 自动创建 .app（推荐）
# Velopack 可从普通发布目录自动创建 .app bundle
# 只需提供 --icon 参数指向 .icns 文件
vpk pack \
  --packId DocuFiller \
  --packVersion 1.10.1 \
  --packDir publish \
  --mainExe DocuFiller \
  --icon Resources/AppIcon.icns \
  --packTitle "DocuFiller"
```

### 2.2 DMG 制作工具

DMG（Disk Image）是 macOS 上最常见的分发格式，用户下载后双击挂载，将 .app 拖放到 Applications 文件夹即可完成安装。

#### 2.2.1 create-dmg（推荐）

开源命令行工具，可创建带有背景图、快捷方式链接的精美 DMG：

```bash
# 安装
brew install create-dmg

# 使用
create-dmg \
  --volname "DocuFiller" \
  --volicon "Resources/AppIcon.icns" \
  --window-pos 200 120 \
  --window-size 600 400 \
  --icon-size 100 \
  --icon "DocuFiller.app" 175 190 \
  --app-drop-link 425 190 \
  --hide-extension "DocuFiller.app" \
  DocuFiller-1.10.1.dmg \
  publish/DocuFiller.app
```

**优点**：
- 完全命令行化，适合 CI/CD
- 可自定义窗口布局和背景图
- 支持代码签名后的 DMG

#### 2.2.2 node-appdmg

基于 Node.js 的 DMG 制作工具，通过 JSON 配置文件定义布局：

```json
{
  "title": "DocuFiller",
  "icon": "Resources/AppIcon.icns",
  "background": "assets/dmg-background.png",
  "icon-size": 80,
  "contents": [
    { "x": 192, "y": 344, "type": "file", "path": "DocuFiller.app" },
    { "x": 448, "y": 344, "type": "link", "path": "/Applications" }
  ]
}
```

```bash
npm install -g appdmg
appdmg dmg-config.json DocuFiller-1.10.1.dmg
```

#### 2.2.3 hdiutil（系统自带）

macOS 自带的磁盘镜像工具，功能原始但无需安装：

```bash
# 创建临时目录
mkdir -p dmg-temp
cp -R publish/DocuFiller.app dmg-temp/
ln -s /Applications dmg-temp/Applications

# 创建 DMG
hdiutil create -volname "DocuFiller" \
  -srcfolder dmg-temp \
  -ov -format UDZO \
  DocuFiller-1.10.1.dmg
```

#### 2.2.4 DMG 工具对比

| 工具 | 复杂度 | 美观度 | CI/CD 友好 | 安装依赖 |
|------|--------|--------|-----------|---------|
| **create-dmg** | 中 | 高 | ✅ 是 | Homebrew |
| **node-appdmg** | 中 | 高 | ✅ 是 | Node.js + npm |
| **hdiutil** | 低 | 低（无自定义） | ✅ 是 | 无（系统自带） |
| **Velopack 自动** | 低 | 中 | ✅ 是 | 无 |

**推荐方案**：Velopack 自动生成 .app bundle，然后使用 `create-dmg` 制作 DMG 安装镜像。

### 2.3 .icns 图标制作

macOS 应用图标需要 `.icns` 格式。可从 PNG 源图生成：

```bash
# 使用 iconutil（系统自带）
mkdir DocuFiller.iconset
# 需要提供 10 种尺寸的 PNG
sips -z 16 16     icon_512.png --out DocuFiller.iconset/icon_16x16.png
sips -z 32 32     icon_512.png --out DocuFiller.iconset/icon_16x16@2x.png
sips -z 32 32     icon_512.png --out DocuFiller.iconset/icon_32x32.png
sips -z 64 64     icon_512.png --out DocuFiller.iconset/icon_32x32@2x.png
sips -z 128 128   icon_512.png --out DocuFiller.iconset/icon_128x128.png
sips -z 256 256   icon_512.png --out DocuFiller.iconset/icon_128x128@2x.png
sips -z 256 256   icon_512.png --out DocuFiller.iconset/icon_256x256.png
sips -z 512 512   icon_512.png --out DocuFiller.iconset/icon_256x256@2x.png
sips -z 512 512   icon_512.png --out DocuFiller.iconset/icon_512x512.png
cp icon_512.png              DocuFiller.iconset/icon_512x512@2x.png
iconutil -c icns DocuFiller.iconset -o AppIcon.icns
```

也可使用在线工具（如 cloudconvert.com/png-to-icns）或第三方工具（如 `png2icns`）。

---

## 3. macOS 代码签名与公证

### 3.1 为什么需要签名和公证

从 macOS Sequoia (15) 开始，Apple 进一步收紧了安全策略：
- **未签名应用**：无法通过 Control+Click → Open 方式打开（此前可行）
- **未公证应用**：Gatekeeper 会阻止运行，提示"无法验证开发者"
- **签名 + 公证的应用**：用户可正常打开，Gatekeeper 显示绿色通过

**结论**：对于面向普通用户的桌面应用，代码签名和公证是**强制性要求**，不是可选项。

### 3.2 前置条件

| 条件 | 详情 |
|------|------|
| **Apple Developer 账号** | 需要付费账号，$99/年 |
| **Developer ID Application 证书** | 用于签名 .app bundle |
| **Developer ID Installer 证书** | 用于签名 .pkg 安装包 |
| **Xcode** | 需要安装 Xcode 及其命令行工具 |
| **App-Specific Password** | 用于 notarytool 自动化公证 |

### 3.3 代码签名流程

#### 3.3.1 创建签名证书

1. 登录 [Apple Developer 证书页面](https://developer.apple.com/account/resources/certificates)
2. 创建 `Developer ID Application` 证书（签名应用）
3. 创建 `Developer ID Installer` 证书（签名安装包）
4. 下载并双击安装到本地 Keychain

#### 3.3.2 签名命令

```bash
# 签名 .app bundle（含 hardened runtime）
codesign --force --deep --options runtime --timestamp \
  --entitlements Entitlements.plist \
  -s "Developer ID Application: Your Name (TEAMID)" \
  DocuFiller.app

# 验证签名
codesign --display --verbose DocuFiller.app
codesign --verify --deep --strict DocuFiller.app
```

#### 3.3.3 Entitlements 文件

DocuFiller 可能需要的权限声明：

```xml
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN"
  "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <!-- 沙盒（可选，非 App Store 分发可不启用） -->
    <!-- <key>com.apple.security.app-sandbox</key><true/> -->

    <!-- 文件访问（读写用户选择的文件） -->
    <key>com.apple.security.files.user-selected.read-write</key>
    <true/>

    <!-- 网络访问（用于自动更新下载） -->
    <key>com.apple.security.network.client</key>
    <true/>
</dict>
</plist>
```

> **注意**：DocuFiller 不在 Mac App Store 分发，可以**不启用沙盒**。不启用沙盒意味着应用有完整的文件系统访问权限，简化了文件操作权限管理。

### 3.4 公证（Notarization）流程

公证是 Apple 对已签名应用的云端安全扫描，确认无恶意代码后发放"公证票据"。

#### 3.4.1 设置 notarytool 凭据

```bash
# 创建 App-Specific Password（https://support.apple.com/en-us/102654）
# 将凭据存储到 Keychain
xcrun notarytool store-credentials "DocuFiller-Notary" \
  --apple-id "your@email.com" \
  --team-id "YOURTEAMID" \
  --password "xxxx-xxxx-xxxx-xxxx"
```

#### 3.4.2 提交公证

```bash
# 将 .app 打包为 zip 提交
/usr/bin/ditto -c -k --keepParent DocuFiller.app DocuFiller.zip

# 提交并等待结果
xcrun notarytool submit DocuFiller.zip \
  --keychain-profile "DocuFiller-Notary" \
  --wait

# 查看日志（如果需要调试）
xcrun notarytool log <submission-id> \
  --keychain-profile "DocuFiller-Notary" \
  notarization-log.json
```

#### 3.4.3 Staple 公证票据

```bash
# 将公证票据附加到 .app bundle
xcrun stapler staple DocuFiller.app

# 验证
spctl -a -vvv -t install DocuFiller.app
# 预期输出：source=Notarized Developer ID
```

### 3.5 Velopack 集成签名与公证

Velopack 可在 `vpk pack` 时自动完成签名和公证：

```bash
vpk pack \
  --packId DocuFiller \
  --packVersion 1.10.1 \
  --packDir publish \
  --mainExe DocuFiller \
  --icon AppIcon.icns \
  --packTitle "DocuFiller" \
  --signAppIdentity "Developer ID Application: Your Name (TEAMID)" \
  --signInstallIdentity "Developer ID Installer: Your Name (TEAMID)" \
  --notaryProfile "DocuFiller-Notary"
```

Velopack 会：
1. 自动对 .app bundle 内的所有二进制文件执行 `codesign`
2. 自动提交公证并等待完成
3. 自动 staple 公证票据

### 3.6 成本分析

| 项目 | 费用 | 备注 |
|------|------|------|
| Apple Developer 账号 | $99/年 | 必须，包含签名和公证权限 |
| Xcode | 免费 | 但占用 ~12GB 磁盘空间 |
| CI/CD（macOS runner） | GitHub Actions 免费 2000 分钟/月 | macOS runner 消耗 10x 分钟 |
| 公证 | 包含在 Developer 账号内 | 无额外费用 |

---

## 4. macOS 分发渠道

### 4.1 直接下载（DMG）

最简单直接的方式，用户从官网或 GitHub Releases 下载 DMG。

**流程**：
1. 构建 + 签名 + 公证 .app
2. 创建 DMG 安装镜像
3. 签名 DMG（`codesign -s "Developer ID Application: ..." DocuFiller.dmg`）
4. 上传到 GitHub Releases / CDN

**优点**：零审核周期，完全自主控制  
**缺点**：用户需要手动检查更新（或依赖 Velopack 自更新）

### 4.2 Homebrew Cask

Homebrew 是 macOS 上最流行的包管理器，通过 Cask 支持 GUI 应用分发。

#### 4.2.1 创建 Cask 定义

```ruby
cask "docufiller" do
  version "1.10.1"
  sha256 "checksum_of_dmg"

  url "https://github.com/user/DocuFiller/releases/download/v#{version}/DocuFiller-#{version}.dmg"
  name "DocuFiller"
  desc "Word文档批量填充工具"
  homepage "https://github.com/user/DocuFiller"

  depends_on macos: ">= :monterey"

  app "DocuFiller.app"

  zap trash: [
    "~/Library/Preferences/com.docufiller.app.plist",
    "~/Library/Application Support/DocuFiller",
    "~/Library/Caches/com.docufiller.app",
  ]
end
```

#### 4.2.2 分发方式

| 方式 | 审核 | 维护成本 |
|------|------|---------|
| **官方 homebrew-cask 仓库** | 需要 PR 审核 | 需要为每个版本提交 PR 更新 |
| **私有 Tap** | 无审核 | 自己维护 tap 仓库，用户通过 `brew tap` 添加 |

**推荐方案**：先使用私有 Tap（如 `brew tap yourname/tap`），社区成熟后再申请官方 Cask。

### 4.3 Mac App Store

DocuFiller 作为文档工具理论上可以上架 Mac App Store，但要求严格：

| 要求 | 影响 |
|------|------|
| 必须启用沙盒 | 文件访问受限，需要用户授权 |
| 不允许自更新机制 | 必须通过 App Store 更新 |
| 需要 App Store 审核 | 1-3 天审核周期 |
| 需要 App Store Connect 配置 | 额外的证书和 Provisioning Profile |
| 收入分成 30%（第一年后 15%） | 如果收费 |

**建议**：初期不进入 Mac App Store，以直接下载 + Homebrew Cask 为主。

---

## 5. Linux 打包方案

### 5.1 AppImage（推荐首选）

AppImage 是一种通用的 Linux 应用打包格式，一个文件即可运行，无需安装。

#### 5.1.1 工作原理

- 将应用及其所有依赖打包到一个单独的文件中
- 用户赋予执行权限后直接运行（`chmod +x && ./DocuFiller.AppImage`）
- 兼容大多数 Linux 发行版（基于 glibc 2.x）
- 不需要 root 权限

#### 5.1.2 AppDir 结构

```
DocuFiller.AppDir/
├── AppRun              # 启动脚本
├── docufiller.desktop  # 桌面入口文件
├── docufiller.png      # 应用图标（PNG 格式）
├── usr/
│   ├── bin/
│   │   └── DocuFiller  # 主可执行文件
│   └── lib/
│       └── ...          # .NET 运行时和依赖
└── ...
```

#### 5.1.3 制作工具

**Velopack 自动创建（推荐）**：

```bash
# Velopack 在 Linux 上自动产出 AppImage
vpk pack \
  --packId DocuFiller \
  --packVersion 1.10.1 \
  --packDir publish \
  --mainExe DocuFiller \
  --icon docufiller.png \
  --categories "Office;Utility;"
```

Velopack 会：
1. 自动创建 AppDir 结构
2. 自动调用 `appimagetool` 生成 AppImage
3. 嵌入 .desktop 文件和图标

**linuxdeploy（手动方案）**：

```bash
# 安装 linuxdeploy
wget https://github.com/linuxdeploy/linuxdeploy/releases/download/continuous/linuxdeploy-x86_64.AppImage
chmod +x linuxdeploy-x86_64.AppImage

# 部署应用
linuxdeploy-x86_64.AppImage \
  --appdir DocuFiller.AppDir \
  --executable publish/DocuFiller \
  --desktop-file docufiller.desktop \
  --icon-file docufiller.png \
  --output appimage
```

#### 5.1.4 .desktop 文件

```ini
[Desktop Entry]
Name=DocuFiller
Comment=Word文档批量填充工具
Exec=DocuFiller
Icon=docufiller
Type=Application
Categories=Office;Utility;
Terminal=false
MimeType=application/vnd.openxmlformats-officedocument.wordprocessingml.document;
```

#### 5.1.5 注意事项

| 问题 | 说明 |
|------|------|
| **glibc 版本** | AppImage 依赖宿主系统的 glibc，建议在较老的发行版（如 Ubuntu 18.04）上构建以确保兼容性 |
| **FUSE** | 某些系统需要安装 `libfuse2`（如 Ubuntu 22.04+：`sudo apt install libfuse2`） |
| **动态库** | .NET 应用使用自包含发布模式（`--self-contained true`），不依赖系统 .NET 运行时 |

### 5.2 deb 包（Debian/Ubuntu）

deb 是 Debian 系发行版（Ubuntu、Mint 等）的原生包格式。

#### 5.2.1 目录结构

```
docufiller_1.10.1_amd64/
├── DEBIAN/
│   ├── control          # 包元数据（必需）
│   ├── postinst         # 安装后脚本
│   └── postrm           # 卸载后脚本
└── usr/
    ├── bin/
    │   └── DocuFiller   # 主可执行文件
    ├── lib/
    │   └── docufiller/  # 应用文件（.NET 运行时 + 依赖）
    └── share/
        ├── applications/
        │   └── docufiller.desktop
        ├── icons/hicolor/256x256/apps/
        │   └── docufiller.png
        └── doc/docufiller/
            └── README.md
```

#### 5.2.2 control 文件

```
Package: docufiller
Version: 1.10.1
Section: utils
Priority: optional
Architecture: amd64
Depends: libicu-dev, libssl-dev
Maintainer: Allan <allan@example.com>
Description: DocuFiller - Word文档批量填充工具
 DocuFiller 是一个用于批量填充 Word 文档模板的桌面应用。
 支持从 Excel 数据源批量生成文档。
Homepage: https://github.com/user/DocuFiller
```

#### 5.2.3 构建 deb 包

```bash
# 使用 dpkg-deb 构建
dpkg-deb --build docufiller_1.10.1_amd64

# 验证
dpkg-deb --info docufiller_1.10.1_amd64.deb
lintian docufiller_1.10.1_amd64.deb
```

### 5.3 rpm 包（Fedora/openSUSE）

rpm 是 Red Hat 系发行版（Fedora、CentOS、openSUSE）的原生包格式。

#### 5.3.1 RPM Spec 文件

```spec
Name:           docufiller
Version:        1.10.1
Release:        1%{?dist}
Summary:        DocuFiller - Word文档批量填充工具

License:        MIT
URL:            https://github.com/user/DocuFiller
Source0:        docufiller-%{version}.tar.gz

BuildArch:      x86_64
Requires:       libicu, openssl-libs

%description
DocuFiller 是一个用于批量填充 Word 文档模板的桌面应用。

%install
mkdir -p %{buildroot}/usr/bin
mkdir -p %{buildroot}/usr/lib/docufiller
mkdir -p %{buildroot}/usr/share/applications
mkdir -p %{buildroot}/usr/share/icons/hicolor/256x256/apps

cp -r publish/* %{buildroot}/usr/lib/docufiller/
ln -s /usr/lib/docufiller/DocuFiller %{buildroot}/usr/bin/DocuFiller
cp docufiller.desktop %{buildroot}/usr/share/applications/
cp docufiller.png %{buildroot}/usr/share/icons/hicolor/256x256/apps/

%files
/usr/bin/DocuFiller
/usr/lib/docufiller/*
/usr/share/applications/docufiller.desktop
/usr/share/icons/hicolor/256x256/apps/docufiller.png

%post
update-desktop-database &>/dev/null || :

%postun
update-desktop-database &>/dev/null || :
```

#### 5.3.2 构建 rpm 包

```bash
rpmbuild -bb docufiller.spec
```

### 5.4 Flatpak

Flatpak 是一种沙盒化的 Linux 应用打包格式，强调安全性和跨发行版兼容。

#### 5.4.1 优点

- 强沙盒隔离，安全性高
- Flathub 分发，覆盖面广
- 支持自动更新

#### 5.4.2 缺点（对 DocuFiller 的挑战）

- **沙盒限制**：文件系统访问受限，需要配置 portal 权限
- **复杂度高**：需要编写 manifest YAML，配置 build-options
- **运行时依赖**：必须基于 Flatpak runtime（如 `org.freedesktop.Platform`）
- **.NET 应用适配**：需要额外配置 .NET 运行时环境

#### 5.4.3 基础 Manifest 示例

```yaml
app-id: com.docufiller.app
runtime: org.freedesktop.Platform
runtime-version: '23.08'
sdk: org.freedesktop.Sdk
command: DocuFiller
finish-args:
  - --share=network          # 网络访问（自动更新）
  - --filesystem=home:rw     # 读写用户主目录
  - --filesystem=/tmp:rw     # 读写临时目录
modules:
  - name: docufiller
    buildsystem: simple
    build-commands:
      - cp -r publish/* /app/
      - ln -s /app/DocuFiller /app/bin/DocuFiller
    sources:
      - type: dir
        path: publish
```

**建议**：初期不优先支持 Flatpak，待 AppImage 和 deb 成熟后再考虑。

### 5.5 Snap

Snap 是 Canonical 推出的跨发行版包格式。

#### 5.5.1 优点

- 自动更新（后台静默）
- Snap Store 分发
- 沙盒隔离

#### 5.5.2 缺点

- **启动速度慢**：首次启动需挂载 squashfs
- **争议性**：社区对 Snap 接受度分化（Ubuntu 强推，其他发行版抵制）
- **Snap Store 闭源**：分发渠道单一
- **需要 snapcraft 工具**：构建环境配置较复杂

**建议**：不优先支持 Snap，投入产出比低于 AppImage + deb。

### 5.6 Linux 打包格式对比

| 格式 | 覆盖面 | 用户体验 | 维护成本 | 自更新 | DocuFiller 优先级 |
|------|--------|---------|---------|--------|-----------------|
| **AppImage** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ✅ Velopack | **首选** |
| **deb** | ⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐ | ✅ Velopack | **次选** |
| **rpm** | ⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐ | ✅ Velopack | 第三 |
| **Flatpak** | ⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐ | ✅ 内置 | 后续考虑 |
| **Snap** | ⭐⭐⭐ | ⭐⭐ | ⭐⭐ | ✅ 内置 | 暂不考虑 |

---

## 6. Linux 分发渠道

### 6.1 GitHub Releases（推荐首选）

直接在 GitHub Releases 上发布 AppImage 和 deb 文件。

**优点**：
- 零成本，无需审核
- Velopack 的 `releases.json` 可直接托管在 GitHub Pages
- 用户下载即可使用

**流程**：
```bash
# 构建 AppImage
vpk pack --packId DocuFiller --packVersion 1.10.1 \
  --packDir publish --mainExe DocuFiller --icon docufiller.png

# 上传到 GitHub Releases
gh release create v1.10.1 \
  ./DocuFiller-1.10.1-x86_64.AppImage \
  ./DocuFiller-1.10.1-amd64.deb \
  --title "DocuFiller v1.10.1" \
  --notes-file CHANGELOG.md
```

### 6.2 AUR（Arch Linux User Repository）

Arch Linux 的社区包仓库，维护 PKGBUILD 脚本：

```bash
# PKGBUILD 示例
pkgname=docufiller-bin
pkgver=1.10.1
pkgrel=1
pkgdesc="Word文档批量填充工具"
arch=('x86_64')
url="https://github.com/user/DocuFiller"
source=("$url/releases/download/v$pkgver/DocuFiller-$pkgver-x86_64.AppImage")
sha256sums=('...')
package() {
    install -Dm755 "$srcdir/DocuFiller-$pkgver-x86_64.AppImage" \
        "$pkgdir/usr/bin/DocuFiller"
}
```

**维护成本**：需要在每次发版后更新 PKGBUILD 的版本和校验和。可通过 GitHub Actions 自动化。

### 6.3 PPA（Personal Package Archive）

Ubuntu 的第三方软件仓库，可让用户通过 `apt` 安装和自动更新。

```bash
# 用户安装
sudo add-apt-repository ppa:yourname/docufiller
sudo apt update
sudo apt install docufiller
```

**维护成本**：需要 Launchpad 账号，每次发版需要上传源码/二进制到 PPA。对 .NET 自包含应用不太友好（包体积大，~80MB+）。

### 6.4 Onerp / AppImageHub

将 AppImage 提交到 AppImageHub（https://appimagehub.com/）等目录站，增加发现度。

---

## 7. Velopack 跨平台打包集成

### 7.1 Velopack 跨平台能力总结

根据 T01 调研报告，Velopack 对三个平台的原生支持：

| 平台 | 安装包格式 | 自更新 | 签名集成 | 状态 |
|------|-----------|--------|---------|------|
| **Windows** | Setup.exe + Portable.zip | ✅ | ✅ signtool / Azure | 成熟 |
| **macOS** | .app bundle + .pkg | ✅ | ✅ codesign + notarytool | 成熟 |
| **Linux** | AppImage | ✅ | — | 稳定 |

### 7.2 跨平台打包命令

```bash
# Windows（在 Windows 上运行）
dotnet publish -c Release -r win-x64 --self-contained -o publish/win
vpk pack --packId DocuFiller --packVersion 1.10.1 \
  --packDir publish/win --mainExe DocuFiller.exe \
  --packTitle "DocuFiller" --icon Resources/app.ico

# macOS（在 macOS 上运行，或交叉编译）
dotnet publish -c Release -r osx-x64 --self-contained -o publish/osx
vpk pack --packId DocuFiller --packVersion 1.10.1 \
  --packDir publish/osx --mainExe DocuFiller \
  --icon Resources/AppIcon.icns \
  --packTitle "DocuFiller" \
  --signAppIdentity "Developer ID Application: Your Name (TEAMID)" \
  --signInstallIdentity "Developer ID Installer: Your Name (TEAMID)" \
  --notaryProfile "DocuFiller-Notary"

# Linux（在 Linux 上运行，或交叉编译）
dotnet publish -c Release -r linux-x64 --self-contained -o publish/linux
vpk pack --packId DocuFiller --packVersion 1.10.1 \
  --packDir publish/linux --mainExe DocuFiller \
  --icon Resources/docufiller.png \
  --packTitle "DocuFiller" \
  --categories "Office;Utility;"
```

### 7.3 交叉编译

Velopack 支持在任意平台上为其他平台打包：

```bash
# 在 Windows 上为 Linux 打包
vpk [linux] pack --packId DocuFiller --packVersion 1.10.1 \
  --packDir publish/linux --mainExe DocuFiller \
  --runtime linux-x64 --icon docufiller.png

# 在 Windows 上为 macOS 打包（但签名需要在 macOS 上完成）
vpk [osx] pack --packId DocuFiller --packVersion 1.10.1 \
  --packDir publish/osx --mainExe DocuFiller \
  --runtime osx-x64 --icon AppIcon.icns
```

> **注意**：macOS 的代码签名和公证必须在 macOS 上完成（依赖 `codesign` 和 `notarytool`）。交叉编译可以生成未签名的包，然后在 macOS 上补充签名。

### 7.4 Releases Feed

Velopack 使用 `releases.json`（或 `releases.stable.json`）作为更新源：

```json
{
  "name": "1.10.1",
  "notes": "Bug fixes and improvements",
  "pub_date": "2026-05-01T00:00:00Z",
  "version": "1.10.1",
  "url": "https://github.com/user/DocuFiller/releases/download/1.10.1/DocuFiller-1.10.1-osx-x64.zip",
  "packages": {
    "osx-x64": {
      "url": "https://github.com/user/DocuFiller/releases/download/1.10.1/DocuFiller-1.10.1-osx-x64.nupkg",
      "size": 85000000
    },
    "linux-x64": {
      "url": "https://github.com/user/DocuFiller/releases/download/1.10.1/DocuFiller-1.10.1-linux-x64.nupkg",
      "size": 82000000
    },
    "win-x64": {
      "url": "https://github.com/user/DocuFiller/releases/download/1.10.1/DocuFiller-1.10.1-win-x64.nupkg",
      "size": 78000000
    }
  }
}
```

Velopack SDK 会根据运行时平台自动选择对应的包进行下载更新。

---

## 8. 跨平台 CI/CD 构建

### 8.1 GitHub Actions 多平台构建

GitHub Actions 提供 Windows、macOS、Linux 三种 runner，可使用 matrix 策略并行构建：

```yaml
name: Build and Package

on:
  push:
    tags: ['v*']

jobs:
  build:
    strategy:
      fail-fast: false
      matrix:
        include:
          - os: windows-latest
            rid: win-x64
            ext: .exe
            icon: Resources/app.ico
          - os: macos-latest
            rid: osx-x64
            ext: ''
            icon: Resources/AppIcon.icns
          - os: ubuntu-latest
            rid: linux-x64
            ext: ''
            icon: Resources/docufiller.png

    runs-on: ${{ matrix.os }}

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'

      - name: Publish
        run: |
          dotnet publish DocuFiller.csproj \
            -c Release \
            -r ${{ matrix.rid }} \
            --self-contained true \
            -p:PublishSingleFile=false \
            -p:IncludeNativeLibrariesForSelfExtract=true \
            -o publish

      - name: Install Velopack
        run: dotnet tool install -g vpk

      - name: Package (Windows)
        if: runner.os == 'Windows'
        run: |
          vpk pack --packId DocuFiller --packVersion ${{ github.ref_name }} `
            --packDir publish --mainExe DocuFiller.exe `
            --packTitle "DocuFiller" --icon ${{ matrix.icon }}
        # 可选：--signParams 或 --azureTrustedSignFile

      - name: Package (macOS)
        if: runner.os == 'macOS'
        run: |
          vpk pack --packId DocuFiller --packVersion ${{ github.ref_name }} \
            --packDir publish --mainExe DocuFiller \
            --packTitle "DocuFiller" --icon ${{ matrix.icon }} \
            --signAppIdentity "$APPLE_SIGN_IDENTITY" \
            --notaryProfile "$APPLE_NOTARY_PROFILE"
        env:
          APPLE_SIGN_IDENTITY: ${{ secrets.APPLE_SIGN_IDENTITY }}
          APPLE_NOTARY_PROFILE: ${{ secrets.APPLE_NOTARY_PROFILE }}

      - name: Package (Linux)
        if: runner.os == 'Linux'
        run: |
          vpk pack --packId DocuFiller --packVersion ${{ github.ref_name }} \
            --packDir publish --mainExe DocuFiller \
            --packTitle "DocuFiller" --icon ${{ matrix.icon }} \
            --categories "Office;Utility;"

      - name: Upload Artifacts
        uses: actions/upload-artifact@v4
        with:
          name: DocuFiller-${{ matrix.rid }}
          path: Releases/*

  release:
    needs: build
    runs-on: ubuntu-latest
    steps:
      - name: Download all artifacts
        uses: actions/download-artifact@v4

      - name: Create GitHub Release
        uses: softprops/action-gh-release@v2
        with:
          files: |
            DocuFiller-win-x64/*
            DocuFiller-osx-x64/*
            DocuFiller-linux-x64/*
```

### 8.2 macOS Runner 注意事项

| 注意点 | 说明 |
|--------|------|
| **分钟消耗** | macOS runner 消耗 10x 分钟（1 分钟 = 10 分钟配额） |
| **签名证书** | 需通过 Keychain 导入，存储为 GitHub Secrets（base64 编码） |
| **notarytool 凭据** | 需要存储为 Secrets，CI 中恢复到 Keychain |
| **Xcode 版本** | GitHub macOS runner 已预装 Xcode |

### 8.3 构建 Docker 化（Linux）

对于 Linux 包，可使用 Docker 确保构建环境一致性：

```dockerfile
FROM ubuntu:18.04

# 安装 .NET SDK 8.0
RUN apt-get update && apt-get install -y \
    wget \
    libicu-dev \
    && wget https://dot.net/v1/dotnet-install.sh \
    && ./dotnet-install.sh --channel 8.0 \
    && ln -s ~/.dotnet/dotnet /usr/local/bin/dotnet

# 安装 Velopack
RUN dotnet tool install -g vpk
ENV PATH="$PATH:/root/.dotnet/tools"

WORKDIR /src
COPY . .

# 发布
RUN dotnet publish DocuFiller.csproj \
    -c Release -r linux-x64 --self-contained -o publish

# 打包 AppImage
RUN vpk pack --packId DocuFiller --packVersion 1.10.1 \
    --packDir publish --mainExe DocuFiller \
    --icon Resources/docufiller.png \
    --categories "Office;Utility;"
```

> **关键**：使用 Ubuntu 18.04 作为基础镜像，确保构建产物兼容更老的 glibc 版本。

---

## 9. 统一版本号管理

### 9.1 当前方案

DocuFiller 使用 `<Version>` 标签管理版本号：

```xml
<Version>1.10.1</Version>
<FileVersion>1.10.1.0</FileVersion>
```

### 9.2 跨平台版本号要求

| 平台 | 格式要求 | 说明 |
|------|---------|------|
| **Velopack** | SemVer2（`1.10.1`、`1.0.0-beta.1`） | 不支持四段版本号（`1.0.0.0`） |
| **macOS Info.plist** | `CFBundleShortVersionString` | 推荐 SemVer |
| **macOS .pkg** | SemVer 或 Apple 版本格式 | — |
| **deb** | `upstream_version` | Debian Policy 推荐兼容 SemVer |
| **rpm** | `Version:Tag` | 同上 |

### 9.3 推荐方案：单一版本源

在项目根目录维护一个 `version.json` 文件作为唯一版本源：

```json
{
  "version": "1.10.1",
  "assemblyVersion": "1.0.0.0"
}
```

CI/CD 流水线中读取此文件并注入到各构建步骤：

```yaml
- name: Read version
  id: version
  run: |
    VERSION=$(jq -r '.version' version.json)
    echo "version=$VERSION" >> $GITHUB_OUTPUT

- name: Package
  run: |
    vpk pack --packVersion ${{ steps.version.outputs.version }} ...
```

### 9.4 版本号策略建议

| 场景 | 格式 | 示例 |
|------|------|------|
| **稳定版** | `MAJOR.MINOR.PATCH` | `1.10.1` |
| **预发布** | `MAJOR.MINOR.PATCH-pre.N` | `2.0.0-pre.1` |
| **构建号** | `MAJOR.MINOR.PATCH+build.NNN` | `1.10.1+build.42` |

> **注意**：Velopack `--packVersion` 只接受 SemVer2 格式，不支持 .NET 的四段版本号。DocuFiller 当前 `<AssemblyVersion>1.0.0.0</AssemblyVersion>` 不受影响——WPF BAML 硬编码了此值，不可修改（参见 csproj 注释）。

---

## 10. 各平台自更新机制

### 10.1 Velopack 统一自更新

DocuFiller 已集成 Velopack SDK，跨平台后继续使用 Velopack 作为统一更新框架：

```csharp
// 应用启动时检查更新（跨平台通用）
VelopackApp.Build()
    .WithFirstRun(v => { /* 首次运行逻辑 */ })
    .Run();

// 手动触发更新检查
var mgr = new UpdateManager("https://releases.docufiller.com/");
var info = await mgr.CheckForUpdatesAsync();
if (info != null)
{
    await mgr.DownloadUpdatesAsync(info);
    mgr.ApplyUpdatesAndRestart(info);
}
```

**Velopack 各平台更新行为**：

| 平台 | 下载位置 | 应用方式 | 特权提升 |
|------|---------|---------|---------|
| **Windows** | `%LOCALAPPDATA%\DocuFiller\updates` | 替换文件 + 重启 | 按需 UAC |
| **macOS** | `/tmp` | 替换 .app bundle + 重启 | AppleScript 请求提权（如果安装在 `/Applications`） |
| **Linux** | `/var/tmp` | 替换 AppImage 文件 | `pkexec` 请求 sudo（如果在特权目录） |

### 10.2 Sparkle（macOS 备选方案）

Sparkle 是 macOS 上最广泛使用的自更新框架（主要用于 Cocoa 应用）。

**特点**：
- 原生 Cocoa UI 更新提示
- 支持 delta 更新
- 支持 Appcast (RSS) 更新源
- 可与 .NET 应用集成（但需要额外绑定层）

**对 DocuFiller 的适用性**：❌ 不推荐。Velopack 已内置 macOS 更新支持，无需引入第二套更新框架。

### 10.3 AppImageUpdate（Linux 备选方案）

AppImageUpdate 是 AppImage 生态的增量更新工具。

**特点**：
- 基于 zsync2 的二进制增量更新
- 不需要服务器端计算差分
- 用户可手动检查更新

**对 DocuFiller 的适用性**：❌ 不推荐。Velopack 已内置 AppImage 更新支持，且提供更好的增量更新（Zstandard 算法）。

### 10.4 自更新方案对比

| 方案 | 平台 | 增量更新 | 集成难度 | DocuFiller 推荐 |
|------|------|---------|---------|----------------|
| **Velopack SDK** | Win + macOS + Linux | ✅ Zstd | 已集成 | ✅ 是 |
| **Sparkle** | macOS | ✅ | 需额外绑定 | ❌ 否 |
| **AppImageUpdate** | Linux | ✅ zsync | 需额外集成 | ❌ 否 |
| **各商店内置更新** | MAS / Snap / Flathub | ✅ | 无需集成 | 后续（如果进商店） |

---

## 11. 对 DocuFiller 的建议方案

### 11.1 推荐打包策略

| 平台 | 首选格式 | 安装体验 | 更新机制 |
|------|---------|---------|---------|
| **Windows** | Setup.exe + Portable.zip | 一键安装 / 解压即用 | Velopack |
| **macOS** | .app bundle（DMG 分发） | 拖放安装 + 签名公证 | Velopack |
| **Linux** | AppImage | chmod +x && 运行 | Velopack |
| **Linux（补充）** | .deb | dpkg -i 安装 | Velopack |

### 11.2 分发策略

| 渠道 | 优先级 | 覆盖平台 | 自动化程度 |
|------|--------|---------|-----------|
| **GitHub Releases** | P0 | 全平台 | CI/CD 全自动 |
| **Homebrew Cask（私有 Tap）** | P1 | macOS | 半自动（需更新 formula） |
| **AUR** | P2 | Arch Linux | 半自动 |
| **Flathub** | P3 | Linux（Flatpak） | 手动 |
| **Mac App Store** | P4 | macOS | 手动 + 审核 |
| **Snap Store** | P5 | Linux（Snap） | 手动 |

### 11.3 CI/CD 流水线设计

```
Tag Push (v*)
    │
    ├─→ Windows Runner ─→ dotnet publish ─→ vpk pack ─→ Upload
    │
    ├─→ macOS Runner ─→ dotnet publish ─→ vpk pack + sign + notarize ─→ Upload
    │
    └─→ Linux Runner ─→ dotnet publish ─→ vpk pack ─→ Upload
                                                              │
                                                              ▼
                                                     GitHub Release
                                                     (含三平台产物)
                                                              │
                                                     ┌────────┼────────┐
                                                     ▼        ▼        ▼
                                                  Updates   Homebrew   AUR
                                                  Feed      Tap       PKGBUILD
```

### 11.4 实施路线图

| 阶段 | 内容 | 预计工期 |
|------|------|---------|
| **Phase 1** | Windows + Linux AppImage（无签名） | 1 周 |
| **Phase 2** | macOS .app + DMG（含签名公证） | 1-2 周 |
| **Phase 3** | Linux .deb 包 | 3-5 天 |
| **Phase 4** | Homebrew Cask + AUR | 2-3 天 |
| **Phase 5** | Flatpak（可选） | 1-2 周 |

### 11.5 关键决策点

1. **Apple Developer 账号**：需要决定是否投资 $99/年进行 macOS 签名公证。如果不签名，macOS 用户需要通过终端 `xattr -cr DocuFiller.app` 来绕过 Gatekeeper，体验较差。
2. **自包含 vs 框架依赖**：建议使用自包含发布（`--self-contained true`），避免要求用户预装 .NET 运行时。代价是包体积增大（~80MB vs ~5MB），但换来零依赖安装体验。
3. **Linux glibc 兼容性**：建议在 Ubuntu 18.04 Docker 容器中构建 Linux 产物，以确保最大兼容性。

---

## 12. 优缺点总结

### 12.1 方案优势

| 优势 | 说明 |
|------|------|
| **Velopack 统一更新** | 三平台共用一套更新框架，无需维护多套更新逻辑 |
| **AppImage 零安装** | Linux 用户下载即用，无依赖问题 |
| **CI/CD 全自动** | 一个 tag push 触发三平台构建，自动发布到 GitHub Releases |
| **SemVer2 统一版本** | 三平台使用同一版本号，减少版本管理混乱 |
| **交叉编译支持** | Velopack 支持在任意平台上为其他平台打包 |

### 12.2 方案挑战

| 挑战 | 说明 | 缓解措施 |
|------|------|---------|
| **macOS 签名成本** | $99/年 Apple Developer 账号 | 初期可先发未签名版，成熟后再投资 |
| **包体积大** | 自包含发布 ~80-100MB | Velopack 增量更新减少下载量 |
| **Linux 发行版碎片化** | deb/rpm 需分别维护 | 以 AppImage 为主，deb 为辅 |
| **macOS runner 分钟消耗** | 10x 分钟消耗 | 优化构建步骤，缓存依赖 |
| **notarytool 等待时间** | 公证通常需 5-15 分钟 | CI 中使用 `--wait` 异步处理 |

### 12.3 风险评估

| 风险 | 概率 | 影响 | 缓解措施 |
|------|------|------|---------|
| macOS Sequoia+ 更严格安全策略 | 高 | 高 | 必须签名公证 |
| AppImage FUSE 依赖问题 | 中 | 低 | 文档说明安装 libfuse2 |
| Velopack Linux 支持不完善 | 低 | 中 | 备选方案：手动制作 AppImage |
| GitHub Actions macOS runner 不可用 | 低 | 中 | 备选：MacStadium 或自托管 runner |

---

## 13. 调研日期与信息来源

### 13.1 调研信息

| 项目 | 数据 |
|------|------|
| **调研日期** | 2026 年 5 月 |
| **调研人** | AI Agent（基于公开文档和社区资料） |
| **DocuFiller 版本** | 1.10.1 |
| **Velopack 版本** | 0.0.1298 |

### 13.2 主要信息来源

| 来源 | URL |
|------|-----|
| **Velopack 官方文档** | https://docs.velopack.io/ |
| **Velopack macOS 打包** | https://docs.velopack.io/packaging/operating-systems/macos |
| **Velopack Linux 打包** | https://docs.velopack.io/packaging/operating-systems/linux |
| **Velopack 代码签名** | https://docs.velopack.io/packaging/signing |
| **Velopack 交叉编译** | https://docs.velopack.io/packaging/cross-compiling |
| **Apple Notarization 指南** | https://developer.apple.com/documentation/security/notarizing_macos_software_before_distribution |
| **Apple Hardened Runtime** | https://developer.apple.com/documentation/security/hardened_runtime |
| **AppImage 规范** | https://docs.appimage.org/ |
| **create-dmg** | https://github.com/create-dmg/create-dmg |
| **GitHub Actions Workflow 语法** | https://docs.github.com/actions/using-workflows/workflow-syntax-for-github-actions |
| **Debian Policy Manual** | https://www.debian.org/doc/debian-policy/ |
| **RPM 打包指南** | https://rpm-packaging-guide.github.io/ |
| **Flatpak 文档** | https://docs.flatpak.org/ |
| **Homebrew Cask 文档** | https://docs.brew.sh/Cask-Cookbook |

### 13.3 相关调研文档

| 文档 | 路径 |
|------|------|
| Velopack 跨平台能力调研 | `docs/cross-platform-research/velopack-cross-platform.md` |
| 核心依赖库兼容性调研 | `docs/cross-platform-research/core-dependencies-compatibility.md` |
| 平台差异处理调研 | `docs/cross-platform-research/platform-differences.md` |
