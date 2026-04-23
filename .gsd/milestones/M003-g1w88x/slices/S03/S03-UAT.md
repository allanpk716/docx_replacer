# S03: S03: 功能级文档更新 — UAT

**Milestone:** M003-g1w88x
**Written:** 2026-04-23T10:45:26.213Z

# S03: 功能级文档更新 — UAT

**Milestone:** M003-g1w88x
**Written:** 2026-04-23

## UAT Type

- UAT mode: artifact-driven
- Why this mode is sufficient: 本切片仅涉及文档更新，无需运行时环境。通过文件内容检查即可验证文档正确性。

## Preconditions

- docs/excel-data-user-guide.md 存在
- docs/features/header-footer-support.md 存在
- docs/批注功能说明.md 存在

## Smoke Test

打开 docs/excel-data-user-guide.md，确认文档包含"三列格式"相关章节。

## Test Cases

### 1. Excel 用户指南包含三列格式说明

1. 读取 docs/excel-data-user-guide.md
2. 搜索"三列"关键词
3. **Expected:** 找到三列格式的完整说明，包括格式自动检测、列定义、示例表格

### 2. Excel 用户指南无 TBD/TODO

1. 读取 docs/excel-data-user-guide.md
2. 搜索 "TBD" 或 "TODO"
3. **Expected:** 无匹配结果

### 3. 页眉页脚文档批注行为正确

1. 读取 docs/features/header-footer-support.md
2. 查找批注相关章节
3. **Expected:** 明确说明"仅正文区域支持批注"，页眉页脚不支持批注

### 4. 页眉页脚文档无 TBD/TODO

1. 读取 docs/features/header-footer-support.md
2. 搜索 "TBD" 或 "TODO"
3. **Expected:** 无匹配结果

### 5. 批注功能说明与代码一致

1. 读取 docs/批注功能说明.md
2. 确认以下内容：CommentManager 方法描述、批注 ID 全局唯一性、仅正文支持批注、批注格式
3. **Expected:** 所有描述与 CommentManager.cs 和 ContentControlProcessor.cs 代码行为一致

### 6. 批注功能说明无 TBD/TODO

1. 读取 docs/批注功能说明.md
2. 搜索 "TBD" 或 "TODO"
3. **Expected:** 无匹配结果

## Edge Cases

### 文档间术语一致性

1. 对比三份文档中关于"批注"的描述
2. **Expected:** header-footer-support.md 和批注功能说明.md 对"页眉页脚不支持批注"的描述一致

## Failure Signals

- 任何文档包含 TBD/TODO 标记
- header-footer-support.md 仍声称页眉页脚支持批注
- excel-data-user-guide.md 缺少三列格式说明

## Not Proven By This UAT

- 代码实现的正确性（仅验证文档与已知代码行为一致）
- 文档在应用内展示的用户体验

## Notes for Tester

- 这三份文档均为 Markdown 格式，可直接用文本编辑器或浏览器查看
- 批注功能说明.md（docs/批注功能说明.md）的文件名为中文
