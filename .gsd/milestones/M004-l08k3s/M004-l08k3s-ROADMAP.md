# M004-l08k3s: 功能瘦身 - 移除不活跃功能模块

**Vision:** 清理 DocuFiller 代码库中所有不再使用的功能模块：在线更新系统、JSON 编辑器遗留、JSON 数据源解析、JSON→Excel 转换器、KeywordEditorUrl、Tools 目录。清理后 Excel 为唯一数据源，编译不依赖外部文件，代码库只包含活跃功能。

## Success Criteria

- dotnet build 在无 External/ 目录文件的情况下编译成功
- dotnet test 全部通过
- 无残留的更新/JSON编辑器/JSON数据源/转换器相关 .cs/.xaml 文件
- CLAUDE.md 和 README.md 与清理后的代码一致
- 应用可正常启动，Excel 数据源处理流程完整可用

## Slices

- [x] **S01: S01** `risk:low` `depends:[]`
  > After this: dotnet build 通过（无 External 文件），grep 确认无更新/JSON编辑器相关代码文件

- [x] **S02: S02** `risk:medium` `depends:[]`
  > After this: dotnet build 通过，Excel 为唯一数据源，主窗口无 JSON/转换器/外部编辑器入口，Tools 目录已删除

- [x] **S03: S03** `risk:medium` `depends:[]`
  > After this: dotnet test 全部通过，文档（CLAUDE.md、README.md）与代码一致

## Boundary Map

### S01 → S02

Produces:
- 清洁的 csproj（无 PreBuild update-client 门禁）
- 清洁的 App.xaml.cs DI 注册（无更新服务、无 JSON 编辑器服务）
- 清洁的 MainWindow.xaml（无更新按钮、无更新横幅）
- 清洁的 MainWindowViewModel（无更新逻辑、无 UpdateBanner）

Consumes:
- nothing (first slice)

### S02 → S03

Produces:
- 清洁的 DocumentProcessorService（仅 Excel 分支，无 IDataParser 依赖）
- 清洁的 MainWindowViewModel（仅 Excel 预览/统计，无 JSON 逻辑）
- 清洁的 MainWindow.xaml（无转换器入口、无 KeywordEditorUrl 入口、文件对话框仅 .xlsx）
- 清洁的 App.xaml.cs（无 IDataParser、IExcelToWordConverter、ConverterWindow DI 注册）
- Tools/ 目录已删除

Consumes from S01:
- App.xaml.cs DI 注册（S01 已清理更新服务）
- MainWindowViewModel（S01 已清理更新逻辑）
- MainWindow.xaml（S01 已清理更新 UI）
