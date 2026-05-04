---
phase: complete-milestone
phase_name: M020 里程碑完成
project: DocuFiller
generated: 2026-05-04T03:00:00.000Z
counts:
  decisions: 4
  lessons: 5
  patterns: 3
  surprises: 1
missing_artifacts: []
---

# M020 LEARNINGS

### Decisions

- **OpenXmlHelper 作为静态工具类而非 DI 服务** — 将 DocumentProcessorService 和 ContentControlProcessor 之间的 6 个重复方法提取到 `Utils/OpenXmlHelper.cs` 静态类，CommentManager 和 ILogger 通过方法参数传入而非字段注入。选择静态类因为方法无状态，避免不必要的 DI 注册。
  Source: S03-SUMMARY.md/What Happened

- **CT.Mvvm 使用完全限定基类名避免命名冲突** — DownloadProgressViewModel 和 UpdateSettingsViewModel 迁移到 CommunityToolkit.Mvvm 时，使用完全限定名 `CommunityToolkit.Mvvm.ComponentModel.ObservableObject` 避免与项目自定义 `ObservableObject.cs` 冲突。
  Source: S04-SUMMARY.md/What Was Done

- **幽灵配置类直接删除而非标注** — 4 个从未被代码引用的配置类（AppSettings、LoggingSettings、FileProcessingSettings、UISettings）直接从 AppSettings.cs 中删除，appsettings.json 精简为仅 2 个 section。标注保留会增加维护混乱。
  Source: S05-SUMMARY.md/执行概要

- **CancellationToken 使用 CreateLinkedTokenSource 链接** — DocumentProcessorService 接收外部 CancellationToken 后，通过 CreateLinkedTokenSource 创建内部 CTS 链接两个 token，支持外部取消和内部取消的统一管理。
  Source: S01-SUMMARY.md/修复内容

### Lessons

- **内部 catch(Exception) 会吞噬 OperationCanceledException** — DocumentProcessorService 的 foreach catch(Exception) 会捕获取消异常，导致取消功能表现为处理失败而非异常传播。测试需调整为验证失败标记而非异常。
  Source: S01-SUMMARY.md/已知限制

- **E2ERegression.csproj 需要与主项目同步链接共享文件** — S03 提取 OpenXmlHelper.cs 时发现 E2ERegression.csproj 预存构建问题（缺少链接文件），需要同步更新测试项目的编译链接。
  Source: S03-SUMMARY.md/偏差

- **TemplateCacheService 动态时间评估限制单元测试** — IsExpired 使用 IOptionsMonitor.CurrentValue 动态评估，无法在测试中注入时钟选择性过期。测试只能验证过期项被移除，无法同时验证新鲜项保留。
  Source: S05-SUMMARY.md/已知限制

- **FormattedCellValue.PlainText 是只读计算属性** — 集成测试重构时发现不能通过对象初始化器设置，必须使用 FormattedCellValue.FromPlainText() 工厂方法构造测试数据。
  Source: S02-SUMMARY.md/关键发现

- **DI 容器的开放泛型注册可被直接利用** — FileService 添加 ILogger<FileService> 时，App.xaml.cs 中已有 `AddSingleton(typeof(ILogger<>), typeof(Logger<>))` 开放泛型注册，无需额外配置即可自动注入。
  Source: S01-SUMMARY.md/修复内容

### Patterns

- **静态工具类模式（Utils/）** — 无状态的共享操作放在 `Utils/` 目录下的静态类中，通过方法参数传入依赖（如 CommentManager、ILogger），无需 DI 注册。OpenXmlHelper 是此模式的参考实现。
  Source: S03-SUMMARY.md/关键设计决策

- **CT.Mvvm 迁移模式** — `[ObservableProperty]` 配合下划线前缀字段名、`[RelayCommand]` 标注方法、`partial void OnPropertyNameChanged()` 处理副作用。完全限定基类名解决命名冲突。可减少约 40% ViewModel 样板代码。
  Source: S04-SUMMARY.md/What Was Done

- **渐进式测试补充模式** — 先在 S01 建立错误路径测试（4 个），S05 再补充快乐路径测试（13 个）。分阶段添加避免单次大规模测试编写，且确保每个阶段都有验证。
  Source: S01-SUMMARY.md/修复内容, S05-SUMMARY.md/执行概要

### Surprises

- **NuGet path1 null 错误** — M020 完成验证时遇到 `dotnet build` 和 `dotnet nuget list source` 都报 `Value cannot be null. (Parameter 'path1')` 错误，但这是系统级 NuGet 环境问题（global-packages 缓存或 NuGet.Config 配置损坏），在主仓库和 worktree 中都复现，非代码缺陷。所有 slice 执行期间构建和测试均通过。
  Source: complete-milestone verification
