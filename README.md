# DOCX文档关键词替换工具

这是一个用于批量替换DOCX文档中关键词的命令行工具，支持单文件处理和批量文件夹处理两种模式。

## 功能特性

- **单文件模式**: 处理单个DOCX文件
- **批量处理模式**: 处理文件夹中的所有DOCX文件
- **关键词替换**: 支持自定义关键词和替换内容
- **保持目录结构**: 批量处理时保持原有的文件夹结构
- **详细日志**: 支持详细模式显示处理过程

## 使用方法

### 命令行参数

```bash
docx_replacer [选项]

选项:
  -config string
        配置文件路径 (默认 "config.json")
  -verbose
        显示详细信息
  -version
        显示版本信息
```

### 单文件模式

创建配置文件 `config.json`：

```json
{
  "input_docx": "input.docx",
  "output_docx": "output.docx",
  "keywords": [
    {
      "key": "#产品名称#",
      "value": "D-二聚体测定试剂盒（胶乳免疫比浊法）",
      "source_file": "产品信息文件"
    }
  ]
}
```

运行命令：
```bash
docx_replacer -config=config.json -verbose
```

### 批量处理模式

创建批量处理配置文件 `batch_config.json`：

```json
{
  "input_folder": "data",
  "output_folder": "output",
  "keywords": [
    {
      "key": "#产品名称#",
      "value": "D-二聚体测定试剂盒（胶乳免疫比浊法）",
      "source_file": "产品信息文件"
    },
    {
      "key": "#公司名称#",
      "value": "某某医疗科技有限公司",
      "source_file": "公司信息文件"
    }
  ]
}
```

运行命令：
```bash
docx_replacer -config=batch_config.json -verbose
```

## 配置文件说明

### 单文件模式配置

- `input_docx`: 输入的DOCX文件路径
- `output_docx`: 输出的DOCX文件路径
- `keywords`: 关键词替换列表

### 批量处理模式配置

- `input_folder`: 输入文件夹路径（包含要处理的DOCX文件）
- `output_folder`: 输出文件夹路径（处理后的文件保存位置）
- `keywords`: 关键词替换列表

### 关键词配置

每个关键词对象包含以下字段：

- `key`: 要替换的关键词（建议使用 `#关键词#` 格式避免误替换）
- `value`: 替换后的内容（支持多行文本）
- `source_file`: 来源文件描述（用于标识数据来源）

## 批量处理特性

1. **自动扫描**: 自动扫描输入文件夹中的所有DOCX文件（包括子文件夹）
2. **保持结构**: 输出文件保持与输入文件相同的目录结构
3. **文件命名**: 输出文件自动添加"_替换后"后缀
4. **跳过临时文件**: 自动跳过以"~$"开头的临时文件
5. **错误处理**: 单个文件处理失败不影响其他文件的处理

## 示例

### 处理单个文件
```bash
# 使用默认配置文件
docx_replacer -verbose

# 使用指定配置文件
docx_replacer -config=my_config.json -verbose
```

### 批量处理文件夹
```bash
# 批量处理data文件夹中的所有DOCX文件
docx_replacer -config=batch_config.json -verbose
```

## 输出示例

### 单文件模式输出
```
配置加载成功:
  模式: 单文件处理
  输入文件: input.docx
  输出文件: output.docx
  关键词数量: 5
  总替换规则数量: 5

关键词替换完成
文档已保存到: output.docx
文档处理完成!
```

### 批量处理模式输出
```
配置加载成功:
  模式: 批量处理
  输入文件夹: data
  输出文件夹: output
  关键词数量: 5
  总替换规则数量: 5

找到 9 个docx文件:
  data\file1.docx
  data\file2.docx
  ...

[1/9] 处理文件: data\file1.docx
  -> 已保存到: output\file1_替换后.docx
[2/9] 处理文件: data\file2.docx
  -> 已保存到: output\file2_替换后.docx
...

批量处理完成! 成功处理 9/9 个文件
```

## 注意事项

1. 确保输入的DOCX文件没有被其他程序打开
2. 输出目录会自动创建，无需手动创建
3. 关键词替换是全局的，建议使用特殊标记（如#关键词#）避免误替换
4. 批量处理时，如果某个文件处理失败，会继续处理其他文件
5. 程序会同时处理文档正文、页眉和页脚中的关键词

## 编译和运行

```bash
# 编译
go build -v

# 运行
./docx_replacer -config=config.json -verbose
```

## 依赖

- Go 1.16+
- github.com/nguyenthenguyen/docx