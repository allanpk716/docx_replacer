using System;
using Microsoft.Extensions.Logging;
using DocuFiller.Models;
using DocuFiller.Services.Interfaces;

namespace DocuFiller.Services
{
    /// <summary>
    /// 进度报告服务实现
    /// </summary>
    public class ProgressReporterService : IProgressReporter
    {
        private readonly ILogger<ProgressReporterService> _logger;
        private int _currentIndex;
        private int _totalItems;
        private bool _isCompleted;
        private bool _hasError;
        private string _errorMessage;
        private string _currentMessage;

        public event EventHandler<ProgressEventArgs> ProgressUpdated;

        public ProgressReporterService(ILogger<ProgressReporterService> logger)
        {
            _logger = logger;
            Reset();
        }

        public void ReportProgress(int currentIndex, int totalCount, string statusMessage = "", string currentFileName = "")
        {
            try
            {
                _currentIndex = Math.Max(0, currentIndex);
                _totalItems = Math.Max(1, totalCount);
                _currentMessage = statusMessage ?? string.Empty;

                var progressArgs = new ProgressEventArgs(
                    _currentIndex,
                    _totalItems,
                    _currentMessage
                );

                _logger.LogDebug($"进度更新: {progressArgs.ProgressPercentage}% ({_currentIndex}/{_totalItems}) - {_currentMessage}");
                ProgressUpdated?.Invoke(this, progressArgs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "报告进度时发生异常");
            }
        }

        public void ReportCompleted(int totalCount, string message = "处理完成")
        {
            try
            {
                _isCompleted = true;
                _totalItems = Math.Max(1, totalCount);
                _currentIndex = _totalItems;
                _currentMessage = message ?? "处理完成";

                var progressArgs = ProgressEventArgs.CreateCompleted(_totalItems, _currentMessage);
                
                _logger.LogInformation($"处理完成: {_currentMessage}");
                ProgressUpdated?.Invoke(this, progressArgs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "报告完成状态时发生异常");
            }
        }

        public void ReportError(int currentIndex, int totalCount, string errorMessage)
        {
            try
            {
                _hasError = true;
                _currentIndex = Math.Max(0, currentIndex);
                _totalItems = Math.Max(1, totalCount);
                _errorMessage = errorMessage ?? "发生未知错误";

                var progressArgs = ProgressEventArgs.CreateError(_currentIndex, _totalItems, _errorMessage);
                
                _logger.LogError($"处理过程中发生错误: {_errorMessage}");

                ProgressUpdated?.Invoke(this, progressArgs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "报告错误状态时发生异常");
            }
        }

        public void Reset()
        {
            try
            {
                _currentIndex = 0;
                _totalItems = 1;
                _isCompleted = false;
                _hasError = false;
                _errorMessage = string.Empty;
                _currentMessage = string.Empty;

                _logger.LogDebug("进度报告器已重置");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重置进度报告器时发生异常");
            }
        }

        public int GetCurrentProgress()
        {
            try
            {
                if (_totalItems <= 0)
                {
                    return 0;
                }

                if (_isCompleted)
                {
                    return 100;
                }

                var percentage = (int)Math.Round((double)_currentIndex / _totalItems * 100);
                return Math.Min(100, Math.Max(0, percentage));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "计算进度百分比时发生异常");
                return 0;
            }
        }

        public bool IsCompleted()
        {
            return _isCompleted;
        }

        public bool HasError()
        {
            return _hasError;
        }

        public string GetCurrentMessage()
        {
            return _currentMessage ?? string.Empty;
        }

        public string GetErrorMessage()
        {
            return _errorMessage ?? string.Empty;
        }

        public int GetCurrentIndex()
        {
            return _currentIndex;
        }

        public int GetTotalItems()
        {
            return _totalItems;
        }

        public void SetTotalItems(int totalItems)
        {
            try
            {
                _totalItems = Math.Max(1, totalItems);
                _logger.LogDebug($"设置总项目数: {_totalItems}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "设置总项目数时发生异常");
            }
        }

        public void IncrementProgress(string message = null)
        {
            try
            {
                ReportProgress(_currentIndex + 1, _totalItems, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "递增进度时发生异常");
            }
        }

        public ProgressEventArgs GetCurrentStatus()
        {
            try
            {
                if (_hasError)
                {
                    return ProgressEventArgs.CreateError(_currentIndex, _totalItems, _errorMessage);
                }

                if (_isCompleted)
                {
                    return ProgressEventArgs.CreateCompleted(_totalItems, _currentMessage);
                }

                return new ProgressEventArgs(
                    _currentIndex,
                    _totalItems,
                    _currentMessage
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取当前状态时发生异常");
                return new ProgressEventArgs(0, 1, "获取状态失败");
            }
        }
    }
}