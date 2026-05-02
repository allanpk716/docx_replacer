# M006-rj9bue: 真实数据端到端回归测试

**Vision:** 用真实业务数据创建独立的端到端回归测试项目：LD68 IVDR.xlsx（三列格式 74 关键词，含上标富文本）+ FD68 IVDR.xlsx（两列格式 59 关键词）+ 43 个 Word 模板。通过 DI 条件类型注册实现 d81cd00 基准版本兼容。覆盖替换正确性（两列+三列）、表格结构完整性、富文本格式保留、页眉页脚替换、批注追踪五个维度。

## Success Criteria

- E2E 测试项目创建完成，独立 xUnit 项目可被 dotnet test 发现运行
- ServiceFactory 在当前代码（M004 后）上成功构建 DocumentProcessorService 并通过测试
- LD68 IVDR.xlsx（三列格式 74 关键词）正确解析，替换后控件值与数据匹配
- FD68 IVDR.xlsx（两列格式 59 关键词）正确解析，替换后控件值与数据匹配
- 表格结构未被破坏（替换前后 TableRow/TableCell 数量不变）
- 富文本上标格式正确保留（LD68 Excel 中 3 个上标单元格输出为 VerticalTextAlignment=Superscript）
- 页眉/页脚中的内容控件被正确替换
- 正文区域的批注被正确添加
- dotnet test 全部通过（包括 108 个现有测试 + 新增 E2E 测试）

## Slices

- [x] **S01: S01** `risk:high` `depends:[]`
  > After this: 运行 dotnet test --filter E2ERegression，基础烟雾测试和替换正确性测试通过。用 LD68 IVDR.xlsx (三列) + CE01/CE06-01 模板验证替换成功。

- [x] **S02: S02** `risk:medium` `depends:[]`
  > After this: 运行 dotnet test --filter E2ERegression，全部 5 个验证维度通过。FD68 (两列) 和 LD68 (三列) 均验证。表格结构完整，富文本上标保留，页眉页脚替换正确，批注追踪正常。

- [x] **S03: S03** `risk:medium` `depends:[]`
  > After this: Checkout d81cd00 → 构建 E2E 测试 → 测试通过 → 切回里程碑分支 → 测试仍通过。

## Boundary Map

### S01: E2E 测试基础设施 + 基本替换验证

| Boundary | Direction | Contract |
|----------|-----------|----------|
| Tests/E2ERegression/ → DocuFiller source (via source linking) | In | 源文件链接引用核心服务文件，条件链接 DataParserService/IDataParser |
| ServiceFactory → DocumentProcessorService | In | ServiceCollection DI 自动解析构造函数参数，条件注册已删除类型 |
| E2E tests → test_data/ (via TestDataHelper) | In | 向上导航查找 test_data/2026年4月23日/ 目录 |
| E2E tests → dotnet test | Out | xUnit 测试可被 dotnet test 发现和运行 |
| E2ERegression.csproj → DocuFiller.sln | Out | 新测试项目添加到解决方案 |

### S02: 两列/三列格式 + 结构验证（表格/富文本/页眉页脚/批注）

| Boundary | Direction | Contract |
|----------|-----------|----------|
| S02 → TestDataHelper (S01) | In | 使用 S01 建立的测试数据路径发现和 ServiceFactory |
| S02 → LD68 IVDR.xlsx (三列格式) | In | 74 行数据，含 3 个上标富文本单元格 |
| S02 → FD68 IVDR.xlsx (两列格式) | In | 59 行数据，纯文本无富文本 |
| S02 → 代表性 docx 模板 | In | CE01(82控件)、CE06-01(49控件)、CE00(35控件) 覆盖不同 Chapter |

### S03: d81cd00 基准跨版本验证

| Boundary | Direction | Contract |
|----------|-----------|----------|
| S03 → d81cd00 commit | In | git checkout --detach d81cd00，源文件回退到基准版本 |
| S03 → E2E test project (untracked) | In | 未跟踪的测试文件在 checkout 后保留在工作树中 |
| S03 → milestone/M006-rj9bue branch | Out | 验证完成后切回里程碑分支，确认测试仍通过 |
