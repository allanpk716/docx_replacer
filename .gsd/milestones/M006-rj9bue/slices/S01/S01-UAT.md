# S01: E2E 测试基础设施 + 三列格式替换正确性验证 — UAT

**Milestone:** M006-rj9bue
**Written:** 2026-04-24T00:11:06.577Z

## UAT: E2E 测试基础设施 + 替换正确性

### 前置条件
- test_data/2026年4月23日/ 目录存在
- dotnet SDK 8.0

### 步骤
1. `dotnet build Tests/E2ERegression/` → 0 errors
2. `dotnet test Tests/E2ERegression/` → 15 passed
3. `dotnet test` → 123 passed (108 existing + 15 E2E)

### 验证点
- ServiceFactory 构建处理器成功
- LD68 Excel 解析为三列（72+ 关键词）
- FD68 Excel 解析为两列（58+ 关键词）
- CE01/CE00/CE06-01 + LD68 数据替换成功
- CE01 + FD68 数据替换成功
- 跨格式对比输出不同
