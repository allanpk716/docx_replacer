---
estimated_steps: 2
estimated_files: 3
skills_used: []
---

# T01: FileService 添加 ILogger 并修复裸 catch 异常吞没

FileService 当前有 4 个方法使用裸 catch 块静默吞掉异常（EnsureDirectoryExists、CopyFileAsync、DeleteFile、WriteFileContentAsync），且完全没有 ILogger 依赖。这导致文件操作失败时无法诊断问题。

需要：添加 ILogger<FileService> 构造函数参数，在所有 catch 块中记录异常详情（文件路径 + 异常类型和消息），同时保持返回 false 的现有行为（不改变接口语义）。更新 IFileService 接口不变，只改实现。更新 App.xaml.cs 的 DI 注册传入 ILogger。

## Inputs

- `Services/FileService.cs — 当前无 ILogger 的 FileService 实现`
- `Services/Interfaces/IFileService.cs — 接口定义（不变）`
- `App.xaml.cs — DI 注册`

## Expected Output

- `Services/FileService.cs — 添加 ILogger 依赖和 4 处异常日志记录`
- `App.xaml.cs — DI 注册确保 ILogger 自动注入`

## Verification

dotnet build && grep -c 'LogError' Services/FileService.cs (应返回 4+)
