using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using DocuFiller.Models.Update;

namespace DocuFiller.Services.Update
{
    /// <summary>
    /// 更新服务实现类
    /// 负责检查、下载和安装应用程序更新
    /// </summary>
    public class UpdateService : IUpdateService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<UpdateService> _logger;
        private readonly string _serverUrl;
        private readonly string _channel;
        private VersionInfo? _currentVersionInfo;

        /// <summary>
        /// 更新可用事件
        /// </summary>
        public event EventHandler<UpdateAvailableEventArgs>? UpdateAvailable;

        /// <summary>
        /// 初始化 UpdateService 的新实例
        /// </summary>
        /// <param name="httpClient">HTTP 客户端</param>
        /// <param name="logger">日志记录器</param>
        public UpdateService(HttpClient httpClient, ILogger<UpdateService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // 从配置读取更新服务器地址和通道
            _serverUrl = GetConfigValue("UpdateServerUrl", "http://192.168.1.100:8080");
            _channel = GetConfigValue("UpdateChannel", "stable");

            _logger.LogInformation("UpdateService 初始化完成: ServerUrl={ServerUrl}, Channel={Channel}",
                _serverUrl, _channel);
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

                // 构建 API 请求 URL
                var requestUrl = $"{_serverUrl}/api/version/latest?channel={channel}";
                _logger.LogDebug("请求 URL: {RequestUrl}", requestUrl);

                // 调用 Go 服务器 API
                var response = await _httpClient.GetAsync(requestUrl);
                response.EnsureSuccessStatusCode();

                // 解析 JSON 响应
                var versionInfo = await response.Content.ReadFromJsonAsync<VersionInfo>();
                if (versionInfo == null)
                {
                    _logger.LogWarning("服务器返回的版本信息为空");
                    return null;
                }

                _logger.LogInformation("服务器最新版本: {Version}, 发布日期: {PublishDate}",
                    versionInfo.Version, versionInfo.PublishDate);

                // 比较版本号
                var comparison = CompareVersions(currentVersion, versionInfo.Version);
                if (comparison < 0)
                {
                    _logger.LogInformation("发现新版本: {LatestVersion} > {CurrentVersion}",
                        versionInfo.Version, currentVersion);

                    // 保存版本信息供后续使用
                    _currentVersionInfo = versionInfo;

                    // 触发更新可用事件
                    UpdateAvailable?.Invoke(this, new UpdateAvailableEventArgs
                    {
                        Version = versionInfo,
                        IsDownloaded = false
                    });

                    return versionInfo;
                }

                _logger.LogInformation("当前版本已是最新: {CurrentVersion}", currentVersion);
                return null;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "检查更新时网络请求失败");
                throw new UpdateException("检查更新时网络请求失败", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查更新时发生未知错误");
                throw new UpdateException("检查更新时发生未知错误", ex);
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

            // 保存版本信息供后续使用
            _currentVersionInfo = version;

            try
            {
                _logger.LogInformation("开始下载更新: Version={Version}, Url={DownloadUrl}",
                    version.Version, version.DownloadUrl);

                // 报告开始下载
                progress?.Report(new DownloadProgress
                {
                    BytesReceived = 0,
                    TotalBytes = version.FileSize,
                    ProgressPercentage = 0,
                    Status = "准备下载..."
                });

                // 流式下载（HttpClient 超时已在 DI 配置中设置为 300 秒）
                var response = await _httpClient.GetAsync(version.DownloadUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                // 获取内容总长度
                var totalBytes = response.Content.Headers.ContentLength ?? version.FileSize;
                var buffer = new byte[8192];
                var bytesRead = 0L;
                var totalRead = 0L;

                // 创建临时文件
                var tempFileName = $"DocuFiller_Update_{version.Version}_{Guid.NewGuid():N}.exe";
                var tempFilePath = Path.Combine(Path.GetTempPath(), tempFileName);

                _logger.LogDebug("临时文件路径: {TempFilePath}", tempFilePath);

                using (var contentStream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                {
                    while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, (int)bytesRead);
                        totalRead += bytesRead;

                        // 报告下载进度
                        var percentage = (int)((totalRead * 100) / totalBytes);
                        progress?.Report(new DownloadProgress
                        {
                            BytesReceived = totalRead,
                            TotalBytes = totalBytes,
                            ProgressPercentage = percentage,
                            Status = $"下载中... {percentage}%"
                        });
                    }
                }

                _logger.LogInformation("下载完成: {TempFilePath}, 大小: {FileSize} 字节",
                    tempFilePath, totalRead);

                // 报告下载完成
                progress?.Report(new DownloadProgress
                {
                    BytesReceived = totalRead,
                    TotalBytes = totalBytes,
                    ProgressPercentage = 100,
                    Status = "下载完成"
                });

                return tempFilePath;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "下载更新时网络请求失败");
                throw new UpdateException("下载更新时网络请求失败", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "下载更新时发生未知错误");
                throw new UpdateException("下载更新时发生未知错误", ex);
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

                // 验证文件大小（防止过大文件导致内存问题）
                var fileInfo = new FileInfo(packagePath);
                var maxSizeBytes = 500L * 1024L * 1024L; // 500MB - 与服务端配置一致
                if (fileInfo.Length > maxSizeBytes)
                {
                    _logger.LogError("更新包文件过大: {FileSize} 字节，超过最大限制 {MaxSize} 字节",
                        fileInfo.Length, maxSizeBytes);
                    try
                    {
                        File.Delete(packagePath);
                        _logger.LogInformation("已删除过大的更新包: {PackagePath}", packagePath);
                    }
                    catch (Exception deleteEx)
                    {
                        _logger.LogWarning(deleteEx, "删除过大更新包失败");
                    }
                    throw new UpdateException($"更新包文件过大: {fileInfo.Length} 字节，超过最大限制 500MB");
                }
                _logger.LogInformation("文件大小验证通过: {FileSize} 字节", fileInfo.Length);

                // 验证文件哈希
                if (_currentVersionInfo != null && !string.IsNullOrEmpty(_currentVersionInfo.FileHash))
                {
                    _logger.LogInformation("开始验证文件哈希值...");
                    var hashValid = await VerifyFileHashAsync(packagePath, _currentVersionInfo.FileHash);
                    if (!hashValid)
                    {
                        _logger.LogError("文件哈希验证失败，取消安装");
                        try
                        {
                            File.Delete(packagePath);
                            _logger.LogInformation("已删除哈希验证失败的更新包: {PackagePath}", packagePath);
                        }
                        catch (Exception deleteEx)
                        {
                            _logger.LogWarning(deleteEx, "删除无效更新包失败");
                        }
                        throw new UpdateException("下载的文件哈希值验证失败，文件可能已损坏或被篡改");
                    }
                    _logger.LogInformation("文件哈希验证成功");
                }
                else
                {
                    _logger.LogWarning("未找到版本信息或哈希值，跳过哈希验证");
                }

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

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "安装更新时发生错误");
                throw new UpdateException("安装更新时发生错误", ex);
            }
        }

        /// <summary>
        /// 验证文件哈希值
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="expectedHash">期望的哈希值</param>
        /// <returns>哈希值是否匹配</returns>
        private async Task<bool> VerifyFileHashAsync(string filePath, string expectedHash)
        {
            try
            {
                _logger.LogInformation("验证文件哈希: FilePath={FilePath}", filePath);

                using (var sha256 = SHA256.Create())
                using (var stream = File.OpenRead(filePath))
                {
                    var hashBytes = await Task.Run(() => sha256.ComputeHash(stream));
                    var hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

                    _logger.LogDebug("计算得到的哈希值: {Hash}", hashString);
                    _logger.LogDebug("期望的哈希值: {ExpectedHash}", expectedHash);

                    var isValid = hashString.Equals(expectedHash, StringComparison.OrdinalIgnoreCase);
                    if (isValid)
                    {
                        _logger.LogInformation("文件哈希验证成功");
                    }
                    else
                    {
                        _logger.LogError("文件哈希验证失败: 期望 {ExpectedHash}, 实际 {ActualHash}",
                            expectedHash, hashString);
                    }

                    return isValid;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证文件哈希时发生错误");
                return false;
            }
        }

        /// <summary>
        /// 比较两个语义化版本号
        /// </summary>
        /// <param name="version1">版本1</param>
        /// <param name="version2">版本2</param>
        /// <returns>
        /// 负数: version1 小于 version2
        /// 0: version1 等于 version2
        /// 正数: version1 大于 version2
        /// </returns>
        private int CompareVersions(string version1, string version2)
        {
            try
            {
                var v1 = new Version(version1.TrimStart('v'));
                var v2 = new Version(version2.TrimStart('v'));
                return v1.CompareTo(v2);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "版本号比较失败: Version1={Version1}, Version2={Version2}",
                    version1, version2);
                return string.Compare(version1, version2, StringComparison.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// 从配置获取值
        /// </summary>
        /// <param name="key">配置键</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>配置值</returns>
        private static string GetConfigValue(string key, string defaultValue)
        {
            try
            {
                var value = System.Configuration.ConfigurationManager.AppSettings[key];
                return string.IsNullOrEmpty(value) ? defaultValue : value;
            }
            catch
            {
                return defaultValue;
            }
        }
    }

    /// <summary>
    /// 更新异常类
    /// </summary>
    public class UpdateException : Exception
    {
        public UpdateException(string message) : base(message) { }

        public UpdateException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
