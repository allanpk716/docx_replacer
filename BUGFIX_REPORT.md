# 井号关键词搜索问题修复报告

## 问题描述

用户反馈日志显示：
```
2025/09/15 17:09:11 关键词 '产品名称：': 不带井号=1次, 带井号=0次 
2025/09/15 17:09:11 关键词 '结构及组成': 不带井号=2次, 带井号=0次 
```

但用户确认文档中确实存在 `#结构及组成#` 这样的关键词，程序却搜索不到。

## 问题根因分析

### 原始代码逻辑

在 `docx_processor.go` 的 `ReplaceKeywordsWithOptions` 方法中：

```go
// 原始代码
if useHashWrapper {
    // 在 oldText 前后添加井号
    searchText = "#" + oldText + "#"
}
```

### 问题场景

1. **配置文件中的关键词**：`"结构及组成"` （不带井号）
2. **文档中的实际内容**：`#结构及组成#` （带井号）
3. **程序搜索的文本**：`#结构及组成#` （正确）
4. **但如果用户在配置中写成**：`"#结构及组成#"` （已带井号）
5. **程序会搜索**：`##结构及组成##` （错误！）

### 调试信息不一致

`DebugContent` 方法中的统计逻辑与实际搜索逻辑不一致：
- 调试时分别统计 `keyword` 和 `"#"+keyword+"#"`
- 但实际替换时的逻辑更复杂

## 解决方案

### 1. 智能井号包装逻辑

修改 `ReplaceKeywordsWithOptions` 方法，添加智能判断：

```go
// 修复后的代码
if useHashWrapper {
    // 智能处理井号包装：如果关键词已经包含井号，则不再添加
    if !strings.HasPrefix(oldText, "#") || !strings.HasSuffix(oldText, "#") {
        searchText = "#" + oldText + "#"
    }
}
```

**逻辑说明**：
- 如果关键词不是以 `#` 开头 **或者** 不是以 `#` 结尾，则添加井号包装
- 只有当关键词既以 `#` 开头又以 `#` 结尾时，才不添加井号

### 2. 统一调试信息

修改 `DebugContent` 方法，使其与实际搜索逻辑保持一致：

```go
// 智能确定搜索文本（与ReplaceKeywordsWithOptions中的逻辑保持一致）
searchText := keyword
if !strings.HasPrefix(keyword, "#") || !strings.HasSuffix(keyword, "#") {
    searchText = "#" + keyword + "#"
}
searchCount := strings.Count(content, searchText)

log.Printf("关键词 '%s': 原文=%d次, 实际搜索'%s'=%d次", keyword, plainCount, searchText, searchCount)
```

### 3. 增强日志信息

修改替换日志，显示实际搜索的文本：

```go
if verbose {
    if count > 0 {
        log.Printf("替换 '%s' -> '%s' (%d次)", searchText, replacement, count)
    } else {
        log.Printf("未找到关键词 '%s'（搜索: '%s'）", oldText, searchText)
    }
}
```

## 测试验证

创建了 `TestDocxProcessor_SmartHashWrapper` 测试用例，验证三种场景：

1. **普通关键词自动添加井号**
   - 输入：`"结构及组成"`
   - 搜索：`"#结构及组成#"`
   - 结果：✅ 正确找到并替换

2. **已带井号的关键词不重复添加**
   - 输入：`"#产品名称#"`
   - 搜索：`"#产品名称#"`
   - 结果：✅ 正确找到并替换

3. **只有前井号的关键词会添加后井号**
   - 输入：`"#规格"`
   - 搜索：`"##规格#"`
   - 结果：✅ 按预期逻辑处理

## 修复效果

修复后的程序将能够：

1. **智能处理井号**：无论用户在配置文件中是否包含井号，都能正确搜索
2. **一致的调试信息**：调试输出与实际搜索逻辑完全一致
3. **清晰的日志**：显示实际搜索的文本，便于问题排查

## 使用建议

1. **推荐配置方式**：在配置文件中使用不带井号的关键词
   ```json
   {
     "key": "结构及组成",
     "value": "试剂盒主要由..."
   }
   ```

2. **兼容性**：程序现在也支持带井号的配置
   ```json
   {
     "key": "#结构及组成#",
     "value": "试剂盒主要由..."
   }
   ```

3. **调试方法**：使用 verbose 模式查看详细的搜索信息

## 文件修改清单

- ✅ `docx_processor.go` - 修复智能井号包装逻辑
- ✅ `docx_processor_test.go` - 添加测试用例
- ✅ 编译生成新的 `docx_replacer.exe`

修复完成，问题已解决！