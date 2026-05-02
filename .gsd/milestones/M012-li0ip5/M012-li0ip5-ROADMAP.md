# M012-li0ip5: 主界面布局紧凑化与拖放修复

**Vision:** 将 DocuFiller 主界面从 1400x900 的松散布局紧凑化为 900x550，在 1366x768 下无需滚动即可完整操作。同时修复窗口未聚焦时拖放失效的 bug。

## Success Criteria

- 在 1366x768 分辨率下两个 Tab 所有控件完整可见无需滚动
- 窗口未聚焦时拖放正常工作
- dotnet build 编译通过
- 现有功能不受影响

## Slices

- [x] **S01: S01** `risk:medium` `depends:[]`
  > After this: 启动应用在 1366x768 下看到完整紧凑的界面，两个 Tab 内容无需滚动即可看全

- [x] **S02: S02** `risk:medium` `depends:[]`
  > After this: 窗口未聚焦时从资源管理器拖拽文件到路径文本框，拖放正常触发

## Boundary Map

### S01

Produces:
- MainWindow.xaml（紧凑化布局，900x550 窗口，路径文本框支持 AllowDrop）
- App.xaml（调整后的全局样式，字号 12-14px）
- MainWindow.xaml.cs（拖放事件处理迁移到 TextBox）

Consumes:
- 现有的 MainWindowViewModel 绑定属性（不改 ViewModel）
- 现有的 App.xaml 样式资源（调整参数但不改结构）

### S01 → S02

Produces:
- 紧凑化后的 MainWindow 布局（S01 产出）
- 基本的 TextBox 拖放事件处理（S01 产出）

Consumes from S01:
- MainWindow.xaml.cs 中的拖放事件处理器
- 路径 TextBox 控件的 AllowDrop 设置

### S02

Produces:
- Window 级别的拖放激活机制
- 所有三种拖放场景的完整验证

Consumes from S01:
- MainWindow.xaml 中的路径 TextBox 控件
- MainWindow.xaml.cs 中的拖放事件处理器
