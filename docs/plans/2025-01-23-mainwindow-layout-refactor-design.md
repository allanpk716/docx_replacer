# 主界面布局重构设计

## 概述

重构 DocuFiller 主界面布局，简化结构并整合功能入口，提升用户体验。

## 设计目标

1. 移除冗余的Tab页（数据预览、内容控件）
2. 整合分散的功能入口到统一位置
3. 简化界面层级结构
4. 将审核清理作为主流程的一部分

## 整体布局结构

### Grid行定义（简化为2行）

```xml
<Grid.RowDefinitions>
    <RowDefinition Height="Auto"/>  <!-- 标题栏 -->
    <RowDefinition Height="*"/>     <!-- TabControl -->
</Grid.RowDefinitions>
```

### 布局层级

```
Grid (Margin="15")
├── Row 0: 标题栏（"Word文档批量填充工具"）
└── Row 1: TabControl
    ├── TabItem: 关键词替换
    ├── TabItem: 审核清理
    └── TabItem: 工具
```

## 各Tab页详细内容

### 1. 关键词替换Tab页（原"文件设置"）

**内容区域：**
- 模板文件/文件夹选择（拖放区）
- 数据文件选择（拖放区）
- 输出目录设置

**底部区域（移入此Tab页）：**
- 进度显示区域（处理进度条 + 进度文字）
- 操作按钮（开始处理、取消处理、退出）

### 2. 审核清理Tab页

**完整嵌入CleanupWindow内容：**
- 拖放区域（支持文件/文件夹拖入）
- 文件列表（显示文件名、大小、状态）
  - 移除选中按钮
  - 清空列表按钮
- 进度条（独立的清理进度）
- 操作按钮（开始清理、关闭）

### 3. 工具Tab页（新增）

**三个功能入口：**
- 关键词编辑器
- JSON转Excel转换工具
- 检查更新

采用卡片式布局或按钮组，每个工具配有简短说明文字。

## 主要变更清单

### 删除的元素

| 元素 | 原位置 | 原因 |
|------|--------|------|
| `<Menu>` 菜单栏 | Grid.Row="0" | 功能整合到Tab页 |
| `UpdateBannerView` 更新横幅 | Grid.Row="1" | 用户主动检查更新 |
| "数据预览"Tab页 | TabControl | 不常用 |
| "内容控件"Tab页 | TabControl | 不常用 |
| 功能链接区域 | 标题栏下方 | 整合到"工具"Tab页 |

### 重命名

| 原名称 | 新名称 |
|--------|--------|
| 文件设置 | 关键词替换 |

### 结构调整

- 进度显示区域：从共享底部区域 → 移入"关键词替换"Tab页
- 操作按钮：从共享底部区域 → 移入"关键词替换"Tab页

## 实施要点

### MainWindow.xaml 修改

1. 简化 Grid.RowDefinitions 为2行
2. 删除 Menu 元素
3. 删除 UpdateBannerView 元素
4. 删除标题栏下方的功能链接 StackPanel
5. TabControl 中：
   - 重命名"文件设置"TabItem为"关键词替换"
   - 删除"数据预览"TabItem
   - 删除"内容控件"TabItem
   - 新增"审核清理"TabItem（嵌入CleanupWindow内容）
   - 新增"工具"TabItem
6. 在"关键词替换"Tab页底部添加进度显示和操作按钮
7. 删除底部的共享进度显示区域和操作按钮区域

### ViewModel 调整

- 确认 `OpenCleanupCommand` 可以复用或移除（因为清理功能直接在Tab页中）

### CleanupWindow 处理

- 保留 CleanupWindow.xaml 和相关代码
- 在新的Tab页中复用其布局和逻辑
- 考虑是否需要重构为 UserControl 以便复用
