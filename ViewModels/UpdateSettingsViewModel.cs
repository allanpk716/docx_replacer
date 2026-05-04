using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocuFiller.Services;
using DocuFiller.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DocuFiller.ViewModels
{
    /// <summary>
    /// 更新源设置 ViewModel，用于编辑 UpdateUrl 和 Channel
    /// </summary>
    public partial class UpdateSettingsViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
    {
        private readonly IUpdateService _updateService;
        private readonly ILogger<UpdateSettingsViewModel> _logger;

        [ObservableProperty] private string _updateUrl = string.Empty;
        [ObservableProperty] private string _channel = "stable";

        private string _sourceTypeDisplay = string.Empty;

        /// <summary>
        /// 窗体关闭回调，由 code-behind 注入，参数为 DialogResult
        /// </summary>
        internal Action<bool?>? CloseCallback { get; set; }

        /// <summary>
        /// 可选的持久化配置读取委托，用于测试时绕过真实文件系统。
        /// 返回 (UpdateUrl, Channel) 元组，null 值表示未配置。
        /// </summary>
        private readonly Func<(string? updateUrl, string? channel)>? _readPersistentConfig;

        public UpdateSettingsViewModel(
            IUpdateService updateService,
            ILogger<UpdateSettingsViewModel> logger,
            IConfiguration configuration,
            Func<(string? updateUrl, string? channel)>? readPersistentConfig = null)
        {
            _updateService = updateService ?? throw new ArgumentNullException(nameof(updateService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _readPersistentConfig = readPersistentConfig;

            _sourceTypeDisplay = _updateService.UpdateSourceType;

            // 优先从持久化配置文件（update-config.json）读取，fallback 到 IConfiguration。
            // Velopack 更新会覆盖安装目录下的 appsettings.json（URL 重置为空），
            // 但安装目录上一级的 update-config.json 不受影响。
            var (persistedUrl, persistedChannel) = _readPersistentConfig != null
                ? _readPersistentConfig()
                : ReadPersistentConfig();
            var rawUrl = !string.IsNullOrWhiteSpace(persistedUrl)
                ? persistedUrl
                : (configuration?["Update:UpdateUrl"] ?? "");
            _updateUrl = string.IsNullOrWhiteSpace(rawUrl) ? string.Empty : rawUrl.Trim();

            var rawChannel = !string.IsNullOrWhiteSpace(persistedChannel)
                ? persistedChannel
                : (configuration?["Update:Channel"] ?? "");
            _channel = string.IsNullOrWhiteSpace(rawChannel)
                ? _updateService.Channel
                : rawChannel.Trim();
        }

        /// <summary>当前源类型（只读显示）</summary>
        public string SourceTypeDisplay => _sourceTypeDisplay;

        /// <summary>通道选项列表</summary>
        public ObservableCollection<string> Channels { get; } = new ObservableCollection<string> { "stable", "beta" };

        [RelayCommand]
        private void Save()
        {
            try
            {
                _updateService.ReloadSource(UpdateUrl, Channel);
                _logger.LogInformation("更新设置已保存：UpdateUrl={UpdateUrl}, Channel={Channel}", UpdateUrl, Channel);
                CloseCallback?.Invoke(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存更新设置失败");
                MessageBox.Show($"保存设置失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            CloseCallback?.Invoke(false);
        }

        /// <summary>
        /// 从持久化配置文件（%USERPROFILE%\.docx_replacer\update-config.json）读取 UpdateUrl 和 Channel。
        /// 完全独立于 Velopack 安装目录，因此是配置的"真实来源"。
        /// </summary>
        private static (string? updateUrl, string? channel) ReadPersistentConfig()
        {
            try
            {
                var configPath = UpdateService.GetPersistentConfigPath();
                if (!File.Exists(configPath)) return (null, null);

                var json = File.ReadAllText(configPath);
                var node = JsonNode.Parse(json);
                if (node == null) return (null, null);

                return (node["UpdateUrl"]?.GetValue<string>(), node["Channel"]?.GetValue<string>());
            }
            catch
            {
                return (null, null);
            }
        }
    }
}
