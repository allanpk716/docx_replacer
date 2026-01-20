using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using DocuFiller.Models.Update;

namespace DocuFiller.Services.Update
{
    /// <summary>
    /// 使用 update-client.exe 的更新服务实现
    /// </summary>
    public class UpdateClientService : IUpdateService
    {
        private readonly ILogger<UpdateClientService> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IConfiguration _configuration;
        private readonly string _updateClientPath;
        private readonly JsonSerializerOptions _jsonOptions;

        /// <summary>
        /// 更新可用事件
        /// </summary>
        public event EventHandler<UpdateAvailableEventArgs>? UpdateAvailable;

        /// <summary>
        /// 初始化 UpdateClientService 的新实例
        /// </summary>
        /// <param name="logger">日志记录器</param>
        /// <param name="loggerFactory">日志工厂</param>
        /// <param name="configuration">配置</param>
        public UpdateClientService(ILogger<UpdateClientService> logger, ILoggerFactory loggerFactory, IConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            // 获取 update-client.exe 路径
            _updateClientPath = GetUpdateClientPath();
            _logger.LogInformation("UpdateClientService 初始化完成: ClientPath={ClientPath}", _updateClientPath);

            // 配置 JSON 选项
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        /// <summary>
        /// 检查是否有可用更新
        /// </summary>
        /// <param name="currentVersion">当前版本号</param>
        /// <param name="channel">更新通道</param>
        /// <returns>如果有新版本则返回版本信息，否则返回 null</returns>
        public async Task<VersionInfo?> CheckForUpdateAsync(string currentVersion, string channel)
        {
            try
            {
                _logger.LogInformation("开始检查更新: CurrentVersion={CurrentVersion}, Channel={Channel}",
                    currentVersion, channel);

                // 调用 update-client.exe check --json
                var result = await RunCommandAsync(
                    "check",
                    "--current-version", currentVersion,
                    "--channel", channel,
                    "--json"
                );

                if (result.ExitCode != 0)
                {
                    _logger.LogWarning("检查更新失败: ExitCode={ExitCode}, Error={Error}",
                        result.ExitCode, result.ErrorOutput);
                    return null;
                }

                // 解析 JSON 响应
                var checkResponse = JsonSerializer.Deserialize<UpdateClientCheckResponse>(result.Output, _jsonOptions);
                if (checkResponse == null)
                {
                    _logger.LogWarning("无法解析检查更新响应");
                    return null;
                }

                _logger.LogDebug("检查更新响应: HasUpdate={HasUpdate}, LatestVersion={Version}",
                    checkResponse.HasUpdate, checkResponse.LatestVersion);

                // 如果没有更新，返回 null
                if (!checkResponse.HasUpdate)
                {
                    _logger.LogInformation("当前版本已是最新");
                    return null;
                }

                // 转换为 VersionInfo
                var versionInfo = ConvertToVersionInfo(checkResponse, channel);
                if (versionInfo == null)
                {
                    _logger.LogWarning("无法转换版本信息");
                    return null;
                }

                _logger.LogInformation("发现新版本: {Version}, 发布日期: {PublishDate}",
                    versionInfo.Version, versionInfo.PublishDate);

                // 触发更新可用事件
                UpdateAvailable?.Invoke(this, new UpdateAvailableEventArgs
                {
                    Version = versionInfo,
                    IsDownloaded = false
                });

                return versionInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查更新时发生错误");
                throw new UpdateException("检查更新时发生错误", ex);
            }
        }

        /// <summary>
        /// 下载更新包
        /// </summary>
        /// <param name="version">版本信息</param>
        /// <param name="progress">下载进度报告</param>
        /// <returns>下载的文件路径</returns>
        public async Task<string> DownloadUpdateAsync(VersionInfo version, IProgress<DownloadProgress> progress)
        {
            if (version == null)
                throw new ArgumentNullException(nameof(version));

            UpdateDownloader? downloader = null;
            try
            {
                _logger.LogInformation("开始下载更新: Version={Version}", version.Version);

                // 创建 UpdateDownloader
                var downloaderLogger = _loggerFactory.CreateLogger<UpdateDownloader>();
                downloader = new UpdateDownloader(downloaderLogger);
                var tempPath = Path.GetTempPath();
                var outputPath = Path.Combine(tempPath, version.FileName);

                // 报告开始下载
                progress?.Report(new DownloadProgress
                {
                    BytesReceived = 0,
                    TotalBytes = version.FileSize,
                    ProgressPercentage = 0,
                    Status = "准备下载..."
                });

                // 启动下载
                bool started = await downloader.StartDownloadAsync(version.Version, outputPath);
                if (!started)
                {
                    throw new UpdateException("启动下载失败");
                }

                // 监控下载进度
                var cts = new CancellationTokenSource();
                var status = await downloader.MonitorDownloadAsync(
                    p => progress?.Report(p),
                    cts.Token
                );

                if (status == null)
                {
                    throw new UpdateException("下载监控失败");
                }

                if (status.State == "error")
                {
                    throw new UpdateException($"下载失败: {status.Error}");
                }

                if (status.State != "completed")
                {
                    throw new UpdateException($"下载未完成: {status.State}");
                }

                // 验证文件存在
                if (!File.Exists(outputPath))
                {
                    throw new UpdateException("下载的文件不存在");
                }

                _logger.LogInformation("下载完成: {OutputPath}", outputPath);

                // 报告下载完成
                progress?.Report(new DownloadProgress
                {
                    BytesReceived = version.FileSize,
                    TotalBytes = version.FileSize,
                    ProgressPercentage = 100,
                    Status = "下载完成"
                });

                return outputPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "下载更新时发生错误");
                throw new UpdateException("下载更新时发生错误", ex);
            }
            finally
            {
                // 确保关闭下载器
                if (downloader != null)
                {
                    await downloader.ShutdownAsync();
                    downloader.Dispose();
                }
            }
        }

        /// <summary>
        /// 安装更新
        /// </summary>
        /// <param name="packagePath">更新包路径</param>
        /// <returns>是否成功启动安装程序</returns>
        public async Task<bool> InstallUpdateAsync(string packagePath)
        {
            if (string.IsNullOrEmpty(packagePath))
                throw new ArgumentException("更新包路径不能为空", nameof(packagePath));

            if (!File.Exists(packagePath))
                throw new FileNotFoundException("更新包文件不存在", packagePath);

            try
            {
                _logger.LogInformation("开始安装更新: PackagePath={PackagePath}", packagePath);

                // 启动安装程序
                var startInfo = new ProcessStartInfo
                {
                    FileName = packagePath,
                    UseShellExecute = true,
                    Verb = "runas", // 以管理员权限运行
                    Arguments = "/silent /norestart" // 静默安装，安装后不重启
                };

                _logger.LogDebug("启动安装程序: {FileName} {Arguments}",
                    startInfo.FileName, startInfo.Arguments);

                var process = Process.Start(startInfo);
                if (process == null)
                {
                    _logger.LogError("启动安装程序失败");
                    return false;
                }

                _logger.LogInformation("安装程序已启动，进程 ID: {ProcessId}", process.Id);

                // 关闭当前应用程序
                _logger.LogInformation("准备关闭当前应用程序...");
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    System.Windows.Application.Current.Shutdown();
                });

                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "安装更新时发生错误");
                throw new UpdateException("安装更新时发生错误", ex);
            }
        }

        /// <summary>
        /// 运行 update-client.exe 命令
        /// </summary>
        /// <param name="args">命令参数</param>
        /// <returns>命令执行结果</returns>
        private async Task<CommandResult> RunCommandAsync(params string[] args)
        {
            try
            {
                _logger.LogDebug("执行命令: {ExePath} {Arguments}",
                    _updateClientPath, string.Join(" ", args));

                var startInfo = new ProcessStartInfo
                {
                    FileName = _updateClientPath,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };

                foreach (var arg in args)
                {
                    startInfo.ArgumentList.Add(arg);
                }

                using var process = Process.Start(startInfo);
                if (process == null)
                {
                    throw new UpdateException("无法启动 update-client.exe 进程");
                }

                // 读取输出
                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();

                // 等待进程退出
                await process.WaitForExitAsync();

                var result = new CommandResult
                {
                    ExitCode = process.ExitCode,
                    Output = output,
                    ErrorOutput = error
                };

                _logger.LogDebug("命令执行完成: ExitCode={ExitCode}", result.ExitCode);
                if (!string.IsNullOrEmpty(result.ErrorOutput))
                {
                    _logger.LogWarning("命令错误输出: {Error}", result.ErrorOutput);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "执行命令时发生错误");
                throw new UpdateException("执行命令时发生错误", ex);
            }
        }

        /// <summary>
        /// 转换 UpdateClientCheckResponse 为 VersionInfo
        /// </summary>
        private VersionInfo? ConvertToVersionInfo(UpdateClientCheckResponse response, string channel)
        {
            try
            {
                return new VersionInfo
                {
                    Version = response.LatestVersion,
                    Channel = channel,
                    FileName = $"DocuFiller-{response.LatestVersion}.exe",
                    FileSize = response.FileSize,
                    FileHash = string.Empty,
                    ReleaseNotes = response.ReleaseNotes,
                    PublishDate = response.PublishDate,
                    Mandatory = response.Mandatory,
                    DownloadUrl = response.DownloadUrl
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "转换版本信息时发生错误");
                return null;
            }
        }

        /// <summary>
        /// 获取 update-client.exe 路径
        /// </summary>
        private string GetUpdateClientPath()
        {
            // 假设 update-client.exe 在应用程序根目录
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(appDir, "update-client.exe");
        }
    }

    /// <summary>
    /// 命令执行结果
    /// </summary>
    public class CommandResult
    {
        public int ExitCode { get; set; }
        public string Output { get; set; } = string.Empty;
        public string ErrorOutput { get; set; } = string.Empty;
    }
}
