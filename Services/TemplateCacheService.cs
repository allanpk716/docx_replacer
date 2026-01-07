using System.Collections.Concurrent;
using DocuFiller.Configuration;
using DocuFiller.Models;
using DocuFiller.Services.Interfaces;
using DocuFiller.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DocuFiller.Services
{
    /// <summary>
    /// 模板缓存服务实现
    /// </summary>
    public class TemplateCacheService : ITemplateCacheService, IDisposable
    {
        private readonly ILogger<TemplateCacheService> _logger;
        private readonly IOptionsMonitor<PerformanceSettings> _performanceSettings;
        private readonly Timer? _cleanupTimer;
        private readonly ConcurrentDictionary<string, CacheItem<ValidationResult>> _validationCache;
        private readonly ConcurrentDictionary<string, CacheItem<List<ContentControlData>>> _contentControlsCache;
        private bool _disposed = false;

        public TemplateCacheService(
            ILogger<TemplateCacheService> logger,
            IOptionsMonitor<PerformanceSettings> performanceSettings)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _performanceSettings = performanceSettings ?? throw new ArgumentNullException(nameof(performanceSettings));

            _validationCache = new ConcurrentDictionary<string, CacheItem<ValidationResult>>();
            _contentControlsCache = new ConcurrentDictionary<string, CacheItem<List<ContentControlData>>>();

            // 设置定期清理过期缓存的定时器
            var cleanupInterval = TimeSpan.FromMinutes(10); // 每10分钟清理一次
            _cleanupTimer = new Timer(CleanupExpiredItems, null, cleanupInterval, cleanupInterval);

            _logger.LogInformation("模板缓存服务已初始化");
        }

        public ValidationResult? GetCachedValidationResult(string templatePath)
        {
            ThrowIfDisposed();

            if (!_performanceSettings.CurrentValue.EnableTemplateCache)
            {
                _logger.LogDebug("模板缓存已禁用");
                return null;
            }

            if (string.IsNullOrWhiteSpace(templatePath))
                return null;

            try
            {
                if (_validationCache.TryGetValue(templatePath, out var cacheItem))
                {
                    if (cacheItem.IsExpired(_performanceSettings.CurrentValue.CacheExpirationMinutes))
                    {
                        _validationCache.TryRemove(templatePath, out _);
                        _logger.LogDebug($"模板验证缓存已过期: {templatePath}");
                        return null;
                    }

                    _logger.LogDebug($"从缓存获取模板验证结果: {templatePath}");
                    return cacheItem.Value;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取模板验证缓存时发生异常: {templatePath}");
            }

            return null;
        }

        public void CacheValidationResult(string templatePath, ValidationResult result)
        {
            ThrowIfDisposed();

            if (!_performanceSettings.CurrentValue.EnableTemplateCache)
            {
                _logger.LogDebug("模板缓存已禁用，跳过缓存验证结果");
                return;
            }

            if (string.IsNullOrWhiteSpace(templatePath) || result == null)
                return;

            try
            {
                var cacheItem = new CacheItem<ValidationResult>(result);
                _validationCache.AddOrUpdate(templatePath, cacheItem, (key, oldValue) => cacheItem);
                _logger.LogDebug($"缓存模板验证结果: {templatePath}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"缓存模板验证结果时发生异常: {templatePath}");
            }
        }

        public List<ContentControlData>? GetCachedContentControls(string templatePath)
        {
            ThrowIfDisposed();

            if (!_performanceSettings.CurrentValue.EnableTemplateCache)
            {
                _logger.LogDebug("模板缓存已禁用");
                return null;
            }

            if (string.IsNullOrWhiteSpace(templatePath))
                return null;

            try
            {
                if (_contentControlsCache.TryGetValue(templatePath, out var cacheItem))
                {
                    if (cacheItem.IsExpired(_performanceSettings.CurrentValue.CacheExpirationMinutes))
                    {
                        _contentControlsCache.TryRemove(templatePath, out _);
                        _logger.LogDebug($"模板内容控件缓存已过期: {templatePath}");
                        return null;
                    }

                    _logger.LogDebug($"从缓存获取模板内容控件: {templatePath}");
                    return cacheItem.Value;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取模板内容控件缓存时发生异常: {templatePath}");
            }

            return null;
        }

        public void CacheContentControls(string templatePath, List<ContentControlData> controls)
        {
            ThrowIfDisposed();

            if (!_performanceSettings.CurrentValue.EnableTemplateCache)
            {
                _logger.LogDebug("模板缓存已禁用，跳过缓存内容控件");
                return;
            }

            if (string.IsNullOrWhiteSpace(templatePath) || controls == null)
                return;

            try
            {
                var cacheItem = new CacheItem<List<ContentControlData>>(controls);
                _contentControlsCache.AddOrUpdate(templatePath, cacheItem, (key, oldValue) => cacheItem);
                _logger.LogDebug($"缓存模板内容控件: {templatePath}, 控件数量: {controls.Count}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"缓存模板内容控件时发生异常: {templatePath}");
            }
        }

        public void InvalidateCache(string templatePath)
        {
            ThrowIfDisposed();

            if (string.IsNullOrWhiteSpace(templatePath))
                return;

            try
            {
                var removedValidation = _validationCache.TryRemove(templatePath, out _);
                var removedControls = _contentControlsCache.TryRemove(templatePath, out _);

                if (removedValidation || removedControls)
                {
                    _logger.LogDebug($"清除模板缓存: {templatePath}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"清除模板缓存时发生异常: {templatePath}");
            }
        }

        public void ClearAllCache()
        {
            ThrowIfDisposed();

            try
            {
                var validationCount = _validationCache.Count;
                var controlsCount = _contentControlsCache.Count;

                _validationCache.Clear();
                _contentControlsCache.Clear();

                _logger.LogInformation($"已清除所有缓存 - 验证结果: {validationCount}, 内容控件: {controlsCount}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清除所有缓存时发生异常");
            }
        }

        public void ClearExpiredCache()
        {
            ThrowIfDisposed();

            try
            {
                var expirationMinutes = _performanceSettings.CurrentValue.CacheExpirationMinutes;
                var expiredValidationKeys = new List<string>();
                var expiredControlsKeys = new List<string>();

                // 检查过期的验证结果缓存
                foreach (var kvp in _validationCache)
                {
                    if (kvp.Value.IsExpired(expirationMinutes))
                    {
                        expiredValidationKeys.Add(kvp.Key);
                    }
                }

                // 检查过期的内容控件缓存
                foreach (var kvp in _contentControlsCache)
                {
                    if (kvp.Value.IsExpired(expirationMinutes))
                    {
                        expiredControlsKeys.Add(kvp.Key);
                    }
                }

                // 移除过期项
                foreach (var key in expiredValidationKeys)
                {
                    _validationCache.TryRemove(key, out _);
                }

                foreach (var key in expiredControlsKeys)
                {
                    _contentControlsCache.TryRemove(key, out _);
                }

                if (expiredValidationKeys.Count > 0 || expiredControlsKeys.Count > 0)
                {
                    _logger.LogInformation($"已清除过期缓存 - 验证结果: {expiredValidationKeys.Count}, 内容控件: {expiredControlsKeys.Count}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清除过期缓存时发生异常");
            }
        }

        private void CleanupExpiredItems(object? state)
        {
            try
            {
                ClearExpiredCache();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "定期清理过期缓存时发生异常");
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(TemplateCacheService));
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _cleanupTimer?.Dispose();
                    ClearAllCache();
                }

                _disposed = true;
            }
        }

        /// <summary>
        /// 缓存项
        /// </summary>
        private class CacheItem<T>
        {
            public T Value { get; }
            public DateTime CreatedAt { get; }

            public CacheItem(T value)
            {
                Value = value;
                CreatedAt = DateTime.UtcNow;
            }

            public bool IsExpired(int expirationMinutes)
            {
                return DateTime.UtcNow.Subtract(CreatedAt).TotalMinutes > expirationMinutes;
            }
        }
    }
}