---
phase: M006-rj9bue
phase_name: 真实数据端到端回归测试
project: DocuFiller
generated: 2026-04-24T00:40:00.000Z
counts:
  decisions: 2
  lessons: 4
  patterns: 3
  surprises: 1
missing_artifacts: []
---

### Decisions

- **SDT 标签查找 vs 全文搜索用于页眉页脚验证**: 选择 SDT 标签查找（SdtElement.Descendants<SdtElement>()）验证页眉页脚内容控件替换，而非全文搜索。原因：SDT 标签查找精确匹配特定控件，不受文档其他位置同名文本干扰。
  Source: S02-SUMMARY.md/key_decisions

- **WordprocessingCommentsPart vs CommentsPart 用于批注验证**: 选择 WordprocessingCommentsPart（OpenXml 3.x API）验证批注是否存在。原因：项目中实际使用的批注 API 是 WordprocessingCommentsPart，与代码实现一致。
  Source: S02-SUMMARY.md/key_decisions

### Lessons

- **csproj 通配符 Include 和条件 Include 重叠导致 NETSDK1022**: 当 `<Compile Include="../*.cs">` 通配符和 `<Compile Include="..IDataParser.cs" Condition="Exists(...)">` 条件包含指向同一文件时，MSBuild 报 NETSDK1022 重复编译项错误。修复方法是在通配符 include 上添加 `Exclude="..IDataParser.cs"`。
  Source: S03-SUMMARY.md/关键发现

- **反射 FindType() 条件注册实现跨版本 DI 兼容**: 通过 `Type.GetType()` 或 `AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes())` 查找已编译类型是否存在，动态决定是否注册 DI 服务映射。DI 容器自动解析构造函数参数，自然处理不同版本的构造函数签名差异。
  Source: S03-SUMMARY.md/关键发现

- **checkout --detach 后未跟踪文件保留在工作树中**: git checkout --detach 切换到基准版本时，未跟踪的文件（如新建的 E2E 测试项目）保留在工作树中。可以利用此特性在基准版本代码上验证新测试。
  Source: S03-SUMMARY.md/T01

- **向上导航查找资源需要设置合理上限**: TestDataHelper 从测试程序集位置向上导航查找 test_data/ 目录时，需要设置最大迭代次数（如 20 级）防止无限循环，同时应检查到达文件系统根目录的情况。
  Source: S01-SUMMARY.md/What Happened

### Patterns

- **DI 条件类型注册模式**: 通过反射检测已编译类型是否存在，动态决定 DI 服务注册。适用于需要向后兼容不同代码版本（如构造函数参数变化）的测试场景。ServiceFactory 在 E2ERegression 项目中实现了此模式。
  Source: S01-SUMMARY.md/What Happened

- **向上导航资源发现模式**: 从测试程序集执行目录向上逐级查找资源目录（test_data/），覆盖 worktree 子目录和主仓库两种场景。使用 Directory.EnumerateFiles 验证目标文件存在。
  Source: S01-SUMMARY.md/What Happened

- **csproj 条件编译 Include + Exclude 配对**: 当通配符 Include 和条件 Include 可能重叠时，必须配对使用 Exclude 属性。模式为 `Include="*.cs" Exclude="specific.cs"` + `Include="specific.cs" Condition="Exists(...)"`。
  Source: S03-SUMMARY.md/T01

### Surprises

- **E2E 测试数量超出估算**: 计划估算 123 个测试（108 现有 + 15 E2E），实际为 135 个（108 + 27 E2E）。原因：E2E 测试对 LD68 和 FD68 两个数据源分别运行替换正确性验证，以及 S02 新增的 12 个维度验证测试。
  Source: S02-SUMMARY.md/What Happened
