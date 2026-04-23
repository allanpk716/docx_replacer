---
estimated_steps: 1
estimated_files: 2
skills_used: []
---

# T03: 删除 Tools 目录并验证构建通过

删除 Tools 目录（包含 10 个诊断工具子目录：CompareDocumentStructure、ControlRelationshipAnalyzer、DeepDiagnostic、DiagnoseTableStructure、E2ETest、ExcelFormattedTestGenerator、ExcelToWordVerifier、StepByStepSimulator、TableCellTest、TableStructureAnalyzer）。从 DocuFiller.csproj 移除 Tools 的 Compile Remove/EmbeddedResource Remove/None Remove 行（目录已不存在）。同时移除 ExcelToWordVerifier 的排除行（也已在 Tools 内或根目录）。运行 dotnet build 确认 0 errors。运行 grep 全面验证无残留引用。

## Inputs

- `DocuFiller.csproj`
- `Tools/`

## Expected Output

- `DocuFiller.csproj`

## Verification

cd DocuFiller && dotnet build && test ! -d Tools
