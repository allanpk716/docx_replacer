# M002-ahlnua: 代码质量清理

**Vision:** 清理生产代码中的调试日志残留、硬编码 URL、以及文件夹选择 hack，提升代码可维护性和专业度。具体包括：(1) 将所有 Console.WriteLine("[DEBUG]") 和 Debug.WriteLine 替换为结构化 ILogger 调用；(2) 将硬编码的关键词编辑器 URL 移入 appsettings.json 配置；(3) 将三个使用 OpenFileDialog 伪装文件夹选择器的方法替换为 Windows API Code Pack 的 CommonOpenFileDialog 或 .NET 内置 FolderBrowserDialog。Tools/ 目录下的独立诊断工具不在此里程碑范围内。

## Success Criteria

- 生产代码中零 Console.WriteLine 残留（Tools/ 除外）
- 生产代码中零 System.Diagnostics.Debug.WriteLine 残留（App.xaml.cs 全局异常处理除外）
- 关键词编辑器 URL 从硬编码移至 appsettings.json 的 UISettings.KeywordEditorUrl 配置项
- BrowseOutput、BrowseTemplateFolder、BrowseCleanupOutput 三个方法使用真正的文件夹选择对话框
- dotnet test 全部通过，零回归

## Slices

- [ ] **S01: S01** `risk:low` `depends:[]`
  > After this: 生产代码中所有调试日志使用 ILogger，关键词编辑器 URL 从配置读取，grep 扫描确认零残留

- [ ] **S02: 文件夹选择对话框替换和验证** `risk:medium` `depends:[S01]`
  > After this: BrowseOutput、BrowseTemplateFolder、BrowseCleanupOutput 三个按钮打开真正的系统文件夹选择对话框，dotnet test 全部通过

## Boundary Map

### S01 → S02

Produces:
- AppSettings.UISettings.KeywordEditorUrl — 关键词编辑器 URL 配置项（appsettings.json）
- 干净的 ILogger 日志调用基线 — S02 可以在此基础上继续修改 ViewModel 代码而不引入新的调试日志

Consumes:
- 现有 ILogger 注入 — MainWindowViewModel 和其他 ViewModel 已有 ILogger 依赖
- 现有 AppSettings 配置系统 — appsettings.json + Options pattern

### S02 (leaf)

Produces:
- FolderBrowserDialog 集成 — BrowseOutput/BrowseTemplateFolder/BrowseCleanupOutput 使用真正的文件夹选择对话框

Consumes:
- S01 清理后的 MainWindowViewModel.cs — S02 在已清理的代码上工作，避免在调试日志残留的基础上修改

Consumes from S01:
- 干净的 ViewModel 代码基线
