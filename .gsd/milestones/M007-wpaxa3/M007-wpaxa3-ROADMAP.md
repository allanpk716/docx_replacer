# M007-wpaxa3: Velopack 自动更新 — 单 EXE 发布 + 内网更新

**Vision:** 为 DocuFiller 集成 Velopack 自动更新框架，实现手动检测远程最新版本号并一键自动升级。发布形态从多文件 zip 改为单 EXE（PublishSingleFile self-contained），同时提供安装版（Setup.exe）和便携版（Portable.zip）。更新源为内网 HTTP 静态文件服务器。

## Success Criteria

- 主窗口底部状态栏显示版本号和可用的检查更新按钮
- 点击检查更新能正确连接内网更新源检测版本
- 发布脚本产出 Setup.exe + Portable.zip + 增量更新包
- 在干净 Windows 环境下验证完整的安装 → 更新流程
- 用户配置文件在更新后保留
- 旧更新系统所有残留已清理
- 所有现有测试通过

## Slices

- [x] **S01: S01** `risk:high` `depends:[]`
  > After this: dotnet build 编译通过，VelopackApp 在 Program.cs 中正确初始化，旧更新残留配置和脚本引用已清理，所有现有测试通过

- [x] **S02: S02** `risk:medium` `depends:[]`
  > After this: 主窗口底部状态栏显示当前版本号，点击检查更新可连接更新源检测版本，有新版本显示确认对话框，无新版本显示已是最新

- [x] **S03: S03** `risk:medium` `depends:[]`
  > After this: build-internal.bat 产出 Velopack 格式完整发布物：Setup.exe + Portable.zip + .nupkg + releases.win.json

- [x] **S04: S04** `risk:low` `depends:[]`
  > After this: 在干净 Windows 上安装旧版 → 检查更新 → 升级到新版 → 确认应用正常启动且用户配置文件保留

## Boundary Map

### S01 → S02
Produces:
  Program.cs → VelopackApp.Build().Run() 初始化
  Services/Interfaces/IUpdateService.cs → 更新服务接口
  appsettings.json → Update:UpdateUrl 配置节点

Consumes: nothing (first slice)

### S01 → S03
Produces:
  DocuFiller.csproj → Velopack NuGet 包引用
  Program.cs → VelopackApp 初始化

Consumes: nothing (first slice)

### S02 → S04
Produces:
  Services/UpdateService.cs → IUpdateService 实现（Velopack UpdateManager 封装）
  MainWindow.xaml → 底部状态栏 UI（版本号 + 检查更新按钮）
  ViewModels/MainWindowViewModel.cs → 更新相关命令和属性

Consumes from S01:
  Program.cs → VelopackApp 初始化
  appsettings.json → Update:UpdateUrl 配置

### S03 → S04
Produces:
  scripts/build-internal.bat → 改造后的发布流程（PublishSingleFile + vpk pack）
  发布产物 → Setup.exe, Portable.zip, .nupkg, releases.win.json

Consumes from S01:
  DocuFiller.csproj → PublishSingleFile 配置
