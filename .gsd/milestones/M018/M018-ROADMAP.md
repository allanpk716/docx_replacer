# M018: 便携版自动更新支持

**Vision:** 移除 DocuFiller 便携版的自动更新阻断，让便携版享有与安装版完全一致的检查→下载→应用更新能力，并提供端到端自动化测试脚本验证本地 HTTP 和内网 Go 服务器两种环境。

## Success Criteria

- 便携版 GUI 状态栏显示正常更新状态（不是'不支持自动更新'）
- 便携版 CLI `update --yes` 能完成完整更新链路（检查→下载→应用→重启→版本升级）
- E2E 脚本在本地 HTTP 和内网 Go 服务器两种环境下都跑通便携版更新
- 安装版更新行为无回归
- dotnet build 编译通过
- 决策 D029 已推翻记录

## Slices

- [x] **S01: S01** `risk:medium` `depends:[]`
  > After this: 便携版 GUI 状态栏显示正常更新状态；便携版 CLI update --yes 完成检查→下载→应用更新流程

- [x] **S02: S02** `risk:medium` `depends:[]`
  > After this: E2E 脚本一键验证便携版在本地 HTTP 和内网 Go 服务器上的完整更新链路

## Boundary Map

## Boundary Map

### S01 → S02

Produces:
- Services/Interfaces/IUpdateService.cs → IsPortable 属性
- ViewModels/MainWindowViewModel.cs → 移除 PortableVersion 枚举和阻断逻辑，便携版走正常更新状态流转
- Cli/Commands/UpdateCommand.cs → 移除 IsInstalled 守卫和 PORTABLE_NOT_SUPPORTED 错误
- Services/UpdateService.cs → IsInstalled 降级为信息属性，新增 IsPortable 实现

Consumes:
- Velopack UpdateManager.IsPortable（SDK 原生属性）
- 现有 UpdateService.CheckForUpdatesAsync / DownloadUpdatesAsync / ApplyUpdatesAndRestart（无改动）
