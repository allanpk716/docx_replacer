using DocuFiller.Models.Update;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DocuFiller.Services.Update
{
    /// <summary>
    /// 管理更新客户端的守护进程模式
    /// </summary>
    public class UpdateDownloader : IDisposable
    {
        private readonly ILogger<UpdateDownloader> _logger;
        private readonly JsonSerializerOptions _jsonOptions;
        private Process? _process;
        private HttpClient? _httpClient;
        private int? _daemonPort;
        private bool _disposed;

        public UpdateDownloader(ILogger<UpdateDownloader> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            // 验证 update-client.exe 是否存在
            string exePath = GetUpdateClientPath();
            if (!File.Exists(exePath))
            {
                throw new FileNotFoundException($"更新客户端不存在: {exePath}", exePath);
            }

            _logger.LogInformation("UpdateDownloader 初始化成功，客户端路径: {ExePath}", exePath);
        }

        /// <summary>
        /// 启动下载进程
        /// </summary>
        public async Task<bool> StartDownloadAsync(string version, string outputPath)
        {
            if (string.IsNullOrEmpty(version))
                throw new ArgumentException("版本号不能为空", nameof(version));

            if (string.IsNullOrEmpty(outputPath))
                throw new ArgumentException("输出路径不能为空", nameof(outputPath));

            // 获取可用端口
            _daemonPort = GetAvailablePort();
            if (!_daemonPort.HasValue)
            {
                _logger.LogError("无法找到可用端口 (范围: 19876-19880)");
                return false;
            }

            _logger.LogInformation("找到可用端口: {Port}", _daemonPort.Value);

            try
            {
                string exePath = GetUpdateClientPath();
                string arguments = $"download --daemon --port {_daemonPort.Value} --version {version} --output \"{outputPath}\"";

                _logger.LogInformation("启动更新客户端: {ExePath} {Arguments}", exePath, arguments);

                var startInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = arguments,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };

                _process = Process.Start(startInfo);
                if (_process == null)
                {
                    _logger.LogError("无法启动更新客户端进程");
                    return false;
                }

                _logger.LogInformation("进程已启动，PID: {ProcessId}", _process.Id);

                // 等待 HTTP 服务器启动
                await Task.Delay(1000);

                // 创建 HttpClient
                _httpClient = new HttpClient
                {
                    BaseAddress = new Uri($"http://localhost:{_daemonPort.Value}")
                };
                _httpClient.Timeout = TimeSpan.FromSeconds(5);

                // 验证服务器是否响应
                var testResponse = await _httpClient.GetAsync("/status");
                if (!testResponse.IsSuccessStatusCode)
                {
                    _logger.LogWarning("HTTP 服务器未正常响应，状态码: {StatusCode}", testResponse.StatusCode);
                    return false;
                }

                _logger.LogInformation("HTTP 服务器已就绪，端口: {Port}", _daemonPort.Value);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "启动下载进程失败");
                CleanupProcess();
                return false;
            }
        }

        /// <summary>
        /// 获取当前下载状态
        /// </summary>
        public async Task<DownloadStatus?> GetStatusAsync()
        {
            if (_httpClient == null)
            {
                _logger.LogWarning("HttpClient 未初始化，无法获取状态");
                return null;
            }

            try
            {
                _logger.LogDebug("正在获取下载状态...");
                var response = await _httpClient.GetAsync("/status");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var daemonStatus = JsonSerializer.Deserialize<DaemonStatusInfo>(json, _jsonOptions);

                if (daemonStatus == null)
                {
                    _logger.LogWarning("无法解析状态响应");
                    return null;
                }

                var status = new DownloadStatus
                {
                    State = daemonStatus.State,
                    Current = daemonStatus.Progress?.Current ?? 0,
                    Total = daemonStatus.Progress?.Total ?? 0,
                    Error = daemonStatus.Error
                };

                _logger.LogDebug("当前状态: {State}, 进度: {Current}/{Total}", status.State, status.Current, status.Total);
                return status;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取下载状态失败");
                return null;
            }
        }

        /// <summary>
        /// 监控下载进度
        /// </summary>
        public async Task<DownloadStatus?> MonitorDownloadAsync(
            Action<DownloadProgress>? onProgress,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("开始监控下载进度");

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var status = await GetStatusAsync();
                    if (status == null)
                    {
                        _logger.LogWarning("无法获取状态，等待重试...");
                        await Task.Delay(1000, cancellationToken);
                        continue;
                    }

                    // 如果正在下载，触发进度回调
                    if (status.State == "downloading" && onProgress != null)
                    {
                        var progress = ConvertProgress(status);
                        onProgress(progress);
                    }

                    // 检查是否完成或出错
                    if (status.State == "completed")
                    {
                        _logger.LogInformation("下载完成");
                        return status;
                    }

                    if (status.State == "error")
                    {
                        _logger.LogError("下载出错: {Error}", status.Error);
                        return status;
                    }

                    // 等待 1 秒后再次检查
                    await Task.Delay(1000, cancellationToken);
                }

                _logger.LogInformation("监控被取消");
                return null;
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("监控被取消");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "监控下载进度时发生错误");
                return null;
            }
        }

        /// <summary>
        /// 关闭守护进程
        /// </summary>
        public async Task ShutdownAsync()
        {
            if (_httpClient == null)
            {
                _logger.LogWarning("HttpClient 未初始化，无法发送关闭命令");
                CleanupProcess();
                return;
            }

            try
            {
                _logger.LogInformation("正在关闭守护进程...");

                // 发送关闭命令
                var response = await _httpClient.PostAsync("/shutdown", null);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogDebug("已发送关闭命令");
                }
                else
                {
                    _logger.LogWarning("关闭命令未成功，状态码: {StatusCode}", response.StatusCode);
                }

                // 等待进程退出（最多 5 秒）
                if (_process != null && !_process.HasExited)
                {
                    bool exited = _process.WaitForExit(5000);
                    if (exited)
                    {
                        _logger.LogInformation("进程已正常退出");
                    }
                    else
                    {
                        _logger.LogWarning("进程未在 5 秒内退出，强制终止");
                        try
                        {
                            _process.Kill(entireProcessTree: true);
                            _logger.LogInformation("进程已被强制终止");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "强制终止进程失败");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "关闭守护进程时发生错误");
            }
            finally
            {
                CleanupProcess();
            }
        }

        /// <summary>
        /// 获取可用端口 (19876-19880)
        /// </summary>
        private int? GetAvailablePort()
        {
            for (int port = 19876; port <= 19880; port++)
            {
                if (IsPortAvailable(port))
                {
                    return port;
                }
            }
            return null;
        }

        /// <summary>
        /// 检查端口是否可用
        /// </summary>
        private bool IsPortAvailable(int port)
        {
            try
            {
                var listener = new TcpListener(IPAddress.Loopback, port);
                listener.Start();
                listener.Stop();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 转换进度信息
        /// </summary>
        private DownloadProgress ConvertProgress(DownloadStatus status)
        {
            return new DownloadProgress
            {
                BytesReceived = status.Current,
                TotalBytes = status.Total,
                ProgressPercentage = status.Total > 0 ? (int)((double)status.Current / status.Total * 100) : 0,
                Status = status.State
            };
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

        /// <summary>
        /// 清理进程资源
        /// </summary>
        private void CleanupProcess()
        {
            _httpClient?.Dispose();
            _httpClient = null;

            if (_process != null)
            {
                try
                {
                    if (!_process.HasExited)
                    {
                        _process.Kill(entireProcessTree: true);
                    }
                    _process.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "清理进程资源时发生错误");
                }
                _process = null;
            }

            _daemonPort = null;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _logger.LogDebug("释放 UpdateDownloader 资源");
            CleanupProcess();
            _disposed = true;
        }
    }

    #region Data Models

    /// <summary>
    /// 下载状态
    /// </summary>
    public class DownloadStatus
    {
        public string State { get; set; } = string.Empty;
        public long Current { get; set; }
        public long Total { get; set; }
        public string? Error { get; set; }
    }

    /// <summary>
    /// 守护进程状态信息（从 HTTP API 返回）
    /// </summary>
    internal class DaemonStatusInfo
    {
        public string State { get; set; } = string.Empty;
        public DaemonProgressInfo? Progress { get; set; }
        public string? Error { get; set; }
    }

    /// <summary>
    /// 守护进程进度信息
    /// </summary>
    internal class DaemonProgressInfo
    {
        public long Current { get; set; }
        public long Total { get; set; }
    }

    #endregion
}
