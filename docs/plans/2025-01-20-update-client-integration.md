# Update Client Integration Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use @superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 集成独立的 `update-client.exe` 工具到 DocuFiller 应用程序，替换现有的 HTTP API 更新方式，实现通过 Daemon 模式的实时下载进度监控。

**Architecture:** 使用独立的 `update-client.exe` 进程通过命令行和 HTTP API (Daemon 模式) 进行更新检查和下载。主程序负责启动进程、监控进度、更新 UI 和用户交互。

**Tech Stack:** .NET 8, WPF, System.Diagnostics.Process, System.Net.Http, Microsoft.Extensions.DependencyInjection

---

## Prerequisites

Before starting, ensure you have:

1. `update-client.exe` - 从 Update Server 下载
2. `update-config.yaml` - 配置文件，包含服务器地址、Token 等
3. Place both files in `External/` directory (created in Task 1)

**获取方式：**
- 登录 Update Server 管理后台
- 进入程序详情页
- 点击「下载更新端」
- 解压并将文件复制到 `External/` 目录

---

## Task 1: Create External Directory Structure

**Files:**
- Create: `External/.gitkeep`
- Modify: `.gitignore`

**Step 1: Create External directory with .gitkeep**

```bash
mkdir External
echo. > External/.gitkeep
```

**Step 2: Update .gitignore**

Add to `.gitignore`:

```gitignore
# External dependencies - manually placed
External/update-client.exe
External/update-config.yaml

# But keep directory structure
!External/.gitkeep
```

**Step 3: Verify structure**

Run: `git status`
Expected: `External/.gitkeep` is shown, but no warning about exe/yaml files (since they don't exist yet)

**Step 4: Commit**

```bash
git add External/.gitkeep .gitignore
git commit -m "feat: add External directory for update-client files"
```

---

## Task 2: Create Daemon API Response Models

**Files:**
- Create: `Models/Update/DownloadStatus.cs`
- Create: `Models/Update/DaemonProgressInfo.cs`
- Create: `Models/Update/UpdateClientResponseModels.cs`

**Step 1: Write the DaemonProgressInfo model**

Create: `Models/Update/DaemonProgressInfo.cs`

```csharp
using System.Text.Json.Serialization;

namespace DocuFiller.Models.Update
{
    /// <summary>
    /// Daemon 模式下的下载进度信息
    /// </summary>
    public class DaemonProgressInfo
    {
        [JsonPropertyName("downloaded")]
        public long Downloaded { get; set; }

        [JsonPropertyName("total")]
        public long Total { get; set; }

        [JsonPropertyName("percentage")]
        public double Percentage { get; set; }

        [JsonPropertyName("speed")]
        public long Speed { get; set; }
    }
}
```

**Step 2: Write the DownloadStatus model**

Create: `Models/Update/DownloadStatus.cs`

```csharp
using System.Text.Json.Serialization;

namespace DocuFiller.Models.Update
{
    /// <summary>
    /// Daemon 模式下 GET /status 的响应
    /// </summary>
    public class DownloadStatus
    {
        [JsonPropertyName("state")]
        public string State { get; set; } = string.Empty;  // idle | downloading | completed | error

        [JsonPropertyName("version")]
        public string Version { get; set; } = string.Empty;

        [JsonPropertyName("file")]
        public string File { get; set; } = string.Empty;

        [JsonPropertyName("progress")]
        public DaemonProgressInfo? Progress { get; set; }

        [JsonPropertyName("error")]
        public string Error { get; set; } = string.Empty;
    }
}
```

**Step 3: Write the update-client response models**

Create: `Models/Update/UpdateClientResponseModels.cs`

```csharp
using System;
using System.Text.Json.Serialization;

namespace DocuFiller.Models.Update
{
    /// <summary>
    /// update-client.exe check --json 的响应
    /// </summary>
    public class UpdateClientCheckResponse
    {
        [JsonPropertyName("hasUpdate")]
        public bool HasUpdate { get; set; }

        [JsonPropertyName("currentVersion")]
        public string CurrentVersion { get; set; } = string.Empty;

        [JsonPropertyName("latestVersion")]
        public string LatestVersion { get; set; } = string.Empty;

        [JsonPropertyName("downloadUrl")]
        public string DownloadUrl { get; set; } = string.Empty;

        [JsonPropertyName("fileSize")]
        public long FileSize { get; set; }

        [JsonPropertyName("releaseNotes")]
        public string ReleaseNotes { get; set; } = string.Empty;

        [JsonPropertyName("publishDate")]
        public DateTime PublishDate { get; set; }

        [JsonPropertyName("mandatory")]
        public bool Mandatory { get; set; }
    }

    /// <summary>
    /// update-client.exe download --json 的响应
    /// </summary>
    public class UpdateClientDownloadResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("file")]
        public string File { get; set; } = string.Empty;

        [JsonPropertyName("fileSize")]
        public long FileSize { get; set; }

        [JsonPropertyName("verified")]
        public bool Verified { get; set; }

        [JsonPropertyName("decrypted")]
        public bool Decrypted { get; set; }
    }
}
```

**Step 4: Build to verify**

Run: `dotnet build`
Expected: Success, no errors

**Step 5: Commit**

```bash
git add Models/Update/
git commit -m "feat: add Daemon API response models for update-client"
```

---

## Task 3: Implement UpdateDownloader (Daemon Manager)

**Files:**
- Create: `Services/Update/UpdateDownloader.cs`

**Step 1: Write the UpdateDownloader class**

Create: `Services/Update/UpdateDownloader.cs`

```csharp
using System;
using System.Net;
using System.Net.Http;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using DocuFiller.Models.Update;
using System.Text.Json;

namespace DocuFiller.Services.Update
{
    /// <summary>
    /// 管理 update-client.exe 的 Daemon 模式下载
    /// </summary>
    public class UpdateDownloader : IDisposable
    {
        private readonly ILogger<UpdateDownloader> _logger;
        private readonly string _updateClientPath;
        private Process? _process;
        private HttpClient? _httpClient;
        private int _port;

        public UpdateDownloader(ILogger<UpdateDownloader> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _updateClientPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "update-client.exe"
            );

            if (!File.Exists(_updateClientPath))
            {
                throw new FileNotFoundException(
                    $"update-client.exe not found at: {_updateClientPath}. " +
                    "Please ensure update-client.exe is in the application directory."
                );
            }
        }

        /// <summary>
        /// 启动下载进程（Daemon 模式）
        /// </summary>
        public async Task<bool> StartDownloadAsync(string version, string outputPath)
        {
            try
            {
                _port = GetAvailablePort();
                _logger.LogInformation("Starting download daemon on port {Port}", _port);

                var psi = new ProcessStartInfo
                {
                    FileName = _updateClientPath,
                    Arguments = $"download --daemon --port {_port} --version {version} --output \"{outputPath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                _process = Process.Start(psi);
                if (_process == null)
                {
                    _logger.LogError("Failed to start update-client process");
                    return false;
                }

                _logger.LogInformation("Update-client process started with PID {ProcessId}", _process.Id);

                // 等待 HTTP 服务器启动
                await Task.Delay(1000);

                _httpClient = new HttpClient
                {
                    BaseAddress = new Uri($"http://localhost:{_port}"),
                    Timeout = TimeSpan.FromSeconds(5)
                };

                // 验证服务器是否启动成功
                var status = await GetStatusAsync();
                if (status == null)
                {
                    _logger.LogError("Daemon HTTP server did not respond");
                    await CleanupProcessAsync();
                    return false;
                }

                _logger.LogInformation("Daemon HTTP server is ready");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start download daemon");
                await CleanupProcessAsync();
                return false;
            }
        }

        /// <summary>
        /// 获取下载状态
        /// </summary>
        public async Task<DownloadStatus?> GetStatusAsync()
        {
            if (_httpClient == null)
                return null;

            try
            {
                var response = await _httpClient.GetAsync("/status");
                if (!response.IsSuccessStatusCode)
                    return null;

                var json = await response.Content.ReadAsStringAsync();
                var status = JsonSerializer.Deserialize<DownloadStatus>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return status;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get status from daemon");
                return null;
            }
        }

        /// <summary>
        /// 监控下载进度
        /// </summary>
        public async Task<DownloadStatus?> MonitorDownloadAsync(
            Action<Models.Update.DownloadProgress> onProgress,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting to monitor download progress");

            while (!cancellationToken.IsCancellationRequested)
            {
                var status = await GetStatusAsync();
                if (status == null)
                {
                    _logger.LogWarning("Failed to get status, retrying...");
                    await Task.Delay(1000, cancellationToken);
                    continue;
                }

                _logger.LogDebug("State: {State}, Progress: {Percentage}%",
                    status.State, status.Progress?.Percentage ?? 0);

                switch (status.State)
                {
                    case "downloading":
                        if (status.Progress != null)
                        {
                            onProgress?.Invoke(ConvertProgress(status.Progress));
                        }
                        break;

                    case "completed":
                        _logger.LogInformation("Download completed: {File}", status.File);
                        return status;

                    case "error":
                        _logger.LogError("Download failed: {Error}", status.Error);
                        return status;

                    case "idle":
                        // Still starting, wait a bit
                        break;
                }

                await Task.Delay(1000, cancellationToken);
            }

            _logger.LogInformation("Download monitoring was cancelled");
            return null;
        }

        /// <summary>
        /// 关闭下载进程和 HTTP 服务器
        /// </summary>
        public async Task ShutdownAsync()
        {
            _logger.LogInformation("Shutting down download daemon");

            try
            {
                if (_httpClient != null)
                {
                    await _httpClient.PostAsync("/shutdown", null);
                    _logger.LogDebug("Sent shutdown command to daemon");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send shutdown command");
            }

            await CleanupProcessAsync();

            _httpClient?.Dispose();
            _httpClient = null;
        }

        /// <summary>
        /// 清理进程
        /// </summary>
        private async Task CleanupProcessAsync()
        {
            if (_process != null && !_process.HasExited)
            {
                _logger.LogDebug("Waiting for process to exit...");
                _process.WaitForExit(5000);

                if (!_process.HasExited)
                {
                    _logger.LogWarning("Process did not exit gracefully, killing...");
                    _process.Kill(entireProcessTree: true);
                }

                _process.Close();
                _process = null;
            }
        }

        /// <summary>
        /// 获取可用端口（19876-19880）
        /// </summary>
        private int GetAvailablePort()
        {
            for (int port = 19876; port <= 19880; port++)
            {
                if (IsPortAvailable(port))
                    return port;
            }

            throw new InvalidOperationException("No available port in range 19876-19880");
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
        private Models.Update.DownloadProgress ConvertProgress(DaemonProgressInfo daemonProgress)
        {
            return new Models.Update.DownloadProgress
            {
                BytesReceived = daemonProgress.Downloaded,
                TotalBytes = daemonProgress.Total,
                ProgressPercentage = (int)daemonProgress.Percentage,
                Status = $"下载中... {daemonProgress.Percentage:F1}%"
            };
        }

        public void Dispose()
        {
            ShutdownAsync().Wait();
            GC.SuppressFinalize(this);
        }
    }
}
```

**Step 2: Build to verify**

Run: `dotnet build`
Expected: Success

**Step 3: Commit**

```bash
git add Services/Update/UpdateDownloader.cs
git commit -m "feat: implement UpdateDownloader for Daemon mode management"
```

---

## Task 4: Implement UpdateClientService

**Files:**
- Create: `Services/Update/UpdateClientService.cs`
- Modify: `Services/Update/IUpdateService.cs` (adjust method signatures if needed)

**Step 1: Update IUpdateService interface**

Modify: `Services/Update/IUpdateService.cs`

Keep existing interface, but note that `DownloadUpdateAsync` will now use Daemon mode internally:

```csharp
using System;
using System.Threading.Tasks;
using DocuFiller.Models.Update;

namespace DocuFiller.Services.Update
{
    public interface IUpdateService
    {
        Task<VersionInfo?> CheckForUpdateAsync(string currentVersion, string channel);
        Task<string> DownloadUpdateAsync(VersionInfo version, IProgress<DownloadProgress> progress);
        Task<bool> InstallUpdateAsync(string packagePath);
        event EventHandler<UpdateAvailableEventArgs>? UpdateAvailable;
    }

    public class UpdateAvailableEventArgs : EventArgs
    {
        public required VersionInfo Version { get; set; }
        public bool IsDownloaded { get; set; }
    }
}
```

**Step 2: Write UpdateClientService**

Create: `Services/Update/UpdateClientService.cs`

```csharp
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using DocuFiller.Models.Update;

namespace DocuFiller.Services.Update
{
    /// <summary>
    /// 使用 update-client.exe 的更新服务实现
    /// </summary>
    public class UpdateClientService : IUpdateService
    {
        private readonly ILogger<UpdateClientService> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _updateClientPath;
        private VersionInfo? _lastCheckedVersion;

        public event EventHandler<UpdateAvailableEventArgs>? UpdateAvailable;

        public UpdateClientService(ILogger<UpdateClientService> logger, IConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            _updateClientPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "update-client.exe"
            );

            if (!File.Exists(_updateClientPath))
            {
                _logger.LogWarning("update-client.exe not found at: {Path}", _updateClientPath);
            }
        }

        /// <summary>
        /// 检查更新
        /// </summary>
        public async Task<VersionInfo?> CheckForUpdateAsync(string currentVersion, string channel)
        {
            if (!File.Exists(_updateClientPath))
            {
                _logger.LogError("Cannot check for updates: update-client.exe not found");
                return null;
            }

            try
            {
                _logger.LogInformation("Checking for updates: CurrentVersion={CurrentVersion}, Channel={Channel}",
                    currentVersion, channel);

                var result = await RunCommandAsync("check", "--current-version", currentVersion, "--json");

                if (result.ExitCode != 0)
                {
                    _logger.LogError("update-client check failed with exit code {ExitCode}: {Error}",
                        result.ExitCode, result.ErrorOutput);
                    return null;
                }

                var checkResponse = JsonSerializer.Deserialize<UpdateClientCheckResponse>(
                    result.Output,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                if (checkResponse == null || !checkResponse.HasUpdate)
                {
                    _logger.LogInformation("No updates available");
                    return null;
                }

                _logger.LogInformation("Update found: {Version}", checkResponse.LatestVersion);

                var versionInfo = ConvertToVersionInfo(checkResponse, channel);
                _lastCheckedVersion = versionInfo;

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
                _logger.LogError(ex, "Failed to check for updates");
                return null;
            }
        }

        /// <summary>
        /// 下载更新（使用 Daemon 模式）
        /// </summary>
        public async Task<string> DownloadUpdateAsync(VersionInfo version, IProgress<DownloadProgress> progress)
        {
            if (!File.Exists(_updateClientPath))
            {
                throw new FileNotFoundException("update-client.exe not found", _updateClientPath);
            }

            _logger.LogInformation("Starting download for version {Version}", version.Version);

            var downloader = new UpdateDownloader(_logger);
            try
            {
                // 输出路径
                var outputPath = Path.Combine(
                    Path.GetTempPath(),
                    "DocuFiller",
                    "Updates"
                );

                if (!Directory.Exists(outputPath))
                {
                    Directory.CreateDirectory(outputPath);
                }

                // 启动 Daemon
                if (!await downloader.StartDownloadAsync(version.Version, outputPath))
                {
                    throw new UpdateException("Failed to start download daemon");
                }

                // 监控进度
                var result = await downloader.MonitorDownloadAsync(
                    p => progress?.Report(p),
                    default
                );

                if (result?.State == "completed")
                {
                    _logger.LogInformation("Download completed: {File}", result.File);
                    return result.File;
                }
                else if (result?.State == "error")
                {
                    throw new UpdateException($"Download failed: {result.Error}");
                }

                throw new UpdateException("Download did not complete");
            }
            finally
            {
                // 始终关闭 Daemon
                await downloader.ShutdownAsync();
            }
        }

        /// <summary>
        /// 安装更新
        /// </summary>
        public async Task<bool> InstallUpdateAsync(string packagePath)
        {
            if (string.IsNullOrEmpty(packagePath))
                throw new ArgumentException("Package path cannot be empty", nameof(packagePath));

            if (!File.Exists(packagePath))
                throw new FileNotFoundException("Update package not found", packagePath);

            try
            {
                _logger.LogInformation("Installing update from: {Path}", packagePath);

                var startInfo = new ProcessStartInfo
                {
                    FileName = packagePath,
                    UseShellExecute = true,
                    Verb = "runas",
                    Arguments = "/silent /norestart"
                };

                var process = Process.Start(startInfo);
                if (process == null)
                {
                    _logger.LogError("Failed to start installer");
                    return false;
                }

                _logger.LogInformation("Installer started with PID {ProcessId}", process.Id);

                // 关闭当前应用
                _logger.LogInformation("Shutting down application...");
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    System.Windows.Application.Current.Shutdown();
                });

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to install update");
                throw new UpdateException("Failed to install update", ex);
            }
        }

        /// <summary>
        /// 运行 update-client 命令
        /// </summary>
        private async Task<CommandResult> RunCommandAsync(params string[] args)
        {
            var psi = new ProcessStartInfo
            {
                FileName = _updateClientPath,
                Arguments = string.Join(" ", args),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            using var process = Process.Start(psi);
            if (process == null)
            {
                return new CommandResult { ExitCode = -1, Output = "", ErrorOutput = "Failed to start process" };
            }

            await process.WaitForExitAsync();

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();

            return new CommandResult
            {
                ExitCode = process.ExitCode,
                Output = output,
                ErrorOutput = error
            };
        }

        /// <summary>
        /// 转换为 VersionInfo
        /// </summary>
        private VersionInfo ConvertToVersionInfo(UpdateClientCheckResponse response, string channel)
        {
            return new VersionInfo
            {
                Version = response.LatestVersion,
                Channel = channel,
                FileName = $"DocuFiller-{response.LatestVersion}.exe",
                FileSize = response.FileSize,
                FileHash = "", // update-client 会验证，不需要我们保存
                ReleaseNotes = response.ReleaseNotes,
                PublishDate = response.PublishDate,
                Mandatory = response.Mandatory,
                DownloadUrl = response.DownloadUrl
            };
        }

        private class CommandResult
        {
            public int ExitCode { get; set; }
            public string Output { get; set; } = string.Empty;
            public string ErrorOutput { get; set; } = string.Empty;
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
```

**Step 3: Build to verify**

Run: `dotnet build`
Expected: Success

**Step 4: Commit**

```bash
git add Services/Update/UpdateClientService.cs
git commit -m "feat: implement UpdateClientService using update-client.exe"
```

---

## Task 5: Update Project File with Build Validation

**Files:**
- Modify: `DocuFiller.csproj`

**Step 1: Add build validation targets**

Add to `DocuFiller.csproj` before the closing `</Project>` tag:

```xml
<ItemGroup>
  <!-- External 文件复制到输出目录 -->
  <None Update="External\update-client.exe">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    <Link>update-client.exe</Link>
  </None>
  <None Update="External\update-config.yaml">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    <Link>update-config.yaml</Link>
  </None>
</ItemGroup>

<Target Name="ValidateUpdateClientFiles" BeforeTargets="BeforeBuild">
  <PropertyGroup>
    <UpdateClientPath>$(ProjectDir)External\update-client.exe</UpdateClientPath>
    <UpdateConfigPath>$(ProjectDir)External\update-config.yaml</UpdateConfigPath>
  </PropertyGroup>

  <Error Text="&#x0A;╔═══════════════════════════════════════════════════════════════╗&#x0A;║  ❌ 构建失败：未找到 update-client.exe                           ║&#x0A;╠═══════════════════════════════════════════════════════════════╣&#x0A;║  请将 update-client.exe 放置在以下位置：                         ║&#x0A;║  $(UpdateClientPath)                                           ║&#x0A;║                                                                   ║&#x0A;║  获取方式：                                                        ║&#x0A;║  1. 登录 Update Server 管理后台                                  ║&#x0A;║  2. 进入程序详情页                                                ║&#x0A;║  3. 点击「下载更新端」                                             ║&#x0A;║  4. 解压并将文件复制到 External/ 目录                             ║&#x0A;║                                                                   ║&#x0A;║  参考文档：docs/EXTERNAL_SETUP.md                                  ║&#x0A;╚═══════════════════════════════════════════════════════════════╝"
         Condition="!Exists('$(UpdateClientPath)')" />

  <Error Text="&#x0A;╔═══════════════════════════════════════════════════════════════╗&#x0A;║  ❌ 构建失败：未找到 update-config.yaml                          ║&#x0A;╠═══════════════════════════════════════════════════════════════╣&#x0A;║  请将 update-config.yaml 放置在以下位置：                        ║&#x0A;║  $(UpdateConfigPath)                                            ║&#x0A;║                                                                   ║&#x0A;║  获取方式：                                                        ║&#x0A;║  1. 登录 Update Server 管理后台                                  ║&#x0A;║  2. 进入程序详情页                                                ║&#x0A;║  3. 点击「下载更新端」                                             ║&#x0A;║  4. 解压并将文件复制到 External/ 目录                             ║&#x0A;║                                                                   ║&#x0A;║  参考文档：docs/EXTERNAL_SETUP.md                                  ║&#x0A;╚═══════════════════════════════════════════════════════════════╝"
         Condition="!Exists('$(UpdateConfigPath)')" />

  <Message Text="&#x0A;╔═══════════════════════════════════════════════════════════════╗&#x0A;║  ✓ Update Client 文件验证通过                                   ║&#x0A;╚═══════════════════════════════════════════════════════════════╝"
           Importance="high"
           Condition="Exists('$(UpdateClientPath)') And Exists('$(UpdateConfigPath)')" />
</Target>

<Target Name="ValidateReleaseFiles" AfterTargets="Publish">
  <PropertyGroup>
    <PublishDir>$(PublishDir)</PublishDir>
  </PropertyGroup>

  <Error Text="❌ 发布包缺少 update-client.exe"
         Condition="!Exists('$(PublishDir)update-client.exe')" />

  <Error Text="❌ 发布包缺少 update-config.yaml"
         Condition="!Exists('$(PublishDir)update-config.yaml')" />

  <Message Text="✓ 发布包验证完成" Importance="high" />
</Target>
```

**Step 2: Build to verify validation fails without files**

Run: `dotnet build`
Expected: Build failure with clear error message about missing files

**Step 3: Test build succeeds with placeholder files**

```bash
# Create placeholder files for testing
echo. > External/update-client.exe
echo. > External/update-config.yaml
dotnet build
# Expected: Success with validation message

# Clean up placeholders
rm External/update-client.exe External/update-config.yaml
```

**Step 4: Commit**

```bash
git add DocuFiller.csproj
git commit -m "feat: add build validation for update-client files"
```

---

## Task 6: Update App.xaml.cs Service Registration

**Files:**
- Modify: `App.xaml.cs`

**Step 1: Update service registration**

Find the service registration section in `App.xaml.cs` and update:

```csharp
// 注册更新服务
// 注意：不再使用 AddHttpClient，UpdateClientService 直接调用 update-client.exe
services.AddSingleton<IUpdateService, UpdateClientService>();
```

Remove the old line:
```csharp
// Delete this line:
// services.AddHttpClient<IUpdateService, UpdateService>(client =>
// {
//     client.Timeout = TimeSpan.FromSeconds(300);
// });
```

**Step 2: Build to verify**

Run: `dotnet build`
Expected: Success (but will fail at runtime without update-client.exe)

**Step 3: Commit**

```bash
git add App.xaml.cs
git commit -m "refactor: switch to UpdateClientService registration"
```

---

## Task 7: Create UpdateBannerViewModel

**Files:**
- Create: `ViewModels/UpdateBannerViewModel.cs`

**Step 1: Write the UpdateBannerViewModel**

Create: `ViewModels/UpdateBannerViewModel.cs`

```csharp
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using DocuFiller.Models.Update;
using DocuFiller.Services.Update;
using Microsoft.Extensions.Logging;

namespace DocuFiller.ViewModels
{
    /// <summary>
    /// 顶部更新横幅的 ViewModel
    /// </summary>
    public class UpdateBannerViewModel : ObservableObject
    {
        private readonly IUpdateService _updateService;
        private readonly ILogger<UpdateBannerViewModel> _logger;
        private VersionInfo? _currentUpdate;

        private bool _isVisible;
        private bool _isDownloading;
        private bool _canInstall;
        private int _downloadProgress;
        private string _statusMessage = string.Empty;
        private string? _downloadedFilePath;

        public ICommand UpdateCommand { get; }
        public ICommand InstallCommand { get; }
        public ICommand CloseCommand { get; }
        public ICommand RemindLaterCommand { get; }

        public bool IsVisible
        {
            get => _isVisible;
            set => SetProperty(ref _isVisible, value);
        }

        public string Version => _currentUpdate?.Version ?? string.Empty;

        public string ChannelBadge => _currentUpdate?.Channel.ToUpperInvariant() ?? string.Empty;

        public Brush ChannelBackground => _currentUpdate?.Channel.ToLowerInvariant() switch
        {
            "stable" => new SolidColorBrush(Color.FromRgb(46, 204, 113)),  // Green
            "beta" => new SolidColorBrush(Color.FromRgb(241, 196, 15)),   // Yellow
            "alpha" => new SolidColorBrush(Color.FromRgb(231, 76, 60)),  // Red
            _ => new SolidColorBrush(Colors.Gray)
        };

        public string Summary => GetSummary();

        public bool ShowWarning => _currentUpdate?.Channel.ToLowerInvariant() != "stable";

        public string ButtonText => _canInstall ? "安装更新" : "立即更新";

        public bool IsDownloading
        {
            get => _isDownloading;
            set
            {
                if (SetProperty(ref _isDownloading, value))
                {
                    OnPropertyChanged(nameof(ButtonText));
                }
            }
        }

        public int DownloadProgress
        {
            get => _downloadProgress;
            set => SetProperty(ref _downloadProgress, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public bool CanInstall
        {
            get => _canInstall;
            set
            {
                if (SetProperty(ref _canInstall, value))
                {
                    OnPropertyChanged(nameof(ButtonText));
                }
            }
        }

        public UpdateBannerViewModel(
            IUpdateService updateService,
            ILogger<UpdateBannerViewModel> logger)
        {
            _updateService = updateService ?? throw new ArgumentNullException(nameof(updateService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            UpdateCommand = new RelayCommand(
                async () => await OnUpdateAsync(),
                () => !IsDownloading
            );

            InstallCommand = new RelayCommand(
                async () => await OnInstallAsync(),
                () => CanInstall && !IsDownloading
            );

            CloseCommand = new RelayCommand(OnClose);
            RemindLaterCommand = new RelayCommand(OnRemindLater);
        }

        /// <summary>
        /// 显示更新横幅
        /// </summary>
        public void ShowUpdate(VersionInfo updateInfo)
        {
            _currentUpdate = updateInfo ?? throw new ArgumentNullException(nameof(updateInfo));
            StatusMessage = $"发现新版本 {_currentUpdate.Version}";
            IsVisible = true;
            IsDownloading = false;
            CanInstall = false;
            DownloadProgress = 0;

            _logger.LogInformation("Update banner shown: {Version} ({Channel})",
                _currentUpdate.Version, _currentUpdate.Channel);

            OnPropertyChanged(nameof(Version));
            OnPropertyChanged(nameof(ChannelBadge));
            OnPropertyChanged(nameof(ChannelBackground));
            OnPropertyChanged(nameof(Summary));
            OnPropertyChanged(nameof(ShowWarning));
            OnPropertyChanged(nameof(ButtonText));
        }

        /// <summary>
        /// 隐藏横幅
        /// </summary>
        public void Hide()
        {
            IsVisible = false;
        }

        /// <summary>
        /// 立即更新 / 下载更新
        /// </summary>
        private async System.Threading.Tasks.Task OnUpdateAsync()
        {
            if (_currentUpdate == null)
                return;

            IsDownloading = true;
            StatusMessage = "正在下载...";
            DownloadProgress = 0;

            var progress = new Progress<DownloadProgress>(p =>
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    DownloadProgress = p.ProgressPercentage;
                    StatusMessage = p.Status;
                });
            });

            try
            {
                _logger.LogInformation("Starting download: {Version}", _currentUpdate.Version);

                var filePath = await _updateService.DownloadUpdateAsync(_currentUpdate, progress);

                _downloadedFilePath = filePath;
                StatusMessage = "下载完成！";
                CanInstall = true;

                _logger.LogInformation("Download completed: {Path}", filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Download failed");
                StatusMessage = $"下载失败: {ex.Message}";
            }
            finally
            {
                IsDownloading = false;
            }
        }

        /// <summary>
        /// 安装更新
        /// </summary>
        private async System.Threading.Tasks.Task OnInstallAsync()
        {
            if (string.IsNullOrEmpty(_downloadedFilePath))
            {
                StatusMessage = "错误: 未找到下载的文件";
                return;
            }

            // 对于 Beta/Alpha 版本，显示警告
            if (ShowWarning)
            {
                var result = MessageBox.Show(
                    $"⚠️  测试版更新警告\n\n" +
                    $"您即将安装 {_currentUpdate?.Channel.ToUpperInvariant()} 版本 {_currentUpdate?.Version}\n\n" +
                    $"⚠️ 测试版可能包含：\n" +
                    $"  • 未完成的特性\n" +
                    $"  • 未知的 Bug\n" +
                    $"  • 数据不稳定风险\n\n" +
                    $"建议在虚拟机或测试环境中使用\n\n" +
                    $"是否继续？",
                    "测试版警告",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning
                );

                if (result == MessageBoxResult.No)
                    return;
            }

            StatusMessage = "正在安装...";

            try
            {
                _logger.LogInformation("Starting install: {Path}", _downloadedFilePath);

                await _updateService.InstallUpdateAsync(_downloadedFilePath);

                // 注意: 如果成功，应用会关闭，不会执行到这里
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Install failed");
                StatusMessage = $"安装失败: {ex.Message}";
            }
        }

        /// <summary>
        /// 关闭横幅
        /// </summary>
        private void OnClose()
        {
            _logger.LogInformation("User closed update banner: {Version}", _currentUpdate?.Version);
            // TODO: 记录跳过的版本
            Hide();
        }

        /// <summary>
        /// 稍后提醒
        /// </summary>
        private void OnRemindLater()
        {
            _logger.LogInformation("User chose remind later: {Version}", _currentUpdate?.Version);
            // TODO: 记录提醒时间
            Hide();
        }

        /// <summary>
        /// 获取更新摘要
        /// </summary>
        private string GetSummary()
        {
            if (_currentUpdate == null)
                return string.Empty;

            var notes = _currentUpdate.ReleaseNotes;
            if (string.IsNullOrWhiteSpace(notes))
                return "查看更新详情";

            // 取第一行作为摘要
            var lines = notes.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            return lines.Length > 0 ? lines[0] : "查看更新详情";
        }
    }
}
```

**Step 2: Create ObservableObject base class if not exists**

If `ObservableObject` doesn't exist in `ViewModels/`, create it:

Create: `ViewModels/ObservableObject.cs`

```csharp
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DocuFiller.ViewModels
{
    /// <summary>
    /// ObservableObject 基类
    /// </summary>
    public class ObservableObject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
```

**Step 3: Build to verify**

Run: `dotnet build`
Expected: Success

**Step 4: Commit**

```bash
git add ViewModels/UpdateBannerViewModel.cs ViewModels/ObservableObject.cs
git commit -m "feat: add UpdateBannerViewModel for top banner UI"
```

---

## Task 8: Create UpdateBannerView

**Files:**
- Create: `Views/UpdateBannerView.xaml`
- Create: `Views/UpdateBannerView.xaml.cs`

**Step 1: Write the XAML view**

Create: `Views/UpdateBannerView.xaml`

```xml
<UserControl x:Class="DocuFiller.Views.UpdateBannerView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             mc:Ignorable="d"
             d:DesignHeight="48" d:DesignWidth="800"
             Visibility="{Binding IsVisible, Converter={StaticResource BoolToVisibilityConverter}}">

    <UserControl.Resources>
        <!-- Boolean to Visibility Converter -->
        <BooleanToVisibilityConverter x:Key="BoolToVisibilityConverter"/>
    </UserControl.Resources>

    <Border Background="#007ACC" Padding="12,8" CornerRadius="0">
        <Border.Effect>
            <DropShadowEffect Color="Black" Opacity="0.2" BlurRadius="8" OffsetY="2"/>
        </Border.Effect>

        <Grid>
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="Auto"/>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>

            <!-- 左侧图标和版本信息 -->
            <StackPanel Grid.Column="0" Orientation="Horizontal">
                <TextBlock Text="&#128190;" FontSize="18" Margin="0,0,8,0" VerticalAlignment="Center"/>
                <TextBlock VerticalAlignment="Center">
                    <Run Text="发现新版本"/>
                    <Run Text="{Binding Version}" FontWeight="Bold"/>
                    <Run Text="["/>
                    <Run Text="{Binding ChannelBadge}" Foreground="#AED6F1"/>
                    <Run Text="]"/>
                </TextBlock>
            </StackPanel>

            <!-- 中间状态/进度 -->
            <TextBlock Grid.Column="1"
                       Text="{Binding StatusMessage}"
                       VerticalAlignment="Center"
                       HorizontalAlignment="Center"
                       Foreground="White"
                       Opacity="0.9"
                       TextWrapping="NoWrap"/>

            <!-- 右侧按钮 -->
            <StackPanel Grid.Column="2" Orientation="Horizontal">
                <!-- 下载/安装按钮 -->
                <Button Content="{Binding ButtonText}"
                        Command="{Binding UpdateCommand}"
                        Padding="16,6"
                        Margin="0,0,8,0"
                        Background="White"
                        Foreground="#007ACC"
                        BorderThickness="0"
                        FontWeight="SemiBold"
                        Cursor="Hand"/>

                <!-- 稍后提醒 -->
                <TextBlock Text="稍后提醒"
                           Foreground="White"
                           VerticalAlignment="Center"
                           Margin="0,0,16,0"
                           Cursor="Hand"
                           MouseDown="OnRemindLaterClick">
                    <TextBlock.Style>
                        <Style TargetType="TextBlock">
                            <Style.Triggers>
                                <Trigger Property="IsMouseOver" Value="True">
                                    <Setter Property="Opacity" Value="0.8"/>
                                </Trigger>
                            </Style.Triggers>
                        </Style>
                    </TextBlock.Style>
                </TextBlock>

                <!-- 关闭按钮 -->
                <Button Content="×"
                        Command="{Binding CloseCommand}"
                        Width="32"
                        Height="32"
                        Background="Transparent"
                        Foreground="White"
                        BorderThickness="0"
                        FontSize="20"
                        Cursor="Hand"
                        Padding="0">
                    <Button.Style>
                        <Style TargetType="Button">
                            <Style.Triggers>
                                <Trigger Property="IsMouseOver" Value="True">
                                    <Setter Property="Background" Value="#E74C3C"/>
                                </Trigger>
                            </Style.Triggers>
                        </Style>
                    </Button.Style>
                </Button>
            </StackPanel>

            <!-- 下载进度条（下载时显示） -->
            <ProgressBar Grid.Column="0"
                         Grid.ColumnSpan="3"
                         Height="3"
                         VerticalAlignment="Bottom"
                         Value="{Binding DownloadProgress}"
                         Maximum="100"
                         Foreground="White"
                         Visibility="{Binding IsDownloading, Converter={StaticResource BoolToVisibilityConverter}}"/>
        </Grid>
    </Border>
</UserControl>
```

**Step 2: Write the code-behind**

Create: `Views/UpdateBannerView.xaml.cs`

```csharp
using System.Windows;
using System.Windows.Controls;

namespace DocuFiller.Views
{
    /// <summary>
    /// UpdateBannerView.xaml 的交互逻辑
    /// </summary>
    public partial class UpdateBannerView : UserControl
    {
        public UpdateBannerView()
        {
            InitializeComponent();
        }

        private void OnRemindLaterClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (DataContext is ViewModels.UpdateBannerViewModel vm)
            {
                vm.RemindLaterCommand.Execute(null);
            }
        }
    }
}
```

**Step 3: Build to verify**

Run: `dotnet build`
Expected: Success

**Step 4: Commit**

```bash
git add Views/UpdateBannerView.xaml Views/UpdateBannerView.xaml.cs
git commit -m "feat: add UpdateBannerView with download progress UI"
```

---

## Task 9: Update MainWindowViewModel

**Files:**
- Modify: `ViewModels/MainWindowViewModel.cs`

**Step 1: Add update checking on startup**

Add to `MainWindowViewModel.cs`:

```csharp
private readonly UpdateBannerViewModel _updateBanner;

// In constructor:
public MainWindowViewModel(
    // ... existing dependencies
    IUpdateService updateService,
    ILogger<MainWindowViewModel> logger)
{
    // ... existing code

    // Initialize update banner
    _updateBanner = new UpdateBannerViewModel(updateService, logger);

    // Check for updates on startup (async, don't block UI)
    Task.Run(async () => await CheckForUpdatesAsync());
}

/// <summary>
/// 检查更新（后台执行）
/// </summary>
private async System.Threading.Tasks.Task CheckForUpdatesAsync()
{
    try
    {
        // Get current version
        var currentVersion = UpdateService.GetCurrentVersion();
        var channel = "stable"; // TODO: 从配置读取

        _logger.LogInformation("Checking for updates on startup: {Version}", currentVersion);

        var updateInfo = await _updateService.CheckForUpdateAsync(currentVersion, channel);

        if (updateInfo != null)
        {
            // 在 UI 线程显示横幅
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                _updateBanner.ShowUpdate(updateInfo);
            });
        }
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "Failed to check for updates on startup");
        // 静默失败，不影响用户使用
    }
}

/// <summary>
/// Update Banner 属性
/// </summary>
public UpdateBannerViewModel UpdateBanner => _updateBanner;
```

**Step 2: Build to verify**

Run: `dotnet build`
Expected: Success

**Step 3: Commit**

```bash
git add ViewModels/MainWindowViewModel.cs
git commit -m "feat: add startup update check to MainWindowViewModel"
```

---

## Task 10: Integrate UpdateBannerView into MainWindow

**Files:**
- Modify: `Views/MainWindow.xaml`
- Modify: `App.xaml.cs` (register view)

**Step 1: Register UpdateBannerView in App.xaml.cs**

Add to service registration in `App.xaml.cs`:

```csharp
// 注册 Views
services.AddTransient<Views.Update.UpdateWindow>();
services.AddTransient<Views.UpdateBannerView>();  // Add this line
```

**Step 2: Update MainWindow.xaml**

Modify: `Views/MainWindow.xaml`

Add the banner to the top of the window:

```xml
<Window x:Class="DocuFiller.Views.MainWindow"
        ...>

    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>  <!-- Update Banner -->
            <RowDefinition Height="*"/>     <!-- Main Content -->
        </Grid.RowDefinitions>

        <!-- Update Banner -->
        <views:UpdateBannerView Grid.Row="0"
                                DataContext="{Binding UpdateBanner}"/>

        <!-- Main Content (move existing content here) -->
        <Grid Grid.Row="1">
            <!-- ... existing content ... -->
        </Grid>
    </Grid>

</Window>
```

Add namespace if not present:

```xml
<Window xmlns:views="clr-namespace:DocuFiller.Views"
        ...>
```

**Step 3: Build to verify**

Run: `dotnet build`
Expected: Success

**Step 4: Commit**

```bash
git add Views/MainWindow.xaml App.xaml.cs
git commit -m "feat: integrate UpdateBannerView into MainWindow"
```

---

## Task 11: Create External Setup Documentation

**Files:**
- Create: `docs/EXTERNAL_SETUP.md`

**Step 1: Write the setup guide**

Create: `docs/EXTERNAL_SETUP.md`

```markdown
# 设置 Update Client

本文档说明如何设置 `update-client.exe` 和相关配置文件。

## 首次设置

### 1. 获取 Update Client

1. 登录 Update Server 管理后台
2. 进入 DocuFiller 程序详情页
3. 点击「下载更新端」按钮
4. 下载并解压 zip 包

### 2. 放置文件

将以下文件复制到项目的 `External/` 目录：

```
docx_replacer/
├── External/
│   ├── update-client.exe      # 更新客户端工具
│   └── update-config.yaml     # 配置文件
```

### 3. 验证设置

运行构建命令验证文件是否正确放置：

```bash
dotnet build
```

如果文件正确放置，你将看到：

```
╔═══════════════════════════════════════════════════════════════╗
║  ✓ Update Client 文件验证完成                                 ║
╚═══════════════════════════════════════════════════════════════╝
```

如果文件缺失，构建将失败并显示清晰的错误提示。

## 配置文件说明

`update-config.yaml` 包含以下关键配置：

```yaml
server:
  url: "http://your-server:8080"    # 更新服务器地址

program:
  id: "docufiller"                    # 程序 ID
  current_version: "1.0.0"            # 当前版本号

auth:
  token: "dl_xxxxxx"                  # Download Token
  encryption_key: "base64key"         # 加密密钥（如启用）

download:
  save_path: "./updates"              # 下载目录
  auto_verify: true                   # 自动验证 SHA256
```

## 更新 Update Client

当 update-client 有新版本时：

1. 从 Update Server 下载新版本
2. 替换 `External/update-client.exe`
3. 检查配置格式是否需要更新
4. 运行 `dotnet build` 验证

## 故障排查

### 构建失败

**问题：** 构建时提示缺少 update-client.exe 或 update-config.yaml

**解决：**
1. 确认文件已放置在 `External/` 目录
2. 确认文件名拼写正确
3. 检查 .gitignore 是否排除了这些文件

### 运行时错误

**问题：** 应用启动后无法检查更新

**解决：**
1. 确认 `update-client.exe` 和 `update-config.yaml` 已复制到输出目录
2. 检查配置文件中的服务器地址是否正确
3. 查看日志文件了解详细错误信息

## 开发注意事项

- **不要提交到 git**: `update-client.exe` 和 `update-config.yaml` 包含敏感信息（Token）
- **团队协作**: 每位开发者需要自行获取配置文件
- **CI/CD**: 在 CI 环境中使用测试配置或跳过更新功能

## 相关文档

- [Update Client 使用指南](../LiteHomeLab/update-server/docs/UPDATE_CLIENT_GUIDE.md)
- [项目构建文档](deployment-guide.md)
```

**Step 2: Commit**

```bash
git add docs/EXTERNAL_SETUP.md
git commit -m "docs: add external setup guide for update-client"
```

---

## Task 12: Create Design Documentation

**Files:**
- Create: `docs/plans/2025-01-20-update-client-design-notes.md`

**Step 1: Write design notes**

Create: `docs/plans/2025-01-20-update-client-design-notes.md`

```markdown
# Update Client Integration - Design Notes

## Architecture Decisions

### Why update-client.exe?

1. **Separation of Concerns**: Update logic is decoupled from the main application
2. **Language Agnostic**: update-client can be written in Go, used by any app
3. **Security**: Token and encryption keys are stored separately
4. **Maintainability**: Update logic can be updated independently

### Daemon Mode

The Daemon mode provides real-time progress monitoring via HTTP API:

- **Port Range**: 19876-19880 (auto-selection)
- **Endpoints**:
  - `GET /status` - Get download status and progress
  - `POST /shutdown` - Gracefully shutdown the daemon
- **Parent Process Monitoring**: Daemon exits if parent dies

## Component Overview

### UpdateClientService
- Replaces HTTP API calls with process execution
- Uses `update-client.exe check` for version checking
- Uses `update-client.exe download --daemon` for downloads

### UpdateDownloader
- Manages the Daemon lifecycle
- Handles port allocation
- Monitors progress via HTTP polling
- Ensures cleanup (shutdown) in all scenarios

### UpdateBannerView/ViewModel
- Non-intrusive top banner UI
- Channel-specific styling (Stable/Beta/Alpha)
- Download progress visualization
- User choice persistence (skip/remind later)

## Data Flow

```
App Startup
    → Check for Updates (background, async)
    → Show Banner if update available
    → User clicks "Update Now"
    → Start Daemon
    → Monitor Progress (poll /status)
    → Download Complete
    → Shutdown Daemon
    → User clicks "Install"
    → Launch installer
    → Close app
```

## Error Handling

| Scenario | Detection | User Action |
|----------|-----------|-------------|
| Files missing | Build validation | Place files in External/ |
| Port exhaustion | Port scan fails | Retry or close other apps |
| Download failure | state="error" | Retry button |
| Install failure | Process.Start=null | Show file path for manual install |

## Future Enhancements

1. **Auto-download**: Download updates in background
2. **Delta updates**: Only download changed files
3. **Rollback**: Keep previous versions for rollback
4. **Metrics**: Track update success rates
5. **Notification**: System tray notification for downloaded updates
```

**Step 2: Commit**

```bash
git add docs/plans/2025-01-20-update-client-design-notes.md
git commit -m "docs: add design notes for update-client integration"
```

---

## Final Steps

### 1. Manual Testing

Before considering this feature complete:

```bash
# 1. Place update-client.exe and update-config.yaml in External/
# 2. Build
dotnet build

# 3. Run
dotnet run

# 4. Verify:
#    - App starts without errors
#    - Update banner appears (if update available)
#    - Download progress shows
#    - Install button works
```

### 2. Integration Testing Checklist

- [ ] External files missing → Build fails with clear message
- [ ] No update available → Banner not shown
- [ ] Update available → Banner appears with correct version
- [ ] Stable version → Green badge, "立即更新" button
- [ ] Beta version → Yellow badge, "查看详情" button
- [ ] Download progress → Updates correctly
- [ ] Download complete → "安装更新" button appears
- [ ] Install clicked → Installer launches, app closes
- [ ] Close banner → Banner hides, doesn't reappear for same version
- [ ] Remind later → Banner hides, can reappear after 24h

### 3. Create Git Tag

```bash
git tag -a v1.1.0 -m "feat: integrate update-client for auto-updates"
git push origin v1.1.0
```

---

## Appendix: Reference Documentation

- **Update Server Docs**: `C:\WorkSpace\Go2Hell\src\github.com\LiteHomeLab\update-server\docs\UPDATE_CLIENT_GUIDE.md`
- **Project Docs**: `CLAUDE.md`
- **External Setup**: `docs/EXTERNAL_SETUP.md`
