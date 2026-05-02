---
depends_on: [M004-l08k3s]
---

# M006-rj9bue: 真实数据端到端回归测试

**Gathered:** 2026-04-23
**Status:** Queued — pending auto-mode execution

## Project Description

用真实业务数据（Excel + Word 模板）创建独立的端到端回归测试项目，验证 M001-M004 所有里程碑的重构和改进没有破坏核心文档替换功能。测试覆盖替换正确性、表格结构完整性、富文本格式保留、页眉页脚替换、批注追踪五个维度。

## Why This Milestone

M004 功能瘦身是最大规模的代码清理（移除 JSON 数据源、在线更新、转换器、Tools 等），直接影响 DocumentProcessorService 的构造函数和处理管道。目前所有现有测试使用 mock 或自造的简单模板，没有真实业务文档的回归保护。在 M005 CLI 开发之前，必须确保核心替换链路完全可用。

## Baseline Commit

**用户确认的功能正常基准版本:** `d81cd002c6ee9c62d3dc0378d7c7a63cd102a2ed`（docs: add build scripts design document）

该版本经过人工测试确认核心替换功能完全正常。M006 的回归测试必须验证：**同一份测试代码在 d81cd00 和 M004 后的代码上都能编译通过且测试通过。**

从 d81cd00 到当前 HEAD 的核心代码变化：
- `ExcelDataParserService.cs` — M001 增加三列格式自动检测（DetectExcelFormat），向后兼容两列模式
- `Models/ExcelFileSummary.cs` — 增加 DuplicateRowIds 字段
- `MainWindow.xaml.cs` / `CleanupWindow.xaml.cs` — M002 日志替换，不影响替换逻辑
- **未变化的核心文件:** DocumentProcessorService（替换管道内部逻辑）、ContentControlProcessor、SafeTextReplacer、SafeFormattedContentReplacer、CommentManager

## Version-Compatible Verification Strategy (关键)

**核心矛盾：** M004 删除了 `IDataParser`/`DataParserService` 整个接口和类，导致 `DocumentProcessorService` 构造函数变更。直接 `new` 构造函数的测试代码只能在某一个版本上编译。

**解决方案：反射构建服务**

### 反射服务工厂 (ServiceFactory)

创建一个 `ServiceFactory` 辅助类，通过反射检测 `DocumentProcessorService` 构造函数参数来自动适配不同版本：

```csharp
public static class ServiceFactory
{
    public static DocumentProcessorService CreateProcessor(
        ILoggerFactory loggerFactory, IFileService fileService, ...)
    {
        var ctors = typeof(DocumentProcessorService).GetConstructors();
        var firstCtor = ctors.First();
        var parameters = firstCtor.GetParameters();
        
        // 检测是否有 IDataParser 参数
        bool hasDataParser = parameters.Any(p => p.ParameterType.Name == "IDataParser");
        
        var args = new List<object>();
        foreach (var param in parameters)
        {
            args.Add(ResolveService(param.ParameterType, services, hasDataParser));
        }
        
        return (DocumentProcessorService)firstCtor.Invoke(args.ToArray());
    }
}
```

### 为什么可行

1. **d81cd00 版本** — 构造函数有 `IDataParser` 参数 → 反射检测到 → 从 DI 容器解析 `DataParserService` 传入
2. **M004 后版本** — 构造函数无 `IDataParser` 参数 → 反射检测不到 → 跳过，不传
3. **测试代码本身不直接引用 `IDataParser` 或 `DataParserService` 类型** — 所有类型解析通过反射完成，编译时无依赖

### 需要注意的问题

- **d81cd00 上不存在 `DataParserService` 类？** — 不对，d81cd00 上它还在。M004 才删除它。
- **M004 后 `DataParserService` 文件被删除** — 反射工厂不需要引用它的类型，只要不加载就行。需要确保工厂不 `using` 任何被删除的命名空间。
- **csproj 文件引用** — 测试项目的 csproj 引用主项目（ProjectReference），不会直接引用具体的 .cs 文件。主项目编译后的 DLL 会被自动引用。所以无论主项目包含哪些文件，测试项目都能编译。

### 验证流程

**M006 执行时（auto-mode，M004 完成后）：**

1. 在当前代码（M004 已合并）上创建测试项目 → 编译通过 → 测试通过
2. Checkout d81cd00 → 主项目重新编译（包含 DataParserService）→ 测试项目重新编译（引用更新后的 DLL）→ 测试通过
3. 回到当前分支 → 确认测试仍然通过
4. 结论：**同一份测试代码在两个版本上行为一致，证明 M004 没有破坏替换功能**

## User-Visible Outcome

### When this milestone is complete, the user can:

- 运行 `dotnet test` 看到真实数据回归测试全部通过
- 对 M004 功能瘦身后的代码有完整的端到端验证覆盖
- 在未来任何重构后运行同样的回归测试确认无破坏
- 确信测试逻辑经过 d81cd00 基准验证，不是只适配当前代码的"巧合测试"

### Entry point / environment

- Entry point: `dotnet test`（xUnit 测试项目）
- Environment: Windows 桌面应用开发环境
- 测试数据: `test_data/2026年4月23日/` 下的真实 Excel + Word 文件

## Completion Class

- Contract complete means: 独立 xUnit 测试项目创建完成，5 个验证维度的测试用例全部通过
- Integration complete means: 反射工厂正确适配 d81cd00 和 M004 后的构造函数，测试在两个版本上均通过
- Operational complete means: `dotnet test` 在无 External/ 文件的环境下全部通过

## Final Integrated Acceptance

To call this milestone complete, we must prove:

- 测试在 d81cd00 和 M004 后的代码上均编译通过且测试通过（通过反射工厂适配）
- 用 `test_data/2026年4月23日/LD68 IVDR.xlsx` + 至少 3 个不同 Chapter 的 docx 模板成功执行批量替换
- 输出文档的内容控件值与 Excel 数据一致
- 表格结构未被破坏（TableCell/TableCell 数量不变）
- 富文本格式（上标/下标）正确保留
- 页眉/页脚中的内容控件被正确替换
- 正文区域的批注被正确添加
- `dotnet test` 全部通过（包括新旧测试）

## Architectural Decisions

### 反射构建服务（版本兼容核心）

**Decision:** 使用反射检测 `DocumentProcessorService` 构造函数参数，自动适配有无 `IDataParser` 的两个版本

**Rationale:** 让同一份测试代码在 d81cd00（有 IDataParser）和 M004 后（无 IDataParser）上都能编译运行。测试代码不直接引用任何被 M004 删除的类型，编译时无硬依赖。

**Alternatives Considered:**
- `#if` 条件编译 — 需要维护两份代码，且需要在 csproj 中定义编译符号，不够优雅
- 只测底层服务（SafeTextReplacer 等）— 跳过了 DocumentProcessorService 的集成测试，覆盖面不足
- 分 Phase 执行，每个 Phase 改一次测试代码 — 无法证明"同一份代码在两个版本上行为一致"

### 独立 xUnit 测试项目

**Decision:** 创建 `Tests/E2ERegression/` 作为独立测试项目（非 Tools/ 下的控制台程序）

**Rationale:** 现有 E2ETest 工具是独立控制台程序，代码引用旧路径和 M004 将删除的 IDataParser 依赖，不可复用。xUnit 测试项目可以直接被 `dotnet test` 发现和运行，与现有测试流程一致。

**Alternatives Considered:**
- 修复现有 E2ETest 工具 — 是控制台程序不是测试项目，无法被 `dotnet test` 发现，且需大量修改
- 在现有 Tests/DocuFiller.Tests/ 中添加 — 会让测试项目膨胀，且真实数据测试需要较长运行时间，应该独立

### 测试数据路径策略

**Decision:** 测试数据使用相对路径 `test_data/2026年4月23日/`，通过向上导航找到项目根目录（查找 .sln 文件）

**Rationale:** 真实业务数据文件较大，不适合放在 Tests/Templates/ 中。使用已有的 test_data/ 目录保持数据组织不变。

**Alternatives Considered:**
- 复制文件到测试目录 — 44个 docx 文件复制浪费空间和时间
- 绝对路径 — 不可移植

### 验证维度设计

**Decision:** 五个验证维度：替换正确性、表格结构、富文本格式、页眉页脚、批注追踪

**Rationale:** 这五个维度覆盖了 DocuFiller 核心功能的完整链路。每个维度至少一个测试用例，从不同 Chapter 的模板中选择有代表性的文件。

**Alternatives Considered:**
- 仅验证替换成功 — 无法发现结构破坏等隐蔽问题
- 逐一验证每个模板的每个字段 — 44个模板太多，测试运行时间过长

## Error Handling Strategy

测试失败时应提供清晰的诊断信息：哪个模板失败、哪个字段不匹配、期望值 vs 实际值。使用 OpenXml SDK 读取输出文档进行断言，失败时输出具体的 XML 元素内容。

反射工厂在无法解析某个服务类型时，抛出清晰的异常说明缺少哪个类型、期望哪些类型。

## Risks and Unknowns

- **反射构建可靠性**: 反射构建服务需要处理所有构造函数参数的解析。如果构造函数参数顺序或类型在不同版本间有其他差异（不只是增删 IDataParser），需要逐一适配
- **Excel 数据格式不确定**: `LD68 IVDR.xlsx` 是两列还是三列格式，需要实际解析确认
- **模板中控件分布**: 44个模板中哪些包含表格控件、页眉页脚控件、富文本内容不确定，需要先用 GetContentControlsAsync 扫描
- **测试运行时间**: 44个模板全部处理可能需要较长时间，选择代表性子集控制在 2 分钟内
- **d81cd00 上编译**: checkout d81cd00 后需要确保整个解决方案能编译（M004 前的代码需要 External/update-client.exe）— 已知问题：csproj PreBuild 门禁。需要用 `-p:SkipExternalCheck=true` 或类似方式绕过，或在 d81cd00 上临时禁用 PreBuild

## Existing Codebase / Prior Art

- `Services/DocumentProcessorService.cs` — 核心处理管道。M004 前构造函数包含 IDataParser（9 个参数），M004 后移除 IDataParser（8 个参数）。替换管道内部逻辑不变。verified against current codebase state
- `Services/ExcelDataParserService.cs` — Excel 数据解析，自动检测两列/三列格式。两个版本均存在且接口不变
- `Services/ContentControlProcessor.cs` — 内容控件处理协调，构造函数（logger, commentManager, safeTextReplacer）两个版本不变
- `Services/CommentManager.cs` — 批注管理，构造函数不变
- `Services/SafeTextReplacer.cs` — 三种替换策略，构造函数不变
- `Services/SafeFormattedContentReplacer.cs` — 富文本格式替换，构造函数不变
- `Services/IDataParser.cs` — M004 前存在，M004 后删除。反射工厂不直接引用此类型
- `Services/DataParserService.cs` — M004 前存在，M004 后删除。反射工厂不直接引用此类型
- `Models/ProcessRequest.cs` — 单文件处理请求模型，不变
- `Models/FolderProcessRequest.cs` — 文件夹批量处理请求模型，不变
- `Models/ContentControlData.cs` — 内容控件数据模型，不变
- `Models/FormattedCellValue.cs` + `Models/TextFragment.cs` — 格式化值模型，不变
- `Tests/DocuFiller.Tests/Services/DocumentProcessorServiceIntegrationTests.cs` — 现有集成测试（直接 new 构造函数，M004 后需改）

## Relevant Requirements

- R020 — 测试全部通过
- 新增需求：真实数据端到端回归测试覆盖，版本兼容

## Scope

### In Scope

- 创建独立 xUnit 测试项目 `Tests/E2ERegression/`
- 实现反射服务工厂（ServiceFactory），自动适配 d81cd00 和 M004 后的构造函数
- 用 `test_data/2026年4月23日/LD68 IVDR.xlsx` + 代表性 docx 模板运行端到端替换
- 验证替换结果正确性（控件值与 Excel 数据匹配）
- 验证表格结构完整性（替换后 TableRow/TableCell 数量不变）
- 验证富文本格式保留（上标/下标 VerticalTextAlignment 正确）
- 验证页眉/页脚内容控件替换
- 验证正文区域批注追踪
- **在 d81cd00 上验证测试通过（基准验证）**

### Out of Scope / Non-Goals

- 不修改任何现有业务代码
- 不修改核心服务逻辑
- 不逐一测试 44 个模板的每个字段（选择代表性子集）
- 不做性能测试
- 不做 GUI 测试

## Technical Constraints

- Windows 环境，.NET 8 + WPF
- 测试数据路径使用相对路径发现策略
- `dotnet test` 必须全部通过
- 反射工厂不直接引用任何被 M004 删除的类型（IDataParser、DataParserService）
- d81cd00 编译时需处理 PreBuild 门禁（External/update-client.exe）

## Integration Points

- `IDocumentProcessor` / `DocumentProcessorService` — 通过反射工厂构建
- `IExcelDataParser` / `ExcelDataParserService` — 两个版本均存在，直接引用
- `IDocumentCleanupService` / `DocumentCleanupService` — 清理功能入口（可选验证）
- `test_data/2026年4月23日/` — 测试数据目录

## Testing Requirements

- 独立 xUnit 测试项目，可被 `dotnet test` 发现和运行
- 每个验证维度至少一个测试方法
- 测试失败时输出清晰的诊断信息
- 选择 3-5 个代表性模板（包含表格控件、页眉页脚控件、富文本等不同特征）
- 测试执行时间控制在合理范围（< 2 分钟）
- **必须在 d81cd00 和 M004 后的代码上都能编译通过且测试通过**

## Acceptance Criteria

- **S01:** 测试项目搭建完成，反射服务工厂实现，在当前代码上测试通过
- **S02:** 替换正确性 + 表格结构 + 富文本格式验证测试通过。Checkout d81cd00 后同一套测试也通过。
- **S03:** 页眉页脚替换 + 批注追踪验证测试通过，`dotnet test` 全部通过。回到当前分支确认仍通过。

## Open Questions

- `LD68 IVDR.xlsx` 的具体格式（两列/三列）和关键词数量需要实际解析确认
- 44 个模板中哪些包含表格控件和页眉页脚控件需要扫描确认 — 将在 S01 中通过 GetContentControlsAsync 扫描
- d81cd00 上编译的 PreBuild 门禁需要绕过策略（已知的 update-client.exe 问题）
