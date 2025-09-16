# DOCX 关键词替换工具

一个用于自动替换 DOCX 文档中关键词的 Go 语言工具。支持普通文本和表格中的关键词替换，能够处理表格中被 XML 分割的关键词情况。

## 功能特性

- 🔍 **智能关键词匹配**: 支持 `#key#` 格式的关键词搜索和替换
- 📄 **全文档支持**: 处理普通段落文本和表格内容
- 🔧 **XML 分割处理**: 自动处理表格中被 XML 标签分割的关键词
- 📁 **批量处理**: 支持单文件和批量目录处理
- ⚙️ **配置驱动**: 通过 JSON 配置文件管理关键词映射
- 🚀 **自动化**: 无需用户交互，完全自动化处理

## 项目结构

```
docx_replacer/
├── cmd/docx-replacer/          # 主程序入口
│   └── main.go
├── internal/                   # 内部模块
│   ├── config/                 # 配置管理
│   ├── domain/                 # 领域模型
│   ├── matcher/                # 关键词匹配引擎
│   └── processor/              # 文档处理器
├── pkg/                        # 公共包
│   ├── docx/                   # DOCX 文件操作
│   └── utils/                  # 工具函数
├── test/                       # 测试文件
│   └── testdata/               # 测试数据
├── go.mod                      # Go 模块文件
└── README.md                   # 项目说明
```

## 安装和使用

### 1. 编译项目

```bash
# 克隆项目
git clone https://github.com/allanpk716/docx_replacer.git
cd docx_replacer

# 下载依赖
go mod tidy

# 编译
go build -o docx-replacer ./cmd/docx-replacer
```

### 2. 准备配置文件

创建 `config.json` 文件：

```json
{
  "project_name": "项目名称",
  "keywords": [
    {
      "key": "COMPANY_NAME",
      "value": "北京科技有限公司",
      "source_file": "company_info.xlsx"
    },
    {
      "key": "PROJECT_TITLE",
      "value": "智能办公系统开发项目",
      "source_file": "project_info.xlsx"
    }
  ]
}
```

### 3. 使用方法

#### 单文件处理

```bash
# 处理单个 DOCX 文件
./docx-replacer -config config.json -input input.docx -output output.docx

# 如果不指定输出文件，会自动生成 input_processed.docx
./docx-replacer -config config.json -input input.docx
```

#### 批量处理

```bash
# 批量处理目录中的所有 DOCX 文件
./docx-replacer -config config.json -input-dir ./input -output-dir ./output

# 如果不指定输出目录，会自动创建 input_processed 目录
./docx-replacer -config config.json -input-dir ./input
```

#### 其他选项

```bash
# 显示版本信息
./docx-replacer -version

# 显示帮助信息
./docx-replacer -help

# 详细输出模式
./docx-replacer -config config.json -input input.docx -verbose
```

## 关键词格式说明

在 DOCX 文档中，关键词需要使用 `#key#` 格式，例如：

- 文档中写入：`公司名称：#COMPANY_NAME#`
- 配置文件中定义：`{"key": "COMPANY_NAME", "value": "北京科技有限公司"}`
- 替换后结果：`公司名称：北京科技有限公司`

## 技术特性

### 智能表格处理

工具能够处理表格中被 XML 标签分割的关键词。例如，当关键词 `#COMPANY_NAME#` 在表格中被分割为：

```xml
<w:t>#COMPANY</w:t><w:t>_NAME#</w:t>
```

工具会自动识别并重组这些分割的关键词进行正确替换。

### 模块化设计

- **配置管理模块**: 负责 JSON 配置文件的解析和验证
- **关键词匹配引擎**: 实现高效的关键词搜索和替换算法
- **文档处理器**: 处理 DOCX 文件的读取、修改和保存
- **表格处理器**: 专门处理表格中的复杂关键词替换场景

## 开发和测试

```bash
# 运行测试
go test ./...

# 运行特定模块测试
go test ./internal/config
go test ./internal/matcher
go test ./internal/processor

# 代码检查
go vet ./...
go fmt ./...
```

## 依赖项

- [go-docx](https://github.com/fumiama/go-docx): DOCX 文件操作库
- Go 1.21+

## 许可证

MIT License

## 贡献

欢迎提交 Issue 和 Pull Request！

## 更新日志

### v1.0.0
- 初始版本发布
- 支持基本的关键词替换功能
- 支持表格中分割关键词的处理
- 支持批量文件处理