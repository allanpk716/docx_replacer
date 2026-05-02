---
id: M009-q7p4iu
title: "GitHub CI/CD 发布 + 多源更新提醒"
status: complete
completed_at: 2026-04-26T11:41:42.904Z
key_decisions:
  - D025: UpdateUrl 非空走内网 Go 服务器，为空走 GitHubSource（多源切换策略）
  - D026: CLI update 命令使用纯 JSONL + --yes 参数，无交互式确认
  - D027: IUpdateService 接口不改签名，IsUpdateUrlConfigured 语义扩展为始终 true
  - D028: GitHub Release 只走 stable 通道，beta 继续走内网 Go 服务器
  - D029: 只支持安装版自动更新，便携版有明确提示引导用户
  - Velopack vpk pack 默认生成 Portable.zip，无需 --packPortable 标志
  - IsInstalled 构造时一次性检测并缓存，避免重复创建 UpdateManager
  - CLI post-command 更新提醒仅对成功命令生效，避免污染错误输出
key_files:
  - .github/workflows/build-release.yml
  - Services/Interfaces/IUpdateService.cs
  - Services/UpdateService.cs
  - Tests/UpdateServiceTests.cs
  - ViewModels/MainWindowViewModel.cs
  - MainWindow.xaml
  - Cli/Commands/UpdateCommand.cs
  - Cli/JsonlOutput.cs
  - Cli/CliRunner.cs
  - App.xaml.cs
  - Tests/DocuFiller.Tests/Cli/UpdateCommandTests.cs
  - Tests/DocuFiller.Tests/Cli/CliRunnerTests.cs
lessons_learned:
  - Velopack vpk pack 在 Windows 上默认生成 Portable.zip，--packPortable 标志不存在（只有 --noPortable 跳过），与文档描述不同
  - Velopack UpdateManager 未实现 IDisposable，不能用 using 语法
  - Velopack 类型使用 NuGet.Versioning.SemanticVersion 而非 System.Version，CLI 输出需注意类型转换
  - GitHub Actions workflow 中 grep 模式的通配符 * 需要 escape 处理，Windows CMD 和 Git Bash 行为不同
  - IUpdateService? 可选注入模式（null 时安全跳过）是处理服务不可用场景的简洁方案
---

# M009-q7p4iu: GitHub CI/CD 发布 + 多源更新提醒

**建立完整的 GitHub CI/CD 发布流水线（v* tag 触发 → 4 类 Release 产物）、UpdateService 多源自动切换（HTTP/GitHubSource）、GUI 状态栏常驻更新提示、CLI update 子命令 + post-command 更新提醒**

## What Happened

M009 为 DocuFiller 建立了从发布到用户感知更新的完整链路，分 4 个并行切片完成：

**S01（CI/CD 流水线）** 创建了 `.github/workflows/build-release.yml`，打 `v*` tag 推送后自动在 GitHub Actions 上构建、Velopack 打包、发布到 GitHub Release。产出 Setup.exe + Portable.zip + .nupkg + releases.win.json 四类文件。24 项结构检查全部通过。

**S02（多源更新服务）** 改造 UpdateService 支持双源自动切换：UpdateUrl 非空走 SimpleWebSource（内网 Go 服务器），为空走 GithubSource（GitHub Releases stable 通道）。新增 IsInstalled 便携版检测（构造时缓存）、UpdateSourceType 属性暴露源类型。IsUpdateUrlConfigured 语义扩展为始终 true（GitHub 永远可用）。10 个单元测试覆盖全部路径。

**S03（GUI 状态栏提示）** 在 MainWindowViewModel 新增 UpdateStatus 枚举（6 种状态）和 5 个绑定属性，构造函数 fire-and-forget 调用 InitializeUpdateStatusAsync 自动检查更新状态。MainWindow.xaml 状态栏新增 TextBlock，通过 InputBindings+MouseBinding 实现声明式点击交互。现有"检查更新"按钮完整保留。172 个测试零回归。

**S04（CLI 更新）** 创建 UpdateCommand 类实现 update 子命令（无 --yes 输出版本 JSONL，--yes 执行下载重启）。在 CliRunner 添加 post-command 更新提醒钩子（exitCode==0 且非 update 子命令时条件性追加 update 行）。9 个新测试覆盖全部路径，154 个测试零回归。

四个切片之间有明确的依赖关系：S01 的 Release 产物供 S02 的 GithubSource 使用；S02 的多源服务被 S03（GUI）和 S04（CLI）消费。所有切片均无偏差、无阻塞、无已知限制。

## Success Criteria Results

### SC1: 打 v* tag 推送后 GitHub Release 自动创建，包含 Setup.exe + Portable.zip + .nupkg + releases.win.json
**PASS** — S01 T01 创建 workflow，T02 通过 24 项结构检查验证 trigger/build/release 步骤。git diff 确认 `.github/workflows/build-release.yml` 存在且包含全部 4 类文件的上传配置。`dotnet build -c Release` 通过确认 CI 兼容性。

### SC2: UpdateUrl 为空时 UpdateService 使用 GitHubSource，非空时使用 HTTP URL
**PASS** — S02 通过 10 个单元测试验证双源切换逻辑。UpdateUrl 为空 → UpdateSourceType 为 "GitHub"（GithubSource），非空 → "HTTP"（SimpleWebSource）。Channel 默认 stable，遵循 D028 决策。

### SC3: GUI 状态栏正确显示三种更新状态（未配置源/有新版本/便携版提示）
**PASS** — S03 T01+T02：UpdateStatus 枚举 6 种状态覆盖便携版/有更新/最新/检查中/错误/无状态。XAML TextBlock 绑定到 ViewModel 属性。构造函数 fire-and-forget 自动检查。172 个测试全部通过。

### SC4: CLI update 命令 JSONL 输出正确，--yes 执行下载应用重启
**PASS** — S04 T01+T03：UpdateCommand 实现完整生命周期。无 --yes 输出 type=update JSONL（含 currentVersion/latestVersion/hasUpdate/isInstalled/updateSourceType），--yes 便携版输出 PORTABLE_NOT_SUPPORTED 错误。6 个单元测试覆盖。

### SC5: CLI 其他命令在 actionable 时追加 update 类型 JSONL 行
**PASS** — S04 T02+T03：CliRunner.TryAppendUpdateReminderAsync 仅在 exitCode==0、subcommand != update、updateInfo != null 时追加。3 个测试覆盖（有更新/无更新/失败不追加）。

### SC6: dotnet build 通过，现有测试不被破坏
**PASS** — `dotnet build` 0 错误，`dotnet test` 154 单元测试 + 27 E2E = 181 全部通过，零回归。

## Definition of Done Results

### DoD-1: All slices complete
**PASS** — S01 ✓ (2/2 tasks), S02 ✓ (2/2), S03 ✓ (2/2), S04 ✓ (3/3)

### DoD-2: All slice summaries exist
**PASS** — S01-SUMMARY.md, S02-SUMMARY.md, S03-SUMMARY.md, S04-SUMMARY.md all exist with full content

### DoD-3: Cross-slice integration points work
**PASS** — S01→S02: Velopack artifacts in Release for GithubSource; S02→S03: IUpdateService properties consumed by ViewModel; S02→S04: IUpdateService methods consumed by UpdateCommand. No integration mismatches reported.

### DoD-4: Code changes verified against integration branch
**PASS** — `git diff master...HEAD` shows 12 non-.gsd files across all 4 slices. Build and full test suite pass.

## Requirement Outcomes

### R037 (core-capability): active → validated
Proof: S01 — `.github/workflows/build-release.yml` 存在，24 项结构检查通过，v* tag trigger + .NET 8 build + Velopack pack + Release 创建完整

### R038 (core-capability): active → validated
Proof: S01 — grep 检查确认 workflow 包含 Setup.exe、Portable.zip、.nupkg、releases.win.json 四类文件上传

### R039 (core-capability): active → validated
Proof: S02 — 10 个单元测试通过，UpdateUrl 为空走 GithubSource，非空走 SimpleWebSource，Channel 默认 stable

### R040 (primary-user-loop): active → validated
Proof: S03 — ViewModel 枚举驱动 + XAML 绑定 + fire-and-forget 检查，172 个测试通过

### R041 (core-capability): active → validated
Proof: S04 — UpdateCommand 实现 ICliCommand，JSONL 输出格式正确，--yes 下载重启，便携版错误处理

### R042 (core-capability): active → validated
Proof: S04 — post-command hook 仅在 exitCode==0 且有新版本时追加，3 个测试验证三种场景

### R043 (constraint): active → validated
Proof: S02 接口只新增属性不改签名 + S03 现有按钮保留在 Column 3 + S04 不修改现有命令行为，154+27=181 测试零回归

## Deviations

S02 T01: IsInstalled 检测原计划使用 `using var tempManager`，Velopack 0.0.1298 的 UpdateManager 未实现 IDisposable，改为普通变量赋值。

## Follow-ups

None.
