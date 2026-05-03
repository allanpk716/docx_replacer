# S01: TextBox 拖放事件改为 Preview 隧道 — UAT

**Milestone:** M017
**Written:** 2026-05-03T04:20:08.095Z

# S01: TextBox 拖放事件改为 Preview 隧道 — UAT

**Milestone:** M017
**Written:** 2026-05-03

## UAT Type

- UAT mode: human-experience
- Why this mode is sufficient: 此切片修复 WPF 拖放交互，需人工拖放操作验证视觉效果和功能正确性，无自动化测试替代方案。

## Preconditions

- 编译成功：`dotnet build` 0 错误
- 启动 DocuFiller GUI（无命令行参数）

## Smoke Test

1. 启动应用
2. 从资源管理器拖一个 .docx 文件到模板 TextBox
3. **Expected:** 鼠标悬停时 TextBox 出现蓝色高亮，放下后路径填入 TextBox

## Test Cases

### 1. 模板 TextBox 拖入 .docx 文件

1. 启动应用，确认模板 TextBox 显示占位提示文字
2. 从资源管理器拖一个 .docx 文件到模板 TextBox
3. **Expected:** 悬停时蓝色高亮，放下后 TextBox 显示文件完整路径，右侧显示模板信息（控件数量等）

### 2. 数据 TextBox 拖入 .xlsx 文件

1. 从资源管理器拖一个 .xlsx 文件到数据 TextBox
2. **Expected:** 悬停时蓝色高亮，放下后 TextBox 显示文件完整路径，数据预览区域显示 Excel 内容

### 3. 模板 TextBox 拖入文件夹

1. 从资源管理器拖一个文件夹到模板 TextBox
2. **Expected:** 蓝色高亮，路径填入，应用按文件夹模式处理

### 4. 拖入不匹配文件类型

1. 拖一个 .pdf 文件到模板 TextBox
2. **Expected:** 不接受拖放或显示错误提示，路径不填入

### 5. 清理区域拖放不受影响

1. 切换到清理标签页
2. 拖一个 .docx 文件到清理拖放区域
3. **Expected:** 拖放功能正常工作，行为与修改前一致

## Edge Cases

### 拖放取消

1. 拖文件到 TextBox 悬停后，移出窗口或按 Esc 取消
2. **Expected:** 蓝色高亮消失，TextBox 内容不变

### 快速连续拖放

1. 快速拖入一个文件，立即再拖入另一个文件替换
2. **Expected:** 两次拖放均正常处理，最终显示第二个文件的路径

## Failure Signals

- 拖放时鼠标显示"禁止"图标 → 修复未生效
- 路径填入后未触发模板验证或数据预览 → 事件处理链断裂
- 清理区域拖放失效 → 改动意外影响了清理区域事件

## Not Proven By This UAT

- 此 UAT 不验证 CLI 模式功能（CLI 不涉及拖放）
- 不验证自动化测试覆盖率（当前无拖放单元测试）

## Notes for Tester

- 此修复仅涉及事件路由策略变更（冒泡→隧道），事件处理逻辑完全未改动
- 如果拖放仍然被拦截，可能是 WPF 版本差异或其他外部因素，需进一步排查
