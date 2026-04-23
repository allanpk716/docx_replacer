# M003-g1w88x: 文档全面更新

**Gathered:** 2026-04-23
**Status:** Ready for planning

## Project Description

全面更新 DocuFiller 项目的文档体系，将所有文档对齐到代码库当前状态。涉及 7 份文档的更新/迁移和 4 份旧文件的清理删除。

## Why This Milestone

产品需求文档和技术架构文档自 2025 年 9 月以来未更新，严重落后于代码实际状态（只描述了 JSON-only 基础版本）。README 和 CLAUDE.md 也有多处过时信息。新开发者（或 AI 编码助手）无法通过文档准确理解项目。

## User-Visible Outcome

### When this milestone is complete, the user can:

- 在 docs/ 目录下找到完整准确的产品需求文档和技术架构文档
- 通过 README.md 了解所有功能和项目全貌
- 通过 CLAUDE.md 获得准确的开发上下文
- Excel 用户指南包含三列格式说明
- 功能级文档与代码实现一致

### Entry point / environment

- Entry point: docs/ 目录下的文档文件
- Environment: 本地文件系统

## Completion Class

- Contract complete means: 所有文档内容与代码库一致，代码示例可验证
- Integration complete means: 文档之间无矛盾，术语一致
- Operational complete means: .trae/documents/ 已清理，docs/ 目录结构完整

## Final Integrated Acceptance

To call this milestone complete, we must prove:

- 产品需求文档涵盖所有现有功能（对比代码库 15 个服务接口）
- 技术架构文档的接口定义与实际代码匹配
- README 和 CLAUDE.md 的服务列表完整且准确
- Excel 用户指南包含三列格式完整说明
- .trae/documents/ 目录下文件已删除

## Architectural Decisions

### 文档归属位置
**Decision:** 产品需求和技术架构文档从 .trae/documents/ 迁移到 docs/
**Rationale:** .trae/ 是 Trae IDE 的约定目录，文档应与项目其他文档统一存放
**Alternatives Considered:**
- 保留在 .trae/ 下 — 分散不便管理

### JSON 编辑器文档处理
**Decision:** 不迁移，直接删除
**Rationale:** JSON 编辑器功能已从代码中移除，对应文档无保留价值
**Alternatives Considered:**
- 迁移作为历史参考 — 会误导开发者以为功能存在

### 更新机制文档策略
**Decision:** 不更新版本管理、外部配置、部署指南等与更新机制相关的文档
**Rationale:** 用户明确要求更新机制不写入文档
**Alternatives Considered:**
- 正常更新 — 与用户意愿矛盾

### 技术架构文档深度
**Decision:** 保持详细风格，包含完整的 C# 接口定义、数据模型代码和 Mermaid 图
**Rationale:** 现有文档的详细程度被用户认可，开发者文档需要技术细节
**Alternatives Considered:**
- 精简描述 — 信息密度不够

## Error Handling Strategy

不适用。纯文档工作，不涉及运行时错误处理。

## Risks and Unknowns

- 页眉页脚批注的实际行为可能与现有文档描述有偏差 — 需逐文件核对代码
- 服务接口定义可能存在未使用的或实验性的接口 — 需判断哪些应写入文档

## Existing Codebase / Prior Art

- `README.md` — 已有部分更新的项目入口文档
- `CLAUDE.md` — AI 编码助手上下文文件
- `docs/excel-data-user-guide.md` — Excel 两列格式用户指南
- `docs/features/header-footer-support.md` — 页眉页脚功能说明
- `docs/批注功能说明.md` — 批注功能说明
- `.trae/documents/DocuFiller产品需求文档.md` — 过时的产品需求文档
- `.trae/documents/DocuFiller技术架构文档.md` — 过时的技术架构文档

## Relevant Requirements

- R005 — 产品需求文档全面更新
- R006 — 技术架构文档全面更新
- R007 — README.md 对齐当前项目状态
- R008 — CLAUDE.md 对齐当前开发环境
- R009 — Excel 用户指南增加三列格式说明
- R010 — 页眉页脚和批注功能说明对齐实现
- R011 — .trae/documents/ 文件迁移和清理

## Scope

### In Scope

- 7 份文档更新/迁移
- .trae/documents/ 清理删除
- 所有文档内容与代码库对齐

### Out of Scope / Non-Goals

- docs/plans/ 历史设计文档
- docs/VERSION_MANAGEMENT.md
- docs/EXTERNAL_SETUP.md
- docs/deployment-guide.md
- JSON 关键词编辑器相关内容
- 任何代码变更

## Technical Constraints

- 纯文档工作，不修改代码
- 代码示例必须与实际代码精确匹配
- 中文为主

## Integration Points

- 文档之间的一致性（README ↔ CLAUDE.md ↔ 架构文档）
- 代码示例与源代码的精确对应

## Testing Requirements

- 逐份文档校验：服务列表 vs 代码库实际接口
- 代码示例 vs 源代码精确匹配
- 项目结构 vs 文件系统实际路径

## Acceptance Criteria

- 产品需求文档涵盖所有功能模块（关键词替换、审核清理、工具三个 Tab）
- 技术架构文档包含 15 个服务接口的完整定义
- README.md 功能列表与实际一致
- CLAUDE.md 服务层架构表完整
- Excel 用户指南包含三列格式
- 功能级文档与代码实现一致
- .trae/documents/ 已清理

## Open Questions

- None.
