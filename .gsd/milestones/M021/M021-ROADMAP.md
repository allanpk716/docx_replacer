# M021: 第二轮重构 - ViewModel 拆分 + 拖放 Behavior + R028 自动更新 + 文档同步

**Vision:** 将 MainWindowViewModel（1623 行）全面拆分为协调器 + 子 ViewModel 模式，消除清理 Tab 重复代码，提取拖放为可复用 Behavior，实现 R028 自动检查更新，删除 CLAUDE.md，同步产品需求文档。

## Success Criteria

- MainWindowViewModel.cs 行数 ≤ 400 行
- 关键词替换 Tab 全部功能正常（模板选择、数据加载、预览、处理、取消、进度显示）
- 清理 Tab 和独立窗口共用 CleanupViewModel
- 拖放功能行为不变（TextBox 和 Border 都支持文件拖放）
- 应用启动 5 秒后状态栏自动显示更新状态（R028）
- CLAUDE.md 已删除
- 产品需求文档 UI 描述与实际代码一致
- dotnet build 零错误，dotnet test 全部通过

## Slices

- [ ] **S01: FillViewModel + UpdateStatusViewModel 拆分** `risk:high` `depends:[]`
  > After this: 关键词替换 Tab 和更新状态功能全部正常工作，MainWindowVM 降至 400 行以内

- [ ] **S02: CleanupViewModel 统一 + CT.Mvvm 迁移** `risk:medium` `depends:[S01]`
  > After this: 清理 Tab 和独立窗口共用 CleanupViewModel（CT.Mvvm），MainWindowVM 无清理代码

- [ ] **S03: DragDropBehavior 提取** `risk:medium` `depends:[S01]`
  > After this: MainWindow.xaml.cs 拖放事件处理器从 15 个降至 ≤3 个（Window 级别），拖放功能正常

- [ ] **S04: 服务接口补全 + CleanupService 日志补充** `risk:low` `depends:[]`
  > After this: IDocumentProcessor 接口覆盖 DocumentProcessorService 公共 API；CleanupService 5 个 catch 块有 ILogger

- [ ] **S05: R028 启动自动检查更新 + 通知徽章** `risk:medium` `depends:[S01]`
  > After this: 应用启动 5 秒后状态栏自动显示更新状态，有新版本时显示视觉指示

- [ ] **S06: CLAUDE.md 删除 + 产品需求文档同步** `risk:low` `depends:[S01,S02]`
  > After this: CLAUDE.md 不存在；产品需求文档 UI 描述与实际代码一致

## Boundary Map

## Boundary Map

### S01 → S02

Produces:
- MainWindowVM 协调器（~400 行），持有子 VM 引用
- FillViewModel（CT.Mvvm），关键词替换 Tab 全部业务逻辑
- UpdateStatusViewModel（CT.Mvvm），更新状态管理 + CheckUpdateAsync
- XAML DataContext 切换到子 VM

Consumes: nothing (first slice)

### S01 → S03

Produces:
- FillViewModel（被 DragDropBehavior 的 Command 回调目标）
- MainWindowVM 协调器结构

Consumes: nothing (first slice)

### S01 → S05

Produces:
- UpdateStatusViewModel（R028 自动检查的目标 VM）

Consumes: nothing (first slice)

### S02 → S06

Produces:
- CleanupViewModel（CT.Mvvm，统一实现）

Consumes from S01:
- MainWindowVM 协调器中清理相关代码已移除

### S04

Independent — no upstream consumption.

### S05 → S06

Produces:
- R028 自动检查更新功能

Consumes from S01:
- UpdateStatusViewModel 的自动检查能力
