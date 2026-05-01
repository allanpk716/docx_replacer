using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using DocuFiller.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Velopack;
using Velopack.Sources;

namespace DocuFiller.Services
{
    /// <summary>
    /// 更新服务实现，封装 Velopack UpdateManager 的检查、下载、应用更新功能
    /// </summary>
    public class UpdateService : IUpdateService
    {
        private readonly ILogger<UpdateService> _logger;
        private IUpdateSource _updateSource;
        private string _updateUrl;
        private string _channel;
        private string _sourceType;
        private readonly bool _isInstalled;

        /// <summary>
        /// appsettings.json 文件路径，用于测试时替换为临时文件路径
        /// </summary>
        internal string AppSettingsPath { get; set; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

        /// <summary>
        /// 持久化配置文件路径，位于安装目录上一级（Velopack 更新时不覆盖）。
        /// Velopack 安装结构: AppData\Local\DocuFiller\current\ ← 更新时替换
        ///                     AppData\Local\DocuFiller\update-config.json ← 更新时保留
        /// </summary>
        internal string? PersistentConfigPath { get; set; }

        public UpdateService(ILogger<UpdateService> logger, IConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // 初始化持久化配置路径：安装目录上一级
            PersistentConfigPath = GetPersistentConfigPath();

            // 优先读取持久化配置（更新后保留），fallback 到 appsettings.json
            var (persistedUrl, persistedChannel) = ReadPersistentConfig();
            var url = !string.IsNullOrWhiteSpace(persistedUrl) ? persistedUrl : (configuration?["Update:UpdateUrl"] ?? "");
            var rawUrl = string.IsNullOrWhiteSpace(url) ? "" : url;

            // Channel: 持久化配置 > appsettings.json > 默认 "stable"
            var channel = !string.IsNullOrWhiteSpace(persistedChannel) ? persistedChannel : (configuration?["Update:Channel"] ?? "");
            _channel = string.IsNullOrWhiteSpace(channel) ? "stable" : channel.Trim();

            if (!string.IsNullOrWhiteSpace(rawUrl))
            {
                // HTTP URL 模式：内网 Go 服务器
                _updateUrl = rawUrl.TrimEnd('/') + "/" + _channel + "/";
                _updateSource = new SimpleWebSource(_updateUrl);
                _sourceType = "HTTP";
            }
            else
            {
                // GitHub Releases 模式：外网用户备选
                _updateUrl = "";
                _updateSource = new GithubSource("https://github.com/allanpk716/docx_replacer", accessToken: null, prerelease: false);
                _sourceType = "GitHub";
            }

            // 检测 IsInstalled 状态（便携版/开发环境返回 false）
            try
            {
                var tempManager = CreateUpdateManager();
                _isInstalled = tempManager.IsInstalled;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "检测安装状态失败，默认为未安装");
                _isInstalled = false;
            }

            _logger.LogInformation("更新服务初始化，源类型: {SourceType}，通道: {Channel}，更新源: {UpdateUrl}，IsInstalled: {IsInstalled}，持久化配置: {ConfigPath}",
                _sourceType, _channel, _updateUrl != "" ? _updateUrl : "GitHub Releases", _isInstalled,
                PersistentConfigPath != null ? PersistentConfigPath : "未启用");
        }

        /// <summary>
        /// 获取持久化配置文件路径。
        /// Velopack 安装时，应用目录结构为: root\current\，上一级 root\ 就是安装根目录，更新时不覆盖。
        /// 非安装环境（开发/便携版）返回 null。
        /// </summary>
        private string? GetPersistentConfigPath()
        {
            try
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                if (baseDir == null) return null;

                // Velopack 安装的应用运行在 root\current\ 下，上一级就是安装根目录
                var parentDir = Directory.GetParent(baseDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                if (parentDir == null) return null;

                // 验证确实是 Velopack 安装结构（上一级有 Update.exe）
                var updateExe = Path.Combine(parentDir.FullName, "Update.exe");
                if (!File.Exists(updateExe)) return null;

                return Path.Combine(parentDir.FullName, "update-config.json");
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 从持久化配置文件读取 UpdateUrl 和 Channel。
        /// 文件格式: {"UpdateUrl":"http://...","Channel":"stable"}
        /// </summary>
        private (string? updateUrl, string? channel) ReadPersistentConfig()
        {
            try
            {
                if (PersistentConfigPath == null || !File.Exists(PersistentConfigPath))
                    return (null, null);

                var json = File.ReadAllText(PersistentConfigPath);
                var node = JsonNode.Parse(json);
                if (node == null) return (null, null);

                var url = node["UpdateUrl"]?.GetValue<string>();
                var channel = node["Channel"]?.GetValue<string>();
                return (url, channel);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "读取持久化配置文件失败，使用 appsettings.json 配置");
                return (null, null);
            }
        }

        /// <inheritdoc />
        public bool IsUpdateUrlConfigured => true; // GitHub Releases 始终可用作备选

        /// <inheritdoc />
        public string Channel => _channel;

        /// <inheritdoc />
        public bool IsInstalled => _isInstalled;

        /// <inheritdoc />
        public string UpdateSourceType => _sourceType;

        /// <inheritdoc />
        public string EffectiveUpdateUrl => _updateUrl;

        /// <inheritdoc />
        public async Task<UpdateInfo?> CheckForUpdatesAsync()
        {
            _logger.LogInformation("开始检查更新，更新源: {UpdateUrl}", _updateUrl);

            var updateManager = CreateUpdateManager();
            var updateInfo = await updateManager.CheckForUpdatesAsync();

            if (updateInfo != null)
            {
                _logger.LogInformation("发现新版本: {Version}", updateInfo.TargetFullRelease.Version);
            }
            else
            {
                _logger.LogInformation("当前已是最新版本");
            }

            return updateInfo;
        }

        /// <inheritdoc />
        public async Task DownloadUpdatesAsync(UpdateInfo updateInfo, Action<int>? progressCallback = null, CancellationToken cancellationToken = default)
        {
            if (updateInfo == null) throw new ArgumentNullException(nameof(updateInfo));

            _logger.LogInformation("开始下载更新: {Version}", updateInfo.TargetFullRelease.Version);

            var updateManager = CreateUpdateManager();
            await updateManager.DownloadUpdatesAsync(updateInfo, progressCallback, cancellationToken);

            _logger.LogInformation("更新下载完成");
        }

        /// <inheritdoc />
        public void ApplyUpdatesAndRestart()
        {
            _logger.LogInformation("开始应用更新并重启应用");

            var updateManager = CreateUpdateManager();

            // 优先使用已下载待应用的更新包，若不存在则使用 UpdateInfo 中的目标版本
            var pendingAsset = updateManager.UpdatePendingRestart;
            updateManager.ApplyUpdatesAndRestart(pendingAsset);
        }

        /// <inheritdoc />
        public void ReloadSource(string updateUrl, string channel)
        {
            updateUrl ??= "";
            channel = string.IsNullOrWhiteSpace(channel) ? "stable" : channel.Trim();

            var oldSourceType = _sourceType;
            var oldUpdateUrl = _updateUrl;
            var oldChannel = _channel;

            _logger.LogInformation("热重载更新源：源类型 {OldSourceType} → {NewSourceType}，通道 {OldChannel} → {NewChannel}，URL {OldUrl} → {NewUrl}",
                oldSourceType, string.IsNullOrWhiteSpace(updateUrl) ? "GitHub" : "HTTP",
                oldChannel, channel,
                oldUpdateUrl != "" ? oldUpdateUrl : "GitHub Releases",
                string.IsNullOrWhiteSpace(updateUrl) ? "GitHub Releases" : updateUrl);

            if (!string.IsNullOrWhiteSpace(updateUrl))
            {
                _updateUrl = updateUrl.TrimEnd('/') + "/" + channel + "/";
                _updateSource = new SimpleWebSource(_updateUrl);
                _sourceType = "HTTP";
            }
            else
            {
                _updateUrl = "";
                _updateSource = new GithubSource("https://github.com/allanpk716/docx_replacer", accessToken: null, prerelease: false);
                _sourceType = "GitHub";
            }

            _channel = channel;

            _logger.LogInformation("更新源热重载完成，源类型: {SourceType}，通道: {Channel}，更新源: {UpdateUrl}",
                _sourceType, _channel, _updateUrl != "" ? _updateUrl : "GitHub Releases");

            PersistToAppSettings(updateUrl, _channel);
        }

        /// <summary>
        /// 将更新源配置持久化到 appsettings.json 文件（安装目录内）和持久化配置文件（安装目录上一级）。
        /// 持久化配置文件在 Velopack 更新时不会被覆盖，确保更新后配置不丢失。
        /// </summary>
        private void PersistToAppSettings(string updateUrl, string channel)
        {
            // 1. 写入持久化配置文件（安装目录上一级，更新时不覆盖）
            if (PersistentConfigPath != null)
            {
                try
                {
                    var config = new JsonObject
                    {
                        ["UpdateUrl"] = updateUrl ?? "",
                        ["Channel"] = channel ?? "stable"
                    };
                    var options = new JsonSerializerOptions { WriteIndented = true };
                    File.WriteAllText(PersistentConfigPath, config.ToJsonString(options));
                    _logger.LogInformation("已将更新源配置持久化到: {Path}", PersistentConfigPath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "持久化更新源配置到 {Path} 失败", PersistentConfigPath);
                }
            }

            // 2. 同步写入 appsettings.json（安装目录内，供 UpdateSettingsViewModel 读取）
            try
            {
                var path = AppSettingsPath;
                if (!File.Exists(path))
                {
                    _logger.LogWarning("appsettings.json 文件不存在，跳过持久化: {Path}", path);
                    return;
                }
                var json = File.ReadAllText(path);
                var node = JsonNode.Parse(json) ?? throw new InvalidOperationException("Failed to parse appsettings.json");
                if (node["Update"] == null)
                    node["Update"] = new JsonObject();
                node["Update"]!["UpdateUrl"] = updateUrl;
                node["Update"]!["Channel"] = channel;
                var options = new JsonSerializerOptions { WriteIndented = true };
                File.WriteAllText(path, node.ToJsonString(options));
                _logger.LogInformation("已将更新源配置同步到 appsettings.json");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "持久化更新源配置到 appsettings.json 失败，内存热重载已生效");
            }
        }

        private UpdateManager CreateUpdateManager()
        {
            return new UpdateManager(_updateSource, new UpdateOptions { ExplicitChannel = _channel });
        }
    }
}
