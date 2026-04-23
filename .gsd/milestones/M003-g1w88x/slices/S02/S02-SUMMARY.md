---
id: S02
parent: M003-g1w88x
milestone: M003-g1w88x
provides:
  - ["Updated README.md with 14-service architecture table, 6 feature modules, Excel tri-column format", "Updated CLAUDE.md with 17 interfaces, 16 data models, Excel processing path, DI lifecycle config"]
requires:
  - slice: S01
    provides: docs/DocuFiller产品需求文档.md and docs/DocuFiller技术架构文档.md as authoritative reference
affects:
  - ["S03"]
key_files:
  - ["README.md", "CLAUDE.md"]
key_decisions:
  - ["保留 CLAUDE.md 表格内容控件处理部分不变（已准确）", "数据模型以表格形式呈现而非完整类定义，保持 CLAUDE.md 简洁性"]
patterns_established:
  - ["文档分层模式：docs/(详细PRD) → README.md(用户入口) → CLAUDE.md(AI上下文)", "I 前缀 grep 计数作为文档完整性快速验证手段"]
observability_surfaces:
  - none
drill_down_paths:
  - [".gsd/milestones/M003-g1w88x/slices/S02/tasks/T01-SUMMARY.md", ".gsd/milestones/M003-g1w88x/slices/S02/tasks/T02-SUMMARY.md"]
duration: ""
verification_result: passed
completed_at: 2026-04-23T10:36:50.645Z
blocker_discovered: false
---

# S02: README.md + CLAUDE.md 更新

**README.md 重写为 14 服务接口架构表 + 6 功能模块完整覆盖 + Excel 三列格式说明；CLAUDE.md 扩展至 17 接口 + 16 数据模型 + Excel 双格式处理路径 + DI 生命周期配置**

## What Happened

## 完成的工作

T01 全面重写了 README.md，T02 在此基础上更新了 CLAUDE.md。两份文档现在与代码库当前状态完全对齐。

### README.md 更新（T01）
- 主要功能从简单列表扩展为 6 个功能模块详细描述
- 服务层架构表从 6 个接口扩展到 14 个，每个标注实现类和职责
- 核心数据模型表从 4 个扩展到 15 个
- 项目结构反映所有实际目录（含 Tools/、Tests/、Update/ 子系统）
- Excel 使用方法补充三列格式说明和自动检测规则
- 技术框架版本号确认为 DocumentFormat.OpenXml 3.0.1、EPPlus 7.5.2、Newtonsoft.Json 13.0.3

### CLAUDE.md 更新（T02）
- 服务层架构表扩展至 14 接口 + 2 非接口处理器
- 关键数据模型扩展至 16 个完整列表
- 新增 Excel 双格式处理路径章节（DetectExcelFormat 机制）
- 新增审核清理、批注追踪、富文本替换等功能说明
- 补充 DI 生命周期配置（Singleton vs Transient）
- 配置系统更新为 appsettings.json + IOptions&lt;T&gt; 模式

### 关键决策
- 保留 CLAUDE.md 的表格内容控件处理部分不变（已准确）
- 数据模型以表格形式呈现而非完整类定义，保持简洁性
- IJsonEditorService 在 CLAUDE.md 中保留但 README.md 中不纳入（功能已删除）

## Verification

## 验证结果

所有 Must-Have 检查全部通过：

1. **README.md 服务层架构表** — 14 个 I 前缀接口全部存在 ✓
2. **README.md 6 功能模块** — 文件输入、Excel 双数据源、文档处理、批注追踪、审核清理、转换工具均覆盖 ✓
3. **README.md Excel 三列格式** — 5 处提及 ✓
4. **README.md 项目结构** — 含所有实际目录 ✓
5. **CLAUDE.md 服务层架构** — 17 个唯一 I 前缀标识符 (>=14) ✓
6. **CLAUDE.md 数据模型** — FormattedCellValue、TextFragment、ExcelFileSummary、CleanupFileItem、FolderProcessRequest、InputSourceType 等均存在 ✓
7. **CLAUDE.md Excel 处理路径** — DetectExcelFormat、两列/三列格式说明均包含 ✓
8. **文档一致性** — 14 核心接口在两份文档中均存在，无术语矛盾 ✓

验证证据：grep 计数命令全部返回预期结果，exit code 均为 0。

## Requirements Advanced

None.

## Requirements Validated

- R007 — README.md 包含 14 个服务接口架构表（grep 验证全部存在）、6 个功能模块完整覆盖、Excel 两列/三列格式说明、准确项目结构
- R008 — CLAUDE.md 包含 17 个唯一 I 前缀标识符（>=14）、16 个关键数据模型、DetectExcelFormat 处理路径说明、DI 生命周期配置

## New Requirements Surfaced

None.

## Requirements Invalidated or Re-scoped

None.

## Operational Readiness

None.

## Deviations

None.

## Known Limitations

["IJsonEditorService 在 CLAUDE.md 中保留但 README.md 中未纳入（功能已删除，保持历史记录）", "NuGet 包版本号未交叉验证（依赖 csproj 文件声明，未逐一检查）"]

## Follow-ups

None.

## Files Created/Modified

None.
