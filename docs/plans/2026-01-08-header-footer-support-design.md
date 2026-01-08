# 页眉页脚内容控件支持功能设计

**日期**: 2026-01-08
**作者**: Claude Code
**状态**: 设计评审中

## 问题概述

当前 DocuFiller 应用程序无法替换位于页眉或页脚中的"纯文本内容控件"。用户报告这一功能缺陷影响其工作流程。

## 根本原因分析

代码库存在两个文档处理层：

1. **DocumentProcessorService** - 主服务，负责业务流程编排
   - 支持批量处理、并行执行、进度报告、取消令牌
   - **缺陷**: 只处理文档主体，忽略页眉页脚

2. **OpenXmlDocumentHandler** - 辅助工具类
   - **优势**: 完整支持页眉页脚处理
   - **问题**: 从未被使用，造成代码冗余

核心问题位于 `DocumentProcessorService.cs:230`:
```csharp
// 只处理文档主体
List<SdtElement> contentControls = document.MainDocumentPart.Document.Descendants<SdtElement>().ToList();
```

## 解决方案

完全重写文档处理层，合并两个处理器的优势。

### 架构设计

```
DocumentProcessorService (主服务)
├── 职责：业务流程编排
│   ├── 文件复制和验证
│   ├── 数据解析和映射
│   ├── 批量处理和并行控制
│   ├── 进度报告和取消令牌
│   └── 错误处理和日志记录
│
└── 依赖：
    ├── ContentControlProcessor (内容控件处理)
    │   └── 增强：支持页眉页脚
    ├── CommentManager (批注管理)
    ├── TemplateCacheService (模板缓存)
    ├── DataParserService (数据解析)
    ├── FileService (文件操作)
    └── ProgressReporterService (进度报告)
```

### 核心变更

1. **删除** `OpenXmlDocumentHandler.cs`
2. **增强** `ContentControlProcessor` 以支持页眉页脚
3. **保持** `IDocumentProcessor` 接口不变
4. **保留** 所有现有功能（批量处理、进度报告、批注、模板缓存）

### 关键实现

#### ContentControlProcessor 增强

```csharp
public enum ContentControlLocation
{
    Body,
    Header,
    Footer
}

public void ProcessContentControlsInDocument(
    WordprocessingDocument document,
    Dictionary<string, object> data,
    CancellationToken cancellationToken)
{
    var mainPart = document.MainDocumentPart;

    // 处理文档主体
    ProcessControlsInPart(
        mainPart.Document,
        data,
        document,
        ContentControlLocation.Body,
        cancellationToken);

    // 处理所有页眉
    foreach (var headerPart in mainPart.HeaderParts)
    {
        ProcessControlsInHeaderPart(
            headerPart,
            data,
            document,
            cancellationToken);
    }

    // 处理所有页脚
    foreach (var footerPart in mainPart.FooterParts)
    {
        ProcessControlsInFooterPart(
            footerPart,
            data,
            document,
            cancellationToken);
    }
}
```

#### CommentManager 增强

添加页眉页脚批注支持：
- `AddCommentToHeaderControl`
- `AddCommentToFooterControl`

#### 进度报告扩展

```csharp
ProgressEventArgs {
    ProgressPercentage: int,
    StatusMessage: string,
    CurrentFileName: string,
    CurrentSection: string,      // 新增
    ProcessedControls: int,       // 新增
    TotalControls: int            // 新增
}
```

### 数据流

```
ProcessSingleDocumentAsync
    ↓
复制模板文件
    ↓
打开 WordprocessingDocument
    ↓
ContentControlProcessor.ProcessContentControlsInDocument
    ├─→ 处理文档主体
    ├─→ 处理页眉 (所有 HeaderParts)
    └─→ 处理页脚 (所有 FooterParts)
    ↓
保存文档
    ↓
返回结果
```

### 错误处理

| 场景 | 处理方式 |
|------|----------|
| 文档主体控件失败 | 记录错误，继续处理页眉页脚 |
| 单个页眉/页脚失败 | 记录警告，继续其他部分 |
| 文档打开失败 | 返回失败，不生成输出文件 |
| 数据缺少匹配字段 | 记录警告，跳过该控件 |

## 测试策略

### 单元测试

- `Test_ProcessBodyControls_Success`
- `Test_ProcessHeaderControls_Success`
- `Test_ProcessFooterControls_Success`
- `Test_ProcessMixedControls_AllLocations`
- `Test_ControlMissingData_SkipsGracefully`

### 集成测试用例

| 用例 | 描述 | 预期结果 |
|------|------|----------|
| 带页眉控件 | 模板包含页眉控件 | 页眉控件被正确替换 |
| 带页脚控件 | 模板包含页脚控件 | 页脚控件被正确替换 |
| 混合位置 | 正文、页眉、页脚都有控件 | 所有位置控件都被替换 |
| 多个页眉页脚 | 首页、奇偶页不同页眉页脚 | 所有页眉页脚都被处理 |

## 实施步骤

1. 增强 `ContentControlProcessor`
2. 增强 `CommentManager`
3. 更新 `DocumentProcessorService`
4. 清理代码（删除 `OpenXmlDocumentHandler`）
5. 测试验证

## 风险和缓解

| 风险 | 缓解措施 |
|------|----------|
| 页眉页脚批注引用错误 | 充分测试批注功能 |
| 首页/奇偶页页眉未处理 | 遍历所有 HeaderParts/FooterParts |
| 破坏现有功能 | 保留现有方法签名，逐步迁移 |
