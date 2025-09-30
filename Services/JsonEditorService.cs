using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using DocuFiller.Models;
using DocuFiller.Services.Interfaces;
using DocuFiller.Utils;

namespace DocuFiller.Services
{
    /// <summary>
    /// JSON编辑器服务实现
    /// </summary>
    public class JsonEditorService : IJsonEditorService
    {
        private readonly ILogger<JsonEditorService> _logger;
        private readonly IFileService _fileService;
        private readonly IKeywordValidationService _validationService;

        public JsonEditorService(
            ILogger<JsonEditorService> logger,
            IFileService fileService,
            IKeywordValidationService validationService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
        }

        public async Task<JsonProjectModel> LoadProjectAsync(string filePath)
        {
            try
            {
                _logger.LogInformation($"[调试] 开始加载JSON项目文件: {filePath}");

                if (!_fileService.FileExists(filePath))
                {
                    _logger.LogError($"[调试] 文件不存在: {filePath}");
                    throw new FileNotFoundException($"文件不存在: {filePath}");
                }

                var jsonContent = await _fileService.ReadFileContentAsync(filePath);
                _logger.LogInformation($"[调试] 文件内容读取完成，长度: {jsonContent.Length}");

                var project = ParseJsonString(jsonContent);
                project.FilePath = filePath;
                project.HasUnsavedChanges = false;

                _logger.LogInformation($"[调试] JSON项目加载成功，项目名: {project.ProjectName}, 关键词数量: {project.KeywordCount}");
                return project;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[调试] 加载JSON项目文件失败: {filePath}");
                throw;
            }
        }

        public async Task<bool> SaveProjectAsync(JsonProjectModel project, string filePath)
        {
            try
            {
                _logger.LogInformation($"[调试] 开始保存JSON项目到文件: {filePath}");

                if (project == null)
                {
                    _logger.LogError("[调试] 项目模型为空");
                    return false;
                }

                // 验证项目数据
                var validationResult = ValidateProject(project);
                if (!validationResult.IsValid)
                {
                    _logger.LogError($"[调试] 项目数据验证失败: {string.Join(", ", validationResult.Errors)}");
                    return false;
                }

                // 创建备份
                if (_fileService.FileExists(filePath))
                {
                    await CreateBackupAsync(filePath);
                }

                // 格式化JSON并保存
                var jsonContent = FormatJsonString(project);
                await _fileService.WriteFileContentAsync(filePath, jsonContent);

                // 更新项目状态
                project.FilePath = filePath;
                project.MarkAsSaved();

                _logger.LogInformation($"[调试] JSON项目保存成功: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[调试] 保存JSON项目文件失败: {filePath}");
                return false;
            }
        }

        public ValidationResult ValidateProject(JsonProjectModel project)
        {
            _logger.LogInformation("[调试] 开始验证JSON项目数据");

            if (project == null)
            {
                return new ValidationResult
                {
                    IsValid = false,
                    Errors = { "项目模型不能为空" }
                };
            }

            var result = project.Validate();
            _logger.LogInformation($"[调试] 项目数据验证完成，结果: {(result.IsValid ? "通过" : "失败")}, 错误数: {result.Errors.Count}");

            return result;
        }

        public string FormatJsonString(JsonProjectModel project)
        {
            try
            {
                _logger.LogInformation("[调试] 开始格式化JSON字符串");

                var jsonObject = new JObject
                {
                    ["project_name"] = project.ProjectName,
                    ["keywords"] = new JArray(
                        project.Keywords.Select(k => new JObject
                        {
                            ["key"] = k.Key,
                            ["value"] = k.Value,
                            ["source_file"] = k.SourceFile
                        }).ToArray()
                    )
                };

                var jsonString = jsonObject.ToString(Formatting.Indented);
                _logger.LogInformation($"[调试] JSON字符串格式化完成，长度: {jsonString.Length}");

                return jsonString;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[调试] 格式化JSON字符串失败");
                throw;
            }
        }

        public JsonProjectModel ParseJsonString(string jsonContent)
        {
            try
            {
                _logger.LogInformation("[调试] 开始解析JSON字符串");

                if (string.IsNullOrWhiteSpace(jsonContent))
                {
                    throw new ArgumentException("JSON内容不能为空");
                }

                var jsonObject = JObject.Parse(jsonContent);
                var project = new JsonProjectModel
                {
                    ProjectName = jsonObject["project_name"]?.ToString() ?? "未命名项目"
                };

                var keywordsArray = jsonObject["keywords"] as JArray;
                if (keywordsArray != null)
                {
                    foreach (var keywordToken in keywordsArray)
                    {
                        var keywordObj = keywordToken as JObject;
                        if (keywordObj != null)
                        {
                            var keyword = new JsonKeywordItem
                            {
                                Key = keywordObj["key"]?.ToString() ?? string.Empty,
                                Value = keywordObj["value"]?.ToString() ?? string.Empty,
                                SourceFile = keywordObj["source_file"]?.ToString() ?? string.Empty
                            };
                            project.Keywords.Add(keyword);
                        }
                    }
                }

                _logger.LogInformation($"[调试] JSON字符串解析完成，项目名: {project.ProjectName}, 关键词数量: {project.KeywordCount}");
                return project;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[调试] 解析JSON字符串失败");
                throw;
            }
        }

        public JsonProjectModel CreateNewProject(string projectName = "新项目")
        {
            _logger.LogInformation($"[调试] 创建新项目: {projectName}");

            var project = new JsonProjectModel
            {
                ProjectName = projectName,
                Keywords = new System.Collections.ObjectModel.ObservableCollection<JsonKeywordItem>()
            };

            // 添加示例关键词
            project.AddKeyword(new JsonKeywordItem
            {
                Key = "#示例关键词#",
                Value = "示例值",
                SourceFile = "示例文件.docx"
            });

            return project;
        }

        public async Task<bool> CreateBackupAsync(string filePath)
        {
            try
            {
                if (!_fileService.FileExists(filePath))
                    return false;

                var backupPath = filePath + ".bak";
                await _fileService.CopyFileAsync(filePath, backupPath);
                
                _logger.LogInformation($"[调试] 创建备份文件成功: {backupPath}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[调试] 创建备份文件失败: {filePath}");
                return false;
            }
        }

        public ValidationResult ValidateFilePath(string filePath)
        {
            var result = new ValidationResult { IsValid = true };

            if (string.IsNullOrWhiteSpace(filePath))
            {
                result.AddError("文件路径不能为空");
                return result;
            }

            if (!filePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                result.AddError("文件必须是JSON格式(.json)");
            }

            try
            {
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    result.AddWarning($"目录不存在，将自动创建: {directory}");
                }
            }
            catch (Exception ex)
            {
                result.AddError($"文件路径格式无效: {ex.Message}");
            }

            result.IsValid = result.Errors.Count == 0;
            return result;
        }

        public async Task<DocuFiller.Models.FileInfo?> GetFileInfoAsync(string filePath)
        {
            return await Task.Run(() =>
            {
                if (!_fileService.FileExists(filePath))
                    return null;

                var fileInfo = new System.IO.FileInfo(filePath);
                return new DocuFiller.Models.FileInfo
                {
                    Name = fileInfo.Name,
                    FullPath = fileInfo.FullName,
                    Size = fileInfo.Length,
                    CreationTime = fileInfo.CreationTime,
                    LastModified = fileInfo.LastWriteTime
                };
            });
        }
    }
}