# S02: 两列格式 + 表格/富文本/页眉页脚/批注验证 — UAT

**Milestone:** M006-rj9bue
**Written:** 2026-04-24T00:14:41.993Z

## UAT: 两列格式 + 结构验证

### 前置条件
- test_data/2026年4月23日/ 存在
- dotnet SDK 8.0

### 步骤
1. `dotnet test Tests/E2ERegression/` → 27 passed
2. `dotnet test` → 135 passed

### 验证维度
- **表格结构**: CE01/CE06-01 替换后 TableRow/TableCell 数量不变
- **富文本**: LD68 的上标格式保留（×10^9/L 模式），FD68 无额外格式
- **页眉页脚**: CE01 header 含 Lyse/BH-LD68，CE00 header 验证通过
- **批注追踪**: 正文区域添加批注，页眉区域无批注
- **两列格式**: FD68 (两列) 与 LD68 (三列) 在相同模板上产生不同输出
