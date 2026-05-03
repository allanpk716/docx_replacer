# M019: 进度条可见增长修复 + 应用图标

**Vision:** 修复下载进度窗口的 ProgressBar 填充不增长问题，为 DocuFiller 生成并应用匹配其定位（Word 文档批量填充）的应用图标。

## Success Criteria

- ProgressBar 填充区域随 Value 变化从左向右增长
- 所有窗口（MainWindow、DownloadProgressWindow、UpdateSettingsWindow、CleanupWindow）显示图标
- exe 文件资源中包含图标
- dotnet build 无错误

## Slices

- [x] **S01: S01** `risk:low` `depends:[]`
  > After this: 下载进度条的填充区域随百分比数值增长从左向右平滑延伸

- [x] **S02: S02** `risk:low` `depends:[]`
  > After this: 主窗口、子窗口、任务栏和 exe 文件都显示 DocuFiller 专属图标，标题栏 emoji 替换为真实图标

## Boundary Map

### S01
Produces:
- 修复后的 ModernProgressBarStyle 模板（App.xaml），包含 PART_Indicator

Consumes:
- nothing（独立修复）

### S02
Produces:
- Resources/app.ico 图标文件
- csproj ApplicationIcon 配置
- 所有窗口 Icon 属性设置
- 标题栏 Image 控件替换 emoji

Consumes:
- nothing（独立任务）
