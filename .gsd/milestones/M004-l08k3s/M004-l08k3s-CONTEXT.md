# M004-l08k3s: 功能瘦身 — 移除不活跃功能模块

**Gathered:** 2026-04-23
**Status:** Ready for planning

## Project Description

DocuFiller 代码库中存在多个已不再使用的功能模块：在线更新系统（依赖外部 update-client.exe）、JSON 编辑器遗留代码、JSON 数据源解析（已被 Excel 数据源取代）、JSON→Excel 转换器窗口、KeywordEditorUrl 内部服务入口、以及 10 个开发阶段诊断工具。这些代码增加维护负担、扩大攻击面、混淆 AI 辅助编码、且有 csproj PreBuild 门禁阻止无外部文件时的构建。

## Why This Milestone

清理不活跃代码是技术债务管理的基本功。具体来说：
1. csproj PreBuild 检查 update-client.exe 是否存在，**阻止了在缺少该文件时的正常构建**
2. JSON 编辑器已废弃但文件还在，AI 辅助编码时会被误导
3. JSON 数据源和转换器窗口在 JSON 编辑器移除后已无意义
4. Tools 目录 10 个项目纯粹是历史遗留

## User-Visible Outcome

### When this milestone is complete, the user can:

- 在没有 update-client.exe 的情况下正常编译项目
- 只看到 Excel 数据源选项（不再有 JSON 文件选择）
- 主界面不再有更新检查按钮、转换器入口、关键词编辑器入口
- 代码库体积显著减小，只包含活跃功能

### Entry point / environment

- Entry point: Visual Studio 2022 / `dotnet build` / `dotnet run`
- Environment: Windows 桌面应用

## Completion Class

- Contract complete means: 所有清理目标文件已删除，编译零错误，测试全部通过
- Integration complete means: `dotnet build` + `dotnet test` 在无 External 文件的环境下正常工作
- Operational complete means: 应用可正常启动，Excel 数据源处理流程完整可用

## Final Integrated Acceptance

To call this milestone complete, we must prove:

- `dotnet build` 在无 External/ 目录文件的情况下编译成功
- `dotnet test` 全部通过
- 无残留的更新/JSON编辑器/JSON数据源/转换器相关 .cs/.xaml 文件
- 文档（CLAUDE.md、README.md）与清理后的代码一致

## Architectural Decisions

### JSON 数据源全部清理

**Decision:** 移除 DataParserService 和 IDataParser，Excel 成为唯一数据源

**Rationale:** 用户明确确认 JSON 数据源不再需要。DocumentProcessorService 中的 JSON 处理分支可以整体移除，构造函数去掉 IDataParser 依赖。

**Alternatives Considered:**
- 保留 JSON 数据源作为备用 — 增加维护负担，用户明确不需要

### 在线更新功能全套移除

**Decision:** 移除所有更新相关代码、Models、ViewModels、Views、外部文件、csproj 门禁

**Rationale:** 在线更新依赖外部 update-client.exe 和更新服务器，用户确认不再需要。PreBuild 门禁是最影响开发的阻碍。

**Alternatives Considered:**
- 仅移除 PreBuild 门禁保留代码 — 半残代码没有保留价值

## Error Handling Strategy

清理后错误处理不变。主要影响是 DocumentProcessorService 移除 JSON 分支后，只保留 Excel 路径的错误处理逻辑。不需要新增错误处理。

## Risks and Unknowns

- DocumentProcessorService 是核心处理管道，移除 IDataParser 依赖时需要仔细处理构造函数变更 — 构造函数参数变更会影响 DI 注册和所有测试中的 mock
- 部分集成测试依赖 JSON 数据（HeaderFooterCommentIntegrationTests），需要重写为使用 Excel 数据
- 文档中有大量 JSON/更新相关描述，需要全面扫描更新

## Existing Codebase / Prior Art

- `Services/DocumentProcessorService.cs` — 核心处理管道，包含 Excel 和 JSON 两个分支
- `App.xaml.cs` — DI 注册，包含所有待清理服务的注册
- `DocuFiller.csproj` — PreBuild 门禁检查 update-client.exe
- `MainWindow.xaml` — 主界面，包含更新按钮、转换器入口、关键词编辑器入口
- `ViewModels/MainWindowViewModel.cs` — 主 ViewModel，包含大量更新和 JSON 相关逻辑

## Relevant Requirements

- R014 — 移除在线更新功能
- R015 — 移除 JSON 编辑器遗留
- R016 — 移除 JSON 数据源
- R017 — 移除转换器窗口
- R018 — 移除 KeywordEditorUrl
- R019 — 清理 Tools 目录
- R020 — 测试全部通过
- R021 — 文档同步更新

## Scope

### In Scope

- 移除在线更新功能（全套：服务、UI、Models、外部文件、csproj 门禁）
- 移除 JSON 编辑器遗留代码（Service、ViewModel、Models、Validation）
- 移除 JSON 数据源解析（DataParserService、IDataParser、主流程中 JSON 相关逻辑）
- 移除转换器窗口（Views、ViewModel、Service、Interface）
- 移除 KeywordEditorUrl 配置和 UI 入口
- 清理 Tools 目录（10 个诊断工具项目）
- 修复受影响的测试
- 同步更新文档（CLAUDE.md、README.md）

### Out of Scope / Non-Goals

- 不更新 docs/VERSION_MANAGEMENT.md、docs/EXTERNAL_SETUP.md、docs/deployment-guide.md（R012 沿用）
- 不做功能增强或新功能开发
- 不重构现有活跃代码的结构

## Technical Constraints

- Windows 环境，.NET 8 + WPF
- 构建后必须 `dotnet build` 通过
- 所有测试必须 `dotnet test` 通过

## Integration Points

- `DocumentProcessorService` 构造函数变更 → `App.xaml.cs` DI 注册需同步
- `MainWindowViewModel` 构造函数变更 → `MainWindow.xaml.cs` 中手动构造需同步
- `DocuFiller.csproj` PreBuild 门禁移除 → 构建不再依赖 External/ 文件

## Testing Requirements

- `dotnet test` 全部通过
- 受影响测试需重写（使用 Excel 数据替代 JSON 数据）或移除（JSON 专用测试）
- 不降低现有测试覆盖率

## Acceptance Criteria

- S01: `dotnet build` 通过，grep 搜索不到更新和 JSON 编辑器相关代码文件
- S02: `dotnet build` 通过，应用可启动，Excel 数据源流程完整可用
- S03: `dotnet test` 全部通过，文档与代码一致

## Open Questions

- None — 所有决策已在讨论中确认
