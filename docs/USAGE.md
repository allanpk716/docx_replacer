# DOCX 关键词替换工具使用说明

## 概述

DOCX 关键词替换工具是一个用于在 Word 文档中自动替换关键词的命令行工具。该工具支持普通文本和表格中的关键词替换，能够处理被 XML 标签分割的关键词情况。

## 功能特性

- 支持 JSON 配置文件定义关键词映射
- 自动在文档中搜索 `#关键词#` 格式的占位符
- 支持普通文本和表格内容的关键词替换
- 处理表格中被 XML 分割的关键词情况
- 完全自动化处理，无需用户交互
- 支持批量关键词替换

## 安装与编译

### 编译源码

```bash
go build ./cmd/docx-replacer
```

编译完成后会生成 `docx-replacer.exe` 可执行文件。

## 使用方法

### 基本语法

```bash
./docx-replacer.exe -config <配置文件> -input <输入文档> -output <输出文档> [选项]
```

### 命令行参数

- `-config`: 配置文件路径（必需）
- `-input`: 输入 DOCX 文件路径（必需）
- `-output`: 输出 DOCX 文件路径（必需）
- `-timeout`: 处理超时时间，默认 30 秒
- `-version`: 显示版本信息
- `-help`: 显示帮助信息

### 配置文件格式

配置文件使用 JSON 格式，包含项目名称和关键词映射：

```json
{
  "project_name": "示例项目",
  "keywords": [
    {
      "key": "COMPANY_NAME",
      "value": "示例公司",
      "source_file": "data.xlsx"
    },
    {
      "key": "PRODUCT_NAME",
      "value": "示例产品",
      "source_file": "data.xlsx"
    },
    {
      "key": "VERSION",
      "value": "1.0.0",
      "source_file": "data.xlsx"
    }
  ]
}
```

### 文档中的关键词格式

在 Word 文档中，关键词需要使用 `#关键词#` 格式，例如：

- `#COMPANY_NAME#` 会被替换为配置文件中对应的值
- `#PRODUCT_NAME#` 会被替换为配置文件中对应的值
- `#VERSION#` 会被替换为配置文件中对应的值

## 使用示例

### 1. 创建配置文件

创建 `config.json` 文件：

```json
{
  "project_name": "医疗设备注册申报",
  "keywords": [
    {
      "key": "DEVICE_NAME",
      "value": "便携式心电监护仪",
      "source_file": "device_info.xlsx"
    },
    {
      "key": "MANUFACTURER",
      "value": "北京医疗科技有限公司",
      "source_file": "company_info.xlsx"
    },
    {
      "key": "MODEL_NUMBER",
      "value": "ECG-2024",
      "source_file": "device_info.xlsx"
    }
  ]
}
```

### 2. 准备输入文档

在 Word 文档中使用占位符：

```
产品名称：#DEVICE_NAME#
制造商：#MANUFACTURER#
型号：#MODEL_NUMBER#
```

### 3. 执行替换

```bash
./docx-replacer.exe -config config.json -input template.docx -output result.docx
```

### 4. 查看结果

输出文档中的内容将变为：

```
产品名称：便携式心电监护仪
制造商：北京医疗科技有限公司
型号：ECG-2024
```

## 高级功能

### 表格中的关键词替换

工具支持表格中的关键词替换，包括处理被 XML 标签分割的情况。例如：

| 项目 | 值 |
|------|----|
| 设备名称 | `#DEVICE_NAME#` |
| 制造商 | `#MANUFACTURER#` |

即使关键词在表格的 XML 结构中被分割，工具也能正确识别和替换。

### 超时设置

对于大型文档，可以设置更长的超时时间：

```bash
./docx-replacer.exe -config config.json -input large_doc.docx -output result.docx -timeout 60
```

## 错误处理

工具会在以下情况下报错：

1. 配置文件不存在或格式错误
2. 输入文档不存在或无法读取
3. 输出路径无法写入
4. 处理超时
5. 关键词配置重复或为空

## 注意事项

1. 确保输入的 DOCX 文件格式正确
2. 配置文件中的关键词不能重复
3. 关键词和值都不能为空
4. 输出文件路径的目录必须存在
5. 工具会自动处理，无需用户交互选择

## 技术支持

如遇到问题，请检查：

1. 配置文件格式是否正确
2. 输入文档是否为有效的 DOCX 文件
3. 文件路径是否正确
4. 是否有足够的磁盘空间

使用 `-help` 参数可以查看完整的帮助信息。