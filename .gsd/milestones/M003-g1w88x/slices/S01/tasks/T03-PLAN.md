---
estimated_steps: 6
estimated_files: 4
skills_used: []
---

# T03: 删除 .trae/documents/ 下的 4 份旧文档

删除 .trae/documents/ 目录下的全部 4 个文件：

1. `DocuFiller产品需求文档.md` — 已迁移到 docs/DocuFiller产品需求文档.md（T01 产出）
2. `DocuFiller技术架构文档.md` — 已迁移到 docs/DocuFiller技术架构文档.md（T02 产出）
3. `JSON关键词编辑器产品需求文档.md` — 不迁移，直接删除（D004：JSON 编辑器功能已移除）
4. `JSON关键词编辑器技术架构文档.md` — 不迁移，直接删除（D004）

删除后检查 .trae/ 目录是否为空，如果为空则删除 .trae/ 目录本身。

## Inputs

- `docs/DocuFiller产品需求文档.md`
- `docs/DocuFiller技术架构文档.md`

## Expected Output

- `.trae/documents/`

## Verification

! test -d .trae/documents/ && test -f docs/DocuFiller产品需求文档.md && test -f docs/DocuFiller技术架构文档.md
