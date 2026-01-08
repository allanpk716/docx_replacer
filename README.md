# DocuFiller - Word文档批量填充工具

## 项目简介

DocuFiller是一个基于C# + .NET 8 + WPF开发的桌面应用程序，用于通过JSON数据文件批量填充Word文档中的内容控件。

## 主要功能

- **批量文档生成**：根据JSON数据批量生成Word文档
- **模板管理**：支持.docx和.dotx格式的Word模板
- **数据预览**：实时预览JSON数据内容和统计信息
- **进度监控**：实时显示处理进度和状态
- **错误处理**：完善的异常处理和日志记录
- **文件管理**：自动创建输出目录和文件命名
- **页眉页脚支持**：支持替换页眉和页脚中的内容控件（详见[功能说明](docs/features/header-footer-support.md)）

## 技术架构

- **框架**：.NET 8 + WPF
- **文档处理**：DocumentFormat.OpenXml
- **JSON处理**：Newtonsoft.Json
- **依赖注入**：Microsoft.Extensions.DependencyInjection
- **日志记录**：Microsoft.Extensions.Logging
- **架构模式**：MVVM + 分层架构

## 项目结构

```
DocuFiller/
├── Models/                 # 数据模型
├── ViewModels/            # 视图模型
├── Services/              # 业务服务
│   ├── Interfaces/        # 服务接口
│   └── *.cs              # 服务实现
├── Utils/                 # 工具类
├── Examples/              # 示例文件
├── Templates/             # 模板文件
├── Logs/                  # 日志文件
└── Output/               # 输出文件
```

## 使用方法

### 1. 准备模板文件
- 在Word中创建文档模板
- 插入内容控件并设置标记
- 保存为.docx或.dotx格式

### 2. 准备数据文件
- 创建JSON格式的数据文件
- 确保字段名与模板中的标记匹配
- 支持数组格式（多条记录）

### 3. 运行应用程序
- 选择Word模板文件
- 选择JSON数据文件
- 设置输出目录
- 点击开始处理

## 示例数据格式

```json
[
  {
    "姓名": "张三",
    "性别": "男",
    "年龄": "28",
    "职位": "软件工程师",
    "部门": "技术部"
  },
  {
    "姓名": "李四",
    "性别": "女",
    "年龄": "25",
    "职位": "产品经理",
    "部门": "产品部"
  }
]
```

## 编译和运行

### 环境要求
- .NET 8 SDK
- Visual Studio 2022 或 VS Code
- Windows 10/11

### 编译步骤
```bash
# 克隆项目
git clone <repository-url>
cd docx_replacer

# 还原依赖
dotnet restore

# 编译项目
dotnet build

# 运行项目
dotnet run
```

## 配置说明

应用程序通过`App.config`文件进行配置：

- **日志配置**：日志级别、保留天数、文件路径
- **文件处理**：最大文件大小、支持的扩展名
- **性能配置**：并发处理数、超时时间
- **UI配置**：自动保存、进度显示等

## 日志和错误处理

- 所有操作都有详细的日志记录
- 日志文件保存在`Logs`目录
- 支持不同级别的日志输出
- 完善的异常处理机制

## 许可证

Copyright © Allan 2024

## 联系方式

如有问题或建议，请联系开发者。