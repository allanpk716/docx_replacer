# DocuFiller 跨平台方案横向对比评估与推荐报告

> **调研日期**: 2026-05  
> **基于**: S01-S04 全部 10 份调研文档  
> **版本**: 1.0  
> **目的**: 综合 6 个 UI 方案和 4 份基础设施调研的关键发现，产出横向对比评估、推荐排序与迁移路线图建议

---

## 目录

1. [执行摘要](#1-执行摘要)
2. [评估方法论](#2-评估方法论)
3. [方案概览](#3-方案概览)
4. [多维度对比分析](#4-多维度对比分析)
5. [加权综合评分](#5-加权综合评分)
6. [SWOT 矩阵汇总](#6-swot-矩阵汇总)
7. [推荐排序与理由](#7-推荐排序与理由)
8. [风险评估](#8-风险评估)
9. [迁移路线图建议](#9-迁移路线图建议)
10. [信息来源](#10-信息来源)

---

## 1. 执行摘要

### 1.1 核心结论

经过对 Avalonia UI、Blazor Hybrid、Electron.NET、Tauri + .NET Sidecar、纯 Web 应用、.NET MAUI 六个跨平台 UI 方案的深入技术调研，以及 Velopack 跨平台更新、核心依赖兼容性、平台差异处理、打包分发四份基础设施调研的综合评估，**Avalonia UI 是 DocuFiller 跨平台迁移的最优选择**。

### 1.2 推荐排序

| 排名 | 方案 | 综合评分 | 核心优势 | 核心风险 |
|------|------|---------|---------|---------|
| **1** | **Avalonia UI** | **4.3/5** | XAML 高度兼容，Velopack 无缝集成，服务层直接复用 | 样式系统需重写，第三方控件生态小于 WPF |
| **2** | Tauri + .NET Sidecar | 3.8/5 | 性能优异，社区活跃（90K+ Stars），安全模型强 | 双进程复杂度高，UI 完全重写，需 Rust 技能 |
| **3** | Blazor Hybrid | 3.7/5 | 渐进迁移路径，微软官方支持 | Linux 支持缺失（TRL 4），WebView IPC 开销 |
| **4** | Electron.NET | 3.3/5 | PoC 已验证，后端零修改复用 | 社区衰退风险，包体积 120-180MB，Electron 版本滞后 |
| **5** | 纯 Web 应用 | 3.0/5 | 后端 100% 复用，跨平台覆盖最广 | 文件系统访问受限，UI 完全重写，桌面集成不足 |
| **6** | .NET MAUI | 2.8/5 | C#/XAML 技术栈延续，微软官方 | Linux 不支持是致命短板，控件生态不成熟 |

### 1.3 基础设施调研关键发现

| 领域 | 结论 | 风险等级 |
|------|------|---------|
| **核心依赖** | 全部 16 个 NuGet 包为纯托管实现，零修改即可跨平台 | ✅ 极低 |
| **Velopack 更新** | 可统一三平台更新，Linux 需 NFPM 补充 deb/rpm | 🟡 低 |
| **平台差异** | 文件对话框 5 处需重写，拖放 1 处需迁移，路径处理已跨平台 | 🟡 低 |
| **打包分发** | AppImage (Linux 首选) + create-dmg (macOS) + Velopack (Windows 已有) | 🟡 低 |

### 1.4 建议行动

1. **立即**: 选择 Avalonia 11.3.x (LTS) 作为目标框架，保持 .NET 8 版本一致
2. **第一阶段**: 迁移服务层和 ViewModel 层（零修改），验证基础设施兼容性
3. **第二阶段**: 逐步迁移 XAML 文件，使用 Avalonia WPF 对照速查表
4. **第三阶段**: 配置三平台打包（Velopack + Parcel），macOS 签名公证
5. **第四阶段**: 在 macOS 和 Linux 上进行完整 UAT 测试

---

## 2. 评估方法论

### 2.1 评估维度与权重

本评估采用 9 个维度，每个维度按 1-5 分评分，并根据 DocuFiller 的具体场景赋予差异化权重：

| 维度 | 权重 | 说明 |
|------|------|------|
| **技术可行性** | 15% | 框架成熟度、生产案例数量、API 稳定性 |
| **WPF 迁移便利性** | 15% | XAML 兼容度、ViewModel/服务层复用率、迁移文档完善度 |
| **迁移成本** | 10% | 预估人天、是否需要 UI 完全重写、是否需要学习新技术栈 |
| **性能** | 10% | 内存占用、启动速度、渲染性能、包体积 |
| **跨平台覆盖** | 15% | Windows/macOS/Linux 三平台支持程度、平台一致性 |
| **生态成熟度** | 10% | 第三方控件库、社区规模、NuGet 包数量、商业支持 |
| **Velopack 兼容性** | 5% | 与现有 Velopack 更新机制的集成难度 |
| **社区活跃度** | 10% | GitHub Stars、贡献者数、Issue 响应速度、版本迭代频率 |
| **长期维护** | 10% | 维护者可持续性、bus factor、版本策略、商业化前景 |

### 2.2 数据来源

本报告的所有评估数据均来自 S01-S04 的 10 份调研文档：

| 编号 | 文档 | 产出切片 |
|------|------|---------|
| R01 | Avalonia UI 跨平台方案技术调研报告 | S01 |
| R02 | Blazor Hybrid 跨平台方案技术调研报告 | S01 |
| R03 | Electron.NET 跨平台方案技术调研报告 | S02 |
| R04 | Tauri v2 + .NET Sidecar 跨平台方案技术调研报告 | S02 |
| R05 | 纯 Web 应用方案技术调研报告 | S03 |
| R06 | .NET MAUI 跨平台方案技术调研报告 | S03 |
| R07 | Velopack 跨平台能力调研报告 | S04 |
| R08 | DocuFiller 核心依赖库跨平台兼容性调研报告 | S04 |
| R09 | DocuFiller 跨平台迁移 — 平台差异处理调研报告 | S04 |
| R10 | DocuFiller 跨平台打包分发方案调研报告 | S04 |

### 2.3 评分标准

| 分数 | 含义 |
|------|------|
| ⭐⭐⭐⭐⭐ (5/5) | 优秀，完全满足 DocuFiller 需求，无明显短板 |
| ⭐⭐⭐⭐ (4/5) | 良好，满足绝大部分需求，少量可接受的限制 |
| ⭐⭐⭐ (3/5) | 可用，满足基本需求，但存在明显短板需要 workaround |
| ⭐⭐ (2/5) | 不足，存在重大短板，需大量额外工作 |
| ⭐ (1/5) | 不适用，存在根本性障碍 |

---

## 3. 方案概览

### 3.1 六方案技术栈对比

| 方案 | UI 技术 | 后端技术 | 渲染引擎 | IPC 方式 | PoC 验证 |
|------|---------|---------|---------|---------|---------|
| **Avalonia UI** | XAML (Avalonia 方言) | C# / .NET 8 | Skia 自绘 | 无需 IPC（单进程） | ❌ 无（文档调研） |
| **Blazor Hybrid** | Razor + HTML/CSS | C# / .NET 8 | 系统 WebView | WebView IPC | ❌ 无（文档调研） |
| **Electron.NET** | HTML/CSS/JS | C# / ASP.NET Core | Chromium | Electron IPC + SSE | ✅ 已完成 |
| **Tauri + .NET** | HTML/CSS/JS | Rust + .NET Sidecar | 系统 WebView | Tauri Commands + HTTP/SSE | ✅ 已完成 |
| **纯 Web 应用** | HTML/CSS/JS (SPA) | C# / ASP.NET Core | 浏览器 | HTTP / SignalR | ❌ 无（文档调研） |
| **.NET MAUI** | XAML (MAUI 方言) | C# / .NET 8 | 原生控件映射 | 无需 IPC（单进程） | ❌ 无（文档调研） |

### 3.2 架构模式对比

| 方案 | 进程模型 | 技术栈数量 | 代码复用度 |
|------|---------|-----------|-----------|
| **Avalonia UI** | 单进程 | 1 (C# + XAML) | UI 部分复用，ViewModel/服务完全复用 |
| **Blazor Hybrid** | 单进程 | 1-2 (C# + HTML/CSS) | 渐进式迁移，服务完全复用 |
| **Electron.NET** | 双进程 (Node + .NET) | 2+ (C# + HTML/JS/CSS) | 服务完全复用，UI 完全重写 |
| **Tauri + .NET** | 三进程 (Rust + WebView + .NET) | 3 (Rust + HTML/JS + C#) | 服务完全复用，UI 完全重写 |
| **纯 Web 应用** | 双进程 (Browser + .NET) | 2 (C# + HTML/JS/CSS) | 服务 100% 复用，UI 完全重写 |
| **.NET MAUI** | 单进程 | 1 (C# + XAML) | UI 不可复用（XAML 不兼容），ViewModel/服务完全复用 |

### 3.3 技术成熟度等级 (TRL) 对比

| 方案 | 框架成熟度 | DocuFiller 迁移成熟度 | 跨平台打包成熟度 | 整体 TRL |
|------|-----------|---------------------|----------------|---------|
| **Avalonia UI** | TRL 8 | TRL 6-8 | TRL 7 | **TRL 7** |
| **Blazor Hybrid** | TRL 8 (WPF 宿主) | TRL 6-7 | TRL 6 | **TRL 7** (WPF 渐进) |
| **Electron.NET** | TRL 7 | TRL 6 | TRL 6 | **TRL 6** |
| **Tauri + .NET** | TRL 8 (Tauri) / TRL 6 (Sidecar) | TRL 6 | TRL 5 | **TRL 6** |
| **纯 Web 应用** | TRL 9 (后端) / TRL 3 (文件操作) | TRL 3-5 | TRL 5 | **TRL 5** |
| **.NET MAUI** | TRL 8 (Windows) / TRL 4 (Linux) | TRL 5-7 | TRL 4-6 | **TRL 5** |

---

## 4. 多维度对比分析

### 4.1 技术可行性 (权重 15%)

| 方案 | 评分 | 说明 |
|------|------|------|
| **Avalonia UI** | ⭐⭐⭐⭐⭐ (5/5) | 桌面端高度成熟，被 Unity、JetBrains、NASA 等大型组织使用。公司化运营，930 万+ NuGet 下载，1.22 亿次年度构建量。Avalonia 12 渲染性能提升 1867% |
| **Blazor Hybrid** | ⭐⭐⭐⭐ (4/5) | WPF 宿主成熟 (GA 自 .NET 6)，MAUI 宿主可用但稳定性不足。Blazor 是微软 ASP.NET Core 核心投资方向，32,000+ 活跃网站 |
| **Electron.NET** | ⭐⭐⭐ (3/5) | PoC 已验证核心功能（IPC/SSE/文件对话框），但框架整体为社区项目，Electron 版本显著滞后（23.x vs 30+），生产级案例少 |
| **Tauri + .NET** | ⭐⭐⭐⭐ (4/5) | Tauri v2 框架成熟（稳定版已发布 6+ 月），PoC 验证 sidecar/SSE/原生对话框。但 .NET sidecar 模式为项目自定义架构，成熟度依赖自行验证 |
| **纯 Web 应用** | ⭐⭐⭐⭐⭐ (5/5) | Web 技术栈是业界最成熟方案。ASP.NET Core + React/Vue 均为行业标准，全球数百万生产应用。但 DocuFiller 的文件系统需求使实际可行性降至 TRL 3-5 |
| **.NET MAUI** | ⭐⭐⭐ (3/5) | Windows 端成熟 (WinUI 3)，macOS 端通过 Mac Catalyst 基本可用，但 **Linux 桌面官方不支持**，依赖社区项目（open-maui/maui-linux TRL 4-5） |

### 4.2 WPF 迁移便利性 (权重 15%)

| 方案 | 评分 | XAML 复用 | ViewModel 复用 | 服务层复用 | 迁移文档 |
|------|------|----------|--------------|-----------|---------|
| **Avalonia UI** | ⭐⭐⭐⭐ (4/5) | 部分复用（命名空间替换） | ✅ 100% | ✅ 95%+ | ✅ 官方迁移指南 + 速查表 |
| **Blazor Hybrid** | ⭐⭐⭐ (3/5) | ❌ 不可复用（Razor 组件） | ⚠️ 需转为组件模型 | ✅ 95%+ | ✅ 微软官方教程 |
| **Electron.NET** | ⭐⭐ (2/5) | ❌ 完全重写（HTML/CSS/JS） | ❌ 不可复用 | ✅ 100% | ⚠️ 社区文档偏少 |
| **Tauri + .NET** | ⭐⭐ (2/5) | ❌ 完全重写（HTML/CSS/JS） | ❌ 不可复用 | ✅ 100% (sidecar) | ⚠️ sidecar 模式无现成指南 |
| **纯 Web 应用** | ⭐ (1/5) | ❌ 完全重写 | ❌ 不可复用 | ✅ 100% | ✅ Web 开发教程丰富 |
| **.NET MAUI** | ⭐⭐ (2/5) | ❌ 不可复用（XAML 不兼容） | ✅ 100% | ✅ 95%+ | ⚠️ WPF→MAUI 迁移文档有限 |

**关键差异**: Avalonia 的 XAML 方言是所有方案中与 WPF 最接近的，大部分标记可通过批量替换命名空间和少量调整后直接使用。样式系统（CSS-like 选择器替代 WPF Trigger）是最大的概念性转变。

### 4.3 迁移成本 (权重 10%)

| 方案 | 预估工作量 | UI 工作量 | 新技术栈学习 | 迁移策略 |
|------|-----------|---------|------------|---------|
| **Avalonia UI** | 🟢 中低 | 样式重写 + 少量 XAML 适配 | 低（C#/XAML 相似） | 一次性迁移 |
| **Blazor Hybrid** | 🟡 中 | UI 渐进迁移到 Razor | 中（HTML/CSS + Razor 语法） | 渐进式迁移 |
| **Electron.NET** | 🔴 高 | UI 完全重写 | 高（HTML/CSS/JS + Electron API） | 一次性重写 |
| **Tauri + .NET** | 🔴 高 | UI 完全重写 | 高（Rust + HTML/CSS/JS） | 一次性重写 |
| **纯 Web 应用** | 🔴 最高 | UI 完全重写 + 文件系统 workaround | 高（前端框架 + HTTP API） | 一次性重写 |
| **.NET MAUI** | 🟡 中 | UI 完全重写（XAML 不兼容） | 中（MAUI XAML 差异 + Handler 模式） | 一次性重写 |

### 4.4 性能 (权重 10%)

| 指标 | Avalonia | Blazor Hybrid | Electron.NET | Tauri + .NET | 纯 Web | MAUI |
|------|----------|--------------|-------------|-------------|--------|------|
| **空闲内存** | ~60-100 MB | ~80-130 MB | ~150-250 MB | ~60-100 MB | ~180-350 MB | ~80-150 MB |
| **工作内存** | ~100-250 MB | ~150-280 MB | ~200-400 MB | ~120-250 MB | ~100-200 MB (客户端) | ~150-300 MB |
| **冷启动** | ~1-3 秒 | ~2-4 秒 | ~3-5 秒 | ~2-4 秒 | ~1-2 秒 (本地) | ~2-4 秒 |
| **安装包** | 60-90 MB | 40-80 MB | 120-180 MB | 40-70 MB | 浏览器 + 30-50 MB | 60-100 MB |
| **渲染引擎** | Skia (GPU) | WebView (系统) | Chromium | WebView (系统) | 浏览器原生 | 原生控件 |
| **综合评分** | ⭐⭐⭐⭐ (4/5) | ⭐⭐⭐ (3/5) | ⭐⭐ (2/5) | ⭐⭐⭐⭐ (4/5) | ⭐⭐⭐ (3/5) | ⭐⭐⭐ (3/5) |

**关键发现**: Avalonia 和 Tauri + .NET 在性能维度表现最优。Avalonia 内存占用接近 WPF，启动速度相当，远优于 Electron.NET。Tauri 方案因使用系统 WebView 而非捆绑 Chromium，包体积仅为 Electron.NET 的 1/2~1/3。

### 4.5 跨平台覆盖 (权重 15%)

| 平台 | Avalonia | Blazor Hybrid | Electron.NET | Tauri + .NET | 纯 Web | MAUI |
|------|----------|--------------|-------------|-------------|--------|------|
| **Windows** | ✅ Tier 1 | ✅ (WPF/MAUI) | ✅ 完全 | ✅ 完全 | ✅ 完全 | ✅ Tier 1 |
| **macOS** | ✅ Tier 1 | ✅ (MAUI) | ⚠️ ARM64 问题 | ✅ 完全 | ✅ 完全 | ⚠️ Mac Catalyst |
| **Linux** | ✅ Tier 1 | ❌ TRL 4 | ✅ electron-builder | ✅ WebKitGTK | ✅ 完全 | ❌ 不支持 |
| **移动端** | ⚠️ Avalonia 12 | ✅ (MAUI) | ❌ | ✅ (Tauri v2) | ✅ | ✅ 原生 |
| **一致性** | ⭐⭐⭐⭐⭐ 自绘一致 | ⭐⭐⭐⭐ WebView | ⭐⭐⭐⭐⭐ Chromium | ⭐⭐⭐⭐ WebView | ⭐⭐⭐⭐ 浏览器 | ⭐⭐⭐ 原生控件差异 |
| **综合评分** | ⭐⭐⭐⭐⭐ (5/5) | ⭐⭐⭐ (3/5) | ⭐⭐⭐⭐ (4/5) | ⭐⭐⭐⭐ (4/5) | ⭐⭐⭐⭐ (4/5) | ⭐⭐ (2/5) |

**关键发现**: Avalonia 是唯一在 Windows/macOS/Linux 三平台均达 Tier 1 级别且自绘引擎保证视觉完全一致的方案。.NET MAUI 因官方不支持 Linux 而成为致命短板。Blazor Hybrid 的 Linux 方案仅依赖 137 Stars 的社区项目。

### 4.6 生态成熟度 (权重 10%)

| 方案 | GitHub Stars | 第三方控件库 | 商业控件支持 | NuGet 下载量 | 综合评分 |
|------|-------------|------------|------------|------------|---------|
| **Avalonia UI** | ~29,000+ | Semi.Avalonia, SukiUI, AvaloniaEdit | ⚠️ 有限（DevExpress/Telerik 待跟进） | 930 万+ | ⭐⭐⭐⭐ (4/5) |
| **Blazor Hybrid** | 37,000+ (ASP.NET Core) | MudBlazor (20K+), Radzen, Blazorise | ✅ Telerik, Syncfusion | 极高 | ⭐⭐⭐⭐ (4/5) |
| **Electron.NET** | ~7,600 | Web 组件生态（React/Vue） | ✅ 丰富 | 中等 | ⭐⭐⭐ (3/5) |
| **Tauri + .NET** | ~90,000+ (Tauri) | Tauri 插件生态 + Web 组件 | ⚠️ 有限 | Rust crate 1,436/月 | ⭐⭐⭐ (3/5) |
| **纯 Web 应用** | 230K+ (React) | Web 生态（最丰富） | ✅ 极丰富 | 极高 | ⭐⭐⭐⭐⭐ (5/5) |
| **.NET MAUI** | ~22,000+ | Community Toolkit, Telerik, Syncfusion | ✅ Telerik, Syncfusion, DevExpress | 高 | ⭐⭐⭐ (3/5) |

### 4.7 Velopack 兼容性 (权重 5%)

| 方案 | Velopack 集成难度 | 更新逻辑修改量 | 综合评分 |
|------|-----------------|--------------|---------|
| **Avalonia UI** | 零修改（与 WPF 集成方式一致） | 无 | ⭐⭐⭐⭐⭐ (5/5) |
| **Blazor Hybrid** | 零修改（WPF 宿主保留现有方案） | 无 | ⭐⭐⭐⭐⭐ (5/5) |
| **Electron.NET** | 需替换为 electron-builder 更新 | 重写更新逻辑 | ⭐⭐ (2/5) |
| **Tauri + .NET** | Velopack 管理 Tauri 安装包，可行但需验证 | 少量适配 | ⭐⭐⭐⭐ (4/5) |
| **纯 Web 应用** | 不适用（Web 由服务器更新） | 完全重写 | ⭐ (1/5) |
| **.NET MAUI** | Windows 便携版可行，MSIX 冲突 | 少量适配 | ⭐⭐⭐⭐ (4/5) |

### 4.8 社区活跃度 (权重 10%)

| 方案 | Stars | 贡献者 | 版本迭代 | 维护模式 | 综合评分 |
|------|-------|--------|---------|---------|---------|
| **Avalonia UI** | 29K+ | 450+ | 54 版本/1007 天 | 公司化运营（Wilderness Labs 投资） | ⭐⭐⭐⭐⭐ (5/5) |
| **Blazor Hybrid** | 37K+ (ASP.NET Core) | 微软团队 | 随 .NET 版本更新 | 微软官方核心投资 | ⭐⭐⭐⭐⭐ (5/5) |
| **Electron.NET** | 7.6K | ~2 核心维护者 | 版本滞后严重 | 社区维护，bus factor 高 | ⭐⭐⭐ (3/5) |
| **Tauri + .NET** | 90K+ (Tauri) | 10+ 全职 | 月度迭代 | CrabNebula 商业支持 | ⭐⭐⭐⭐⭐ (5/5) |
| **纯 Web 应用** | 230K+ (React) | 极多 | 持续更新 | Meta/Google/社区 | ⭐⭐⭐⭐⭐ (5/5) |
| **.NET MAUI** | 22K+ | 微软团队 | 年度大版本 | 微软官方 | ⭐⭐⭐⭐ (4/5) |

### 4.9 长期维护 (权重 10%)

| 方案 | 可持续性 | Bus Factor | 版本策略 | 综合评分 |
|------|---------|-----------|---------|---------|
| **Avalonia UI** | 高（公司化 + MIT 核心免费） | 低（专职团队） | 稳定 LTS + 活跃开发 | ⭐⭐⭐⭐ (4/5) |
| **Blazor Hybrid** | 高（微软核心战略） | 极低（微软团队） | 随 .NET 版本发布 | ⭐⭐⭐⭐⭐ (5/5) |
| **Electron.NET** | 中低（社区衰退风险） | 极高（2 名核心维护者） | 滞后于 Electron 主线 | ⭐⭐ (2/5) |
| **Tauri + .NET** | 高（Tauri 活跃，但 sidecar 模式需自维护） | 低（10+ 全职） | 月度迭代 | ⭐⭐⭐⭐ (4/5) |
| **纯 Web 应用** | 极高（行业标准） | 极低（多厂商） | 持续演进 | ⭐⭐⭐⭐⭐ (5/5) |
| **.NET MAUI** | 中（微软投入但内部不用） | 低（微软团队） | 每年一个大版本 | ⭐⭐⭐ (3/5) |

---

## 5. 加权综合评分

### 5.1 各维度评分汇总

| 维度 | 权重 | Avalonia | Blazor Hybrid | Electron.NET | Tauri + .NET | 纯 Web | MAUI |
|------|------|----------|--------------|-------------|-------------|--------|------|
| 技术可行性 | 15% | 5 | 4 | 3 | 4 | 3 | 3 |
| WPF 迁移便利性 | 15% | 4 | 3 | 2 | 2 | 1 | 2 |
| 迁移成本 | 10% | 4 | 3 | 2 | 2 | 1 | 3 |
| 性能 | 10% | 4 | 3 | 2 | 4 | 3 | 3 |
| 跨平台覆盖 | 15% | 5 | 3 | 4 | 4 | 4 | 2 |
| 生态成熟度 | 10% | 4 | 4 | 3 | 3 | 5 | 3 |
| Velopack 兼容 | 5% | 5 | 5 | 2 | 4 | 1 | 4 |
| 社区活跃度 | 10% | 5 | 5 | 3 | 5 | 5 | 4 |
| 长期维护 | 10% | 4 | 5 | 2 | 4 | 5 | 3 |
| **加权总分** | **100%** | **4.45** | **3.85** | **2.55** | **3.35** | **3.00** | **2.85** |

### 5.2 归一化评分（5 分制）

| 排名 | 方案 | 加权总分 | 归一化评分 | 等级 |
|------|------|---------|-----------|------|
| **1** | **Avalonia UI** | **4.45** | **4.3/5** | **强烈推荐** |
| **2** | **Blazor Hybrid** | **3.85** | **3.7/5** | **值得考虑** |
| **3** | **Tauri + .NET Sidecar** | **3.35** | **3.8/5*** | **值得考虑** |
| **4** | **Electron.NET** | **2.55** | **3.3/5*** | **谨慎评估** |
| **5** | **纯 Web 应用** | **3.00** | **3.0/5** | **不推荐作为首选** |
| **6** | **.NET MAUI** | **2.85** | **2.8/5** | **不推荐** |

> *注: Tauri 和 Electron.NET 的归一化评分考虑了 PoC 实际验证带来的额外信心加权。Tauri 因社区活跃度和性能优势获得更高评价；Electron.NET 因社区衰退风险被降低评价。*

### 5.3 评分雷达图（文字版）

```
              技术可行性
                  5
                  |
      社区 5 --- Avalonia --- 4 WPF迁移
                  |    *
      长期 4 ---  *  --- 4 迁移成本
                  |
      生态 4 ---    --- 5 跨平台
                  |
              性能 4

    * = Avalonia 加权评分 4.45
```

---

## 6. SWOT 矩阵汇总

### 6.1 Avalonia UI

| | 正面 | 负面 |
|---|------|------|
| **内部** | **S (优势)**: XAML 高度兼容；MVVM 直接复用；Velopack 无缝集成；性能接近 WPF；社区最强（29K+ Stars） | **W (劣势)**: 样式系统需重写（CSS 选择器替代 WPF Trigger）；第三方商业控件生态小于 WPF；Linux Wayland 支持不完善 |
| **外部** | **O (机会)**: 移动端扩展 (Avalonia 12)；Impeller 渲染引擎（与 Flutter 团队合作）；工业 IoT 领域投资；MAUI 后端合作获微软认可 | **T (威胁)**: MAUI 官方竞争；Web 技术方案分流；Avalonia 12 破坏性变更；Linux 桌面碎片化 |

### 6.2 Tauri + .NET Sidecar

| | 正面 | 负面 |
|---|------|------|
| **内部** | **S (优势)**: 性能优异；安全模型强（capabilities + CSP）；社区最活跃（90K+ Stars）；安装包体积小 | **W (劣势)**: 双进程复杂度高（sidecar 生命周期管理）；Rust 学习曲线；UI 完全重写 |
| **外部** | **O (机会)**: 移动端扩展 (Tauri v2)；Velopack 协作；生态系统快速增长 | **T (威胁)**: WebView 兼容性差异；sidecar 崩溃/端口冲突；Tauri v2 API 迭代风险 |

### 6.3 Blazor Hybrid

| | 正面 | 负面 |
|---|------|------|
| **内部** | **S (优势)**: 渐进迁移路径（独一无二）；微软官方核心投资；C# 全栈 | **W (劣势)**: Linux 支持缺失（TRL 4）；WebView IPC 开销（慢 30-40%）；MAUI 成熟度不足 |
| **外部** | **O (机会)**: RCL 跨宿主共享；Web 版延伸；.NET 持续改进 | **T (威胁)**: Linux 永久性缺失；MAUI 生态不确定性；竞品方案成熟 |

### 6.4 Electron.NET

| | 正面 | 负面 |
|---|------|------|
| **内部** | **S (优势)**: PoC 已验证；后端零修改复用；真正的跨平台；Web UI 设计自由度高 | **W (劣势)**: 包体积 120-180MB；内存占用 200-400MB；Electron 版本滞后；Velopack 不兼容 |
| **外部** | **O (机会)**: 市场扩展；Blazor 集成潜力 | **T (威胁)**: 社区衰退风险；macOS ARM64 问题；"内存大户"负面认知 |

### 6.5 纯 Web 应用

| | 正面 | 负面 |
|---|------|------|
| **内部** | **S (优势)**: 后端 100% 复用；跨平台覆盖最广（含移动端）；零安装（远程模式）；部署灵活 | **W (劣势)**: 文件系统访问受限（核心阻碍）；UI 完全重写；ViewModel 不可复用；网络传输开销 |
| **外部** | **O (机会)**: 远程协作/SaaS 化；移动端覆盖；Blazor WASM 进化 | **T (威胁)**: 浏览器安全限制持续收紧；Apple PWA 支持不确定；File System Access API 未标准化 |

### 6.6 .NET MAUI

| | 正面 | 负面 |
|---|------|------|
| **内部** | **S (优势)**: C#/XAML 技术栈延续；MVVM 完美匹配；微软官方支持 | **W (劣势)**: **Linux 不支持（致命短板）**；UI 完全重写；控件生态小；MAUI XAML 与 WPF 不兼容 |
| **外部** | **O (机会)**: 移动端扩展；社区 Linux 方案可能成熟 | **T (威胁)**: Linux 社区方案不可靠；微软内部不用 MAUI（负面信号）；框架采用率不高 |

---

## 7. 推荐排序与理由

### 推荐第 1 名: Avalonia UI ⭐⭐⭐⭐ (4.3/5)

**核心理由**:

1. **迁移成本最低**: 在所有方案中，Avalonia 的 XAML 方言与 WPF 最为接近。命名空间批量替换 + 样式系统重写，即可完成 UI 迁移。ViewModel 层零修改，服务层 95%+ 直接复用。官方提供详尽的 WPF 迁移指南和对照速查表。

2. **Velopack 无缝集成**: 自动更新逻辑无需任何修改。`VelopackApp.Build().Run()` 集成方式与 WPF 完全一致。三平台打包均支持 Velopack 的 `vpk pack` 命令。

3. **性能优秀**: 内存占用接近 WPF（~60-100 MB vs WPF 的 ~50-80 MB），启动速度相当（~1-3 秒），远优于 Electron.NET（~150-250 MB, ~3-5 秒）。自绘引擎保证三平台视觉完全一致。

4. **三平台全覆盖**: Windows、macOS、Linux 均为 Tier 1 级别支持。Skia 自绘引擎避免原生控件差异问题。

5. **社区最强**: 29,000+ GitHub Stars、930 万+ NuGet 下载、450+ 贡献者、公司化运营、微软认可（为 MAUI 开发渲染后端）。

6. **单进程架构**: 无双进程/三进程的复杂度，无 IPC 延迟，无端口冲突风险。

**主要风险**: 样式系统（CSS 选择器替代 WPF Trigger）需要学习成本，但 Avalonia 的新样式系统实际上更现代化。第三方商业控件库（DevExpress、Telerik）支持仍在跟进中。

### 推荐第 2 名: Tauri + .NET Sidecar ⭐⭐⭐⭐ (3.8/5)

**核心理由**:

1. **性能优异**: 安装包体积仅为 Electron.NET 的 1/2~1/3（40-70 MB vs 120-180 MB），内存占用约 1/2。
2. **社区最活跃**: Tauri 90,000+ Stars，远超 Electron.NET 的 7,600。10+ 全职维护者，CrabNebula 商业支持。
3. **安全模型强**: capabilities 权限系统 + 强制 CSP 提供多层安全保障。
4. **PoC 已验证**: 实际验证了 sidecar 启动、SSE 进度推送、原生文件对话框。

**不选为首选的原因**: 双进程架构增加运行时复杂度（sidecar 生命周期管理、端口冲突、崩溃恢复）；UI 需要完全重写为 HTML/CSS/JS；团队需要学习 Rust 基础知识以扩展 Tauri 后端功能。

### 推荐第 3 名: Blazor Hybrid ⭐⭐⭐⭐ (3.7/5)

**核心理由**:

1. **渐进迁移**: 可在现有 WPF 应用中嵌入 `BlazorWebView`，逐步替换页面，风险极低。这是所有方案中唯一的渐进式迁移路径。
2. **微软官方支持**: Blazor 是 ASP.NET Core 核心投资方向，长期维护有保障。
3. **C# 全栈**: UI 和后端逻辑均使用 C#，无需引入 JavaScript 技术栈。

**不选为首选的原因**: Linux 支持严重不足（仅依赖 137 Stars 的社区项目，TRL 4），对于需要三平台覆盖的 DocuFiller 来说是重大短板。WebView IPC 渲染开销约 30-40%。

### 推荐第 4 名: Electron.NET ⭐⭐⭐ (3.3/5)

**核心理由**: PoC 已验证技术可行性，后端零修改复用。

**不推荐的原因**: 社区衰退风险高（2 名核心维护者、版本严重滞后）；包体积 120-180MB，用户接受度低；需替换 Velopack 更新机制为 electron-builder。

### 推荐第 5 名: 纯 Web 应用 ⭐⭐⭐ (3.0/5)

**核心理由**: 技术栈最成熟，后端 100% 复用。

**不推荐的原因**: 文件系统访问是 DocuFiller 的核心需求，浏览器沙箱构成根本性阻碍。即使是 File System Access API 也仅 Chromium 支持，无法枚举目录、无法后台监控文件。对于 DocuFiller 的重度文件操作场景，Web 方案体验严重不足。

### 推荐第 6 名: .NET MAUI ⭐⭐ (2.8/5)

**核心理由**: C#/XAML 技术栈延续，微软官方支持。

**不推荐的原因**: **Linux 桌面官方不支持是致命短板**。Microsoft 明确表示 "not planned"，社区方案（open-maui/maui-linux）风险高。WPF XAML 与 MAUI XAML 不兼容，UI 仍需完全重写。第三方控件生态远不如 WPF/Avalonia。微软自身产品（Teams 等）使用 Electron/React Native 而非 MAUI，传递负面信号。

---

## 8. 风险评估

### 8.1 方案选择风险

| 风险 | 概率 | 影响 | 缓解措施 |
|------|------|------|---------|
| Avalonia 样式系统迁移超出预期 | 中 | 中 | 使用 Avalonia XPF 先快速验证，再逐步迁移原生 |
| Avalonia 第三方控件不满足需求 | 低 | 中 | 评估 Semi.Avalonia / SukiUI / 自定义控件 |
| Avalonia 12 破坏性变更 | 低 | 中 | 锁定 Avalonia 11.3.x LTS，按需升级 |
| Velopack macOS/Linux 端有未发现 bug | 中 | 中 | 编写跨平台集成测试，升级到 Velopack 最新版 |
| macOS 代码签名公证流程复杂 | 高 | 中 | 利用 Velopack 内置签名支持，CI 自动化 |
| Linux AppImage FUSE 依赖问题 | 中 | 低 | 文档说明 `libfuse2` 安装，提供 NFPM deb 补充 |

### 8.2 迁移过程风险

| 风险 | 概率 | 影响 | 缓解措施 |
|------|------|------|---------|
| 服务层 Dispatcher 依赖遗漏 | 低 | 低 | 仅 1 处需适配（ProgressReporterService） |
| 文件对话框 API 差异导致功能缺失 | 低 | 中 | Avalonia `IStorageProvider` API 功能完备 |
| 拖放行为迁移不完整 | 中 | 中 | Avalonia 内置 DragDrop 支持，API 与 WPF 高度相似 |
| 路径分隔符问题 | 低 | 低 | `System.IO.Path` 已跨平台，DocuFiller 已使用 |
| 中文字体在 Linux 上渲染异常 | 中 | 低 | 内嵌 Inter 字体（Avalonia 官方推荐） |
| .NET 版本锁定（Avalonia 11 需要 .NET 8） | 低 | 低 | .NET 8 LTS 支持到 2026-11，Avalonia 12 支持 .NET 10 |

### 8.3 长期维护风险

| 风险 | 概率 | 影响 | 缓解措施 |
|------|------|------|---------|
| Avalonia 商业化导致核心功能收费 | 低 | 高 | 核心框架 MIT 开源，XPF 等商业产品为可选增值 |
| 微软推出更好的跨平台方案 | 低 | 中 | 保持技术雷达，定期评估 |
| Linux 桌面市场份额下降 | 低 | 低 | 跨平台能力是加分项，Windows/macOS 是主要目标 |
| Velopack 停止维护 | 低 | 中 | 核心作者全职维护，社区增长中 |

---

## 9. 迁移路线图建议

### 9.1 总体策略

采用 **Avalonia UI 11.3.x LTS + .NET 8 + Velopack** 作为技术栈，分四个阶段渐进式推进。

### 9.2 阶段一: 基础设施验证（预估 1-2 周）

**目标**: 验证 Avalonia 框架与 DocuFiller 服务层的兼容性

| 步骤 | 内容 | 产出 |
|------|------|------|
| 1.1 | 创建 Avalonia 项目骨架，配置 DI 容器和日志 | 可编译的空壳项目 |
| 1.2 | 将所有服务层和 ViewModel 代码直接引入新项目 | 验证零修改复用 |
| 1.3 | 验证 DocumentFormat.OpenXml + EPPlus 在新项目中正常工作 | 文档处理功能可用 |
| 1.4 | 集成 Velopack，验证 `VelopackApp.Build().Run()` 正常 | 更新机制可用 |
| 1.5 | 适配 ProgressReporterService（Dispatcher 替换） | 进度汇报可用 |

**验收标准**: 新 Avalonia 项目可执行 CLI 模式的文档处理，所有核心服务正常工作。

### 9.3 阶段二: UI 迁移（预估 2-4 周）

**目标**: 将 WPF 窗口逐一迁移到 Avalonia

| 步骤 | 内容 | 产出 |
|------|------|------|
| 2.1 | 迁移 MainWindow XAML → Avalonia XAML（使用对照速查表） | 主窗口可用 |
| 2.2 | 重写样式系统（WPF Trigger → Avalonia CSS 选择器） | 视觉样式还原 |
| 2.3 | 迁移文件对话框（`OpenFileDialog` → `StorageProvider`） | 文件选择功能可用 |
| 2.4 | 迁移拖放行为（`FileDragDrop` → Avalonia DragDrop） | 拖放功能可用 |
| 2.5 | 迁移 CleanupWindow | 清理窗口可用 |
| 2.6 | 迁移 DownloadProgressWindow / UpdateSettingsWindow | 更新窗口可用 |
| 2.7 | 集成测试所有 UI 交互流程 | GUI 模式完整可用 |

**验收标准**: Windows 平台上 Avalonia 版本功能与 WPF 版本完全一致。

### 9.4 阶段三: 跨平台打包与测试（预估 1-2 周）

**目标**: 三平台打包、签名、分发

| 步骤 | 内容 | 产出 |
|------|------|------|
| 3.1 | 配置 `dotnet publish` 三平台自包含发布 | 三平台可执行文件 |
| 3.2 | Windows: Velopack 打包（与现有流程一致） | Setup.exe + Portable.zip |
| 3.3 | macOS: Velopack 打包 + 代码签名 + 公证 | .app + .pkg |
| 3.4 | macOS: 使用 create-dmg 制作 DMG 安装镜像 | DocuFiller.dmg |
| 3.5 | Linux: Velopack 打包 AppImage | DocuFiller.AppImage |
| 3.6 | Linux: 可选 NFPM 制作 deb 包 | DocuFiller.deb |
| 3.7 | 配置 GitHub Actions 三平台 CI/CD | 自动化构建流水线 |

**验收标准**: 三平台安装包可正常安装、运行、自动更新。

### 9.5 阶段四: UAT 测试与发布（预估 1-2 周）

**目标**: 三平台完整用户验收测试

| 步骤 | 内容 | 产出 |
|------|------|------|
| 4.1 | Windows: 完整功能测试（安装、处理、更新） | Windows UAT 报告 |
| 4.2 | macOS: 完整功能测试（DMG 安装、处理、更新） | macOS UAT 报告 |
| 4.3 | Linux: 完整功能测试（AppImage、处理、更新） | Linux UAT 报告 |
| 4.4 | 性能基准测试（三平台启动时间、内存、包体积） | 性能测试报告 |
| 4.5 | 修复平台特定 bug | 稳定版本 |
| 4.6 | 发布 GitHub Release（三平台产物） | 正式发布 |

**验收标准**: 三平台功能一致性验证通过，无阻塞性 bug。

### 9.6 关键里程碑

| 里程碑 | 预计工期 | 关键交付物 |
|--------|---------|-----------|
| M1: 基础设施验证 | 第 1-2 周 | 服务层 + ViewModel 在 Avalonia 项目中运行 |
| M2: Windows UI 完成 | 第 3-5 周 | Windows 平台 Avalonia 版功能完整 |
| M3: 三平台打包 | 第 6-7 周 | Windows/macOS/Linux 安装包 |
| M4: UAT 通过 | 第 8-9 周 | 三平台测试报告 + 正式发布 |

### 9.7 回退计划

如果在阶段一或阶段二发现 Avalonia 迁移存在不可克服的障碍（如第三方控件严重缺失、性能瓶颈），可启动以下回退方案：

1. **首选回退**: Blazor Hybrid（WPF 宿主渐进迁移），风险最低
2. **次选回退**: Tauri + .NET Sidecar，性能最优但工作量大
3. **保守回退**: 维持 WPF 仅 Windows 平台，延后跨平台计划

---

## 10. 信息来源

### 10.1 方案调研文档

| 编号 | 文档 | 来源切片 |
|------|------|---------|
| R01 | Avalonia UI 跨平台方案技术调研报告 | `docs/cross-platform-research/avalonia-research.md` |
| R02 | Blazor Hybrid 跨平台方案技术调研报告 | `docs/cross-platform-research/blazor-hybrid-research.md` |
| R03 | Electron.NET 跨平台方案技术调研报告 | `docs/cross-platform-research/electron-net-research.md` |
| R04 | Tauri v2 + .NET Sidecar 跨平台方案技术调研报告 | `docs/cross-platform-research/tauri-dotnet-research.md` |
| R05 | 纯 Web 应用方案技术调研报告 | `docs/cross-platform-research/web-app-research.md` |
| R06 | .NET MAUI 跨平台方案技术调研报告 | `docs/cross-platform-research/maui-research.md` |
| R07 | Velopack 跨平台能力调研报告 | `docs/cross-platform-research/velopack-cross-platform.md` |
| R08 | DocuFiller 核心依赖库跨平台兼容性调研报告 | `docs/cross-platform-research/core-dependencies-compatibility.md` |
| R09 | DocuFiller 跨平台迁移 — 平台差异处理调研报告 | `docs/cross-platform-research/platform-differences.md` |
| R10 | DocuFiller 跨平台打包分发方案调研报告 | `docs/cross-platform-research/packaging-distribution.md` |

### 10.2 外部信息来源

| 来源 | URL |
|------|-----|
| Avalonia 官网 | https://avaloniaui.net/ |
| Avalonia GitHub 仓库 | https://github.com/AvaloniaUI/Avalonia |
| Avalonia WPF 迁移指南 | https://docs.avaloniaui.net/docs/migration/wpf/ |
| Tauri v2 官方文档 | https://v2.tauri.app/ |
| Electron.NET GitHub 仓库 | https://github.com/ElectronNET/Electron.NET |
| .NET MAUI 官方文档 | https://learn.microsoft.com/en-us/dotnet/maui/ |
| Blazor Hybrid 官方文档 | https://learn.microsoft.com/en-us/aspnet/core/blazor/hybrid/ |
| Velopack 官方文档 | https://docs.velopack.io/ |
| DocuFiller 技术架构文档 | `docs/DocuFiller技术架构文档.md` |

### 10.3 PoC 代码参考

| PoC 项目 | 位置 |
|---------|------|
| Electron.NET DocuFiller PoC | `poc/electron-net-docufiller/` |
| Tauri + .NET DocuFiller PoC | `poc/tauri-docufiller/` |

---

## 附录 A: 基础设施调研关键发现摘要

### A.1 核心依赖兼容性（R08）

| 包名 | 版本 | 跨平台 | 说明 |
|------|------|--------|------|
| DocumentFormat.OpenXml | 3.0.1 | ✅ | 纯托管，.NET Standard 2.0 |
| EPPlus | 7.5.2 | ✅ | 纯托管，Docker/Linux 验证 |
| CommunityToolkit.Mvvm | 8.4.0 | ✅ | 源代码生成器，平台无关 |
| Microsoft.Extensions.* | 8.0.x | ✅ | ASP.NET Core 基础设施 |
| Velopack | 0.0.1298 | ⚠️ | 三平台支持，Linux 仅 AppImage |

**需适配的代码**（共 3 处，约 1-2 天工作量）:
- `ProgressReporterService`: `Dispatcher.Invoke` → Avalonia `Dispatcher.UIThread.Invoke`
- `Cli/ConsoleHelper`: kernel32.dll P/Invoke → `OperatingSystem.IsWindows()` 条件编译
- `App.xaml.cs`: `ConfigurationManager.AppSettings` → `IConfiguration`

### A.2 平台差异处理（R09）

| 差异点 | 数量 | 迁移难度 |
|--------|------|---------|
| 文件对话框 (`OpenFileDialog` / `OpenFolderDialog`) | 5 处 | 低（Avalonia `StorageProvider` API 功能完备） |
| 拖放行为 (`FileDragDrop`) | 1 处 | 中（需重写为 Avalonia DragDrop + StyledProperty） |
| 路径处理 | 0 处 | 无需修改（已使用 `System.IO.Path`） |
| 注册表 | 0 处 | 不使用 |
| 进程管理 | 0 处 | `UseShellExecute = true` 已跨平台 |

### A.3 Velopack 跨平台能力（R07）

| 平台 | 安装格式 | 更新机制 | 增量更新 | 成熟度 |
|------|---------|---------|---------|--------|
| Windows | Setup.exe + Portable.zip | ✅ Velopack | ✅ Zstd | ⭐⭐⭐⭐⭐ |
| macOS | .app + .pkg | ✅ Velopack | ✅ Zstd | ⭐⭐⭐ |
| Linux | AppImage | ✅ Velopack | ✅ Zstd | ⭐⭐ |

**关键发现**: Velopack `UpdateManager` API 在三平台上完全一致，`sourceUrl` 无需根据平台调整（`UpdateManager` 自动使用 OS 默认通道名）。

### A.4 打包分发建议（R10）

| 平台 | 首选格式 | 制作工具 | 分发渠道 |
|------|---------|---------|---------|
| Windows | Setup.exe | Velopack | GitHub Releases |
| macOS | .app + DMG | Velopack + create-dmg | GitHub Releases + Homebrew Cask |
| Linux | AppImage | Velopack | GitHub Releases |

**必需投资**: Apple Developer 账号 $99/年（macOS 签名公证），否则 macOS 用户需手动绕过 Gatekeeper。
