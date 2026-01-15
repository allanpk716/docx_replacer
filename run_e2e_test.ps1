# 端到端测试脚本 - 验证表格单元格内容控件修复效果
# 使用方法: powershell -ExecutionPolicy Bypass -File run_e2e_test.ps1

Write-Host "=== DocuFiller 端到端测试 ===" -ForegroundColor Cyan
Write-Host ""

# 获取项目根目录
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptPath

# 测试文件路径
$templateFile = "test_data\t1\IVDR-BH-FD68-CE01 Device Description and Specification including Variants and Accessories.docx"
$dataFile = "test_data\t1\FD68 IVDR.xlsx"
$outputDir = "test_data\t1\output"

Write-Host "测试配置:" -ForegroundColor Yellow
Write-Host "  模板文件: $templateFile"
Write-Host "  数据文件: $dataFile"
Write-Host "  输出目录: $outputDir"
Write-Host ""

# 检查文件是否存在
if (-not (Test-Path $templateFile)) {
    Write-Host "错误: 模板文件不存在: $templateFile" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path $dataFile)) {
    Write-Host "错误: 数据文件不存在: $dataFile" -ForegroundColor Red
    exit 1
}

# 创建输出目录
if (-not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
    Write-Host "已创建输出目录: $outputDir" -ForegroundColor Green
}

Write-Host "开始构建项目..." -ForegroundColor Yellow
Write-Host ""

# 构建项目
$buildResult = dotnet build -c Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "构建失败!" -ForegroundColor Red
    exit 1
}

Write-Host "构建成功!" -ForegroundColor Green
Write-Host ""

Write-Host "=== 重要提示 ===" -ForegroundColor Yellow
Write-Host "由于这是一个 WPF 应用程序,无法通过命令行直接运行。" -ForegroundColor White
Write-Host "请按照以下步骤进行手动测试:" -ForegroundColor White
Write-Host ""
Write-Host "1. 运行应用程序:" -ForegroundColor Cyan
Write-Host "   dotnet run" -ForegroundColor Gray
Write-Host ""
Write-Host "2. 在应用程序中:" -ForegroundColor Cyan
Write-Host "   - 选择模板文件: $templateFile" -ForegroundColor Gray
Write-Host "   - 选择数据文件: $dataFile" -ForegroundColor Gray
Write-Host "   - 选择输出目录: $outputDir" -ForegroundColor Gray
Write-Host "   - 点击开始处理" -ForegroundColor Gray
Write-Host ""
Write-Host "3. 验证输出文档:" -ForegroundColor Cyan
Write-Host "   - 打开输出目录: $outputDir" -ForegroundColor Gray
Write-Host "   - 打开生成的文档" -ForegroundColor Gray
Write-Host "   - 导航到章节 1.4.3.2 Instrument" -ForegroundColor Gray
Write-Host "   - 检查表格格式是否正常" -ForegroundColor Gray
Write-Host "   - 检查 'Brief Product Description' 列中的内容是否正确替换" -ForegroundColor Gray
Write-Host "   - 检查表格边框、列宽等格式是否保留" -ForegroundColor Gray
Write-Host "   - 检查是否有内容'跑到下一行'" -ForegroundColor Gray
Write-Host ""

Write-Host "测试准备完成!" -ForegroundColor Green
Write-Host ""

# 可选: 询问是否要启动应用程序
$response = Read-Host "是否要启动应用程序? (Y/N)"
if ($response -eq "Y" -or $response -eq "y") {
    Write-Host ""
    Write-Host "启动应用程序..." -ForegroundColor Yellow
    dotnet run
}
