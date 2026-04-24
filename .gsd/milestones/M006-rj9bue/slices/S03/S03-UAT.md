# S03: d81cd00 基准跨版本验证 — UAT

**Milestone:** M006-rj9bue
**Written:** 2026-04-24T00:33:33.484Z

# UAT: S03 d81cd00 基准跨版本验证

## 前置条件
- 仓库包含 d81cd00 提交（基准版本，含 IDataParser，9 参数构造函数）
- 仓库包含 milestone/M006-rj9bue 分支（当前版本，不含 IDataParser，8 参数构造函数）
- `test_data/2026年4月23日/` 目录存在，包含 LD68 IVDR.xlsx 和 FD68 IVDR.xlsx
- E2E 测试项目在 Tests/E2ERegression/ 目录中

## 测试用例 1: d81cd00 基准版本构建 E2E 测试
1. 将 E2E 测试源文件复制到 d81cd00 工作树
2. 运行 `dotnet restore Tests/E2ERegression/E2ERegression.csproj`
3. 运行 `dotnet build Tests/E2ERegression/E2ERegression.csproj`
4. **预期**：构建成功，0 错误
5. **实际**：✅ 通过

## 测试用例 2: d81cd00 上 E2E 测试通过
1. 在 d81cd00 工作树上运行 `dotnet test Tests/E2ERegression/E2ERegression.csproj --no-build --verbosity normal`
2. **预期**：至少 25/27 测试通过（三列解析测试可能失败，因为 d81cd00 不支持三列格式）
3. **实际**：✅ 25/27 通过，2 个三列格式测试预期失败

## 测试用例 3: 替换正确性跨版本兼容
1. 在 d81cd00 上，7 个替换正确性测试全部通过
2. **预期**：文档处理管道在两个版本上行为一致
3. **实际**：✅ 7/7 通过（CE01、CE06-01、CE00 模板替换验证）

## 测试用例 4: ServiceFactory 条件注册自适应
1. 在 d81cd00 上，ServiceFactory 通过反射找到 IDataParser 并注册（9 参数构造函数）
2. 在当前分支上，ServiceFactory 跳过 IDataParser（8 参数构造函数）
3. **预期**：两个版本上 DI 容器均能正确解析 DocumentProcessorService
4. **实际**：✅ 通过（构建和测试均成功）

## 测试用例 5: 切回里程碑分支全量测试
1. 运行 `git checkout milestone/M006-rj9bue`
2. 运行 `dotnet build`
3. 运行 `dotnet test --verbosity normal`
4. **预期**：全部 135 个测试通过（108 DocuFiller.Tests + 27 E2ERegression），0 失败
5. **实际**：✅ 135/135 通过

## 边缘情况
- **NETSDK1022 重复编译**：d81cd00 上 IDataParser.cs 同时被条件 include 和通配符 include 匹配。已通过 Exclude 修复。
- **工作树残留**：从 d81cd00 切回里程碑分支后，无残留 obj/bin 缓存导致构建失败。

