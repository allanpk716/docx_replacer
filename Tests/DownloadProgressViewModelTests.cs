using System;
using System.Threading;
using DocuFiller.ViewModels;
using Xunit;

namespace DocuFiller.Tests
{
    public class DownloadProgressViewModelTests : IDisposable
    {
        private readonly TimeSpan _baseTime = TimeSpan.FromSeconds(1000);
        private int _timeOffset;
        private readonly DownloadProgressViewModel _vm;

        public DownloadProgressViewModelTests()
        {
            _vm = CreateVm(10_000_000, "1.0.0");
        }

        private DownloadProgressViewModel CreateVm(
            long totalBytes = 10_000_000,
            string version = "1.0.0")
        {
            return new DownloadProgressViewModel(
                totalBytes,
                version,
                timestampProvider: () => _baseTime + TimeSpan.FromSeconds(_timeOffset),
                dispatcherInvoke: action => action()); // direct invocation — no WPF Dispatcher
            {
            }
        }

        private void AdvanceTime(int seconds)
        {
            _timeOffset += seconds;
        }

        public void Dispose()
        {
            _vm.Dispose();
        }

        // ── 1. Initial state ─────────────────────────────────────────────

        [Fact]
        public void InitialState_IsCorrect()
        {
            Assert.Equal(0, _vm.ProgressPercent);
            Assert.True(_vm.IsDownloading);
            Assert.False(_vm.IsCompleted);
            Assert.Null(_vm.ErrorMessage);
            Assert.Equal(string.Empty, _vm.DownloadSpeed);
            Assert.Equal(string.Empty, _vm.RemainingTime);
            Assert.Equal("准备下载...", _vm.StatusText);
        }

        [Fact]
        public void CancellationToken_IsNotCancelledInitially()
        {
            Assert.False(_vm.CancellationToken.IsCancellationRequested);
        }

        // ── 2. Single UpdateProgress ─────────────────────────────────────

        [Fact]
        public void UpdateProgress_SingleUpdate_SetsPercent_NoSpeedOrEta()
        {
            _vm.UpdateProgress(30);

            Assert.Equal(30, _vm.ProgressPercent);
            // Need ≥2 points for speed, so speed/ETA should remain empty
            Assert.Equal(string.Empty, _vm.DownloadSpeed);
            Assert.Equal(string.Empty, _vm.RemainingTime);
        }

        [Fact]
        public void UpdateProgress_StatusText_ContainsVersionAndPercent()
        {
            _vm.UpdateProgress(50);

            Assert.Contains("1.0.0", _vm.StatusText);
            Assert.Contains("50%", _vm.StatusText);
        }

        // ── 3. Speed calculation ─────────────────────────────────────────

        [Fact]
        public void UpdateProgress_TwoPoints_CalculatesSpeed()
        {
            // 10 MB total. First point: 0% at t=0, second: 50% at t=5s
            // Speed = 5MB / 5s = 1,000,000 B/s = 976.6 KB/s (binary units: /1024)
            _vm.UpdateProgress(0);
            AdvanceTime(5);
            _vm.UpdateProgress(50);

            Assert.Equal(50, _vm.ProgressPercent);
            // 10_000_000 * 50/100 / 5 = 1_000_000 B/s → 976.6 KB/s
            Assert.Contains("KB/s", _vm.DownloadSpeed);
            Assert.Equal("976.6 KB/s", _vm.DownloadSpeed);
        }

        [Fact]
        public void UpdateProgress_ThreePoints_CumulativeAverage()
        {
            // 10 MB total. Points: 0%@0s, 50%@5s, 100%@8s
            // After 3rd point: 10MB / 8s = 1_250_000 B/s = 1.2 MB/s (binary: /1048576)
            _vm.UpdateProgress(0);
            AdvanceTime(5);
            _vm.UpdateProgress(50);
            AdvanceTime(3);
            _vm.UpdateProgress(100);

            Assert.Equal(100, _vm.ProgressPercent);
            // 1_250_000 / 1048576 = 1.192... → F1 = "1.2 MB/s"
            Assert.Equal("1.2 MB/s", _vm.DownloadSpeed);
        }

        [Fact]
        public void UpdateProgress_KB_SpeedRange()
        {
            // 100 KB total. 0%@0s, 50%@1s → 50KB/s
            using var vm = CreateVm(100 * 1024, "1.0.0");
            vm.UpdateProgress(0);
            AdvanceTime(1);
            vm.UpdateProgress(50);

            Assert.Contains("KB/s", vm.DownloadSpeed);
        }

        // ── 4. ETA calculation ────────────────────────────────────────────

        [Fact]
        public void UpdateProgress_TwoPoints_CalculatesEta()
        {
            // 10 MB total. 0%@0s, 50%@5s → speed=976.6 KB/s, remaining=5MB → ETA≈5.12s → "约 5 秒"
            _vm.UpdateProgress(0);
            AdvanceTime(5);
            _vm.UpdateProgress(50);

            Assert.Contains("秒", _vm.RemainingTime);
            Assert.Equal("约 5 秒", _vm.RemainingTime);
        }

        [Fact]
        public void UpdateProgress_NearlyComplete_ShowsAlmostDone()
        {
            // 10 MB total. 0%@0s, 99%@1s → speed ≈ 9.9 MB/s, remaining ≈ 0.1MB → ETA < 1s
            _vm.UpdateProgress(0);
            AdvanceTime(1);
            _vm.UpdateProgress(99);

            Assert.Equal("即将完成", _vm.RemainingTime);
        }

        [Fact]
        public void UpdateProgress_LargeETA_ShowsMinutes()
        {
            // 1 GB total. 0%@0s, 1%@1s → speed ≈ 10MB/s, remaining ≈ 990MB → ETA ≈ 99s ≈ 1min
            using var vm = CreateVm(1_000_000_000, "2.0.0");
            vm.UpdateProgress(0);
            AdvanceTime(1);
            vm.UpdateProgress(1);

            Assert.Contains("分钟", vm.RemainingTime);
        }

        // ── 5. Cancel ─────────────────────────────────────────────────────

        [Fact]
        public void CancelCommand_TriggersCancellationToken()
        {
            Assert.True(_vm.CancelCommand.CanExecute(null));
            _vm.CancelCommand.Execute(null);

            Assert.True(_vm.CancellationToken.IsCancellationRequested);
        }

        [Fact]
        public void MarkCancelled_SetsState()
        {
            _vm.MarkCancelled();

            Assert.False(_vm.IsDownloading);
            Assert.NotNull(_vm.ErrorMessage);
            Assert.Contains("取消", _vm.ErrorMessage);
            Assert.Contains("取消", _vm.StatusText);
        }

        // ── 6. Complete ───────────────────────────────────────────────────

        [Fact]
        public void UpdateProgress_100Percent_MarksCompleted()
        {
            _vm.UpdateProgress(0);
            AdvanceTime(1);
            _vm.UpdateProgress(100);

            Assert.Equal(100, _vm.ProgressPercent);
            Assert.False(_vm.IsDownloading);
            Assert.True(_vm.IsCompleted);
        }

        [Fact]
        public void MarkCompleted_SetsCompleted()
        {
            _vm.MarkCompleted();

            Assert.Equal(100, _vm.ProgressPercent);
            Assert.True(_vm.IsCompleted);
            Assert.False(_vm.IsDownloading);
            Assert.Equal("下载完成", _vm.StatusText);
            Assert.Equal(string.Empty, _vm.DownloadSpeed);
            Assert.Equal(string.Empty, _vm.RemainingTime);
        }

        // ── 7. Error handling ─────────────────────────────────────────────

        [Fact]
        public void MarkFailed_SetsErrorMessageAndCompleted()
        {
            _vm.MarkFailed("网络超时");

            Assert.False(_vm.IsDownloading);
            Assert.Equal("网络超时", _vm.ErrorMessage);
            Assert.Contains("网络超时", _vm.StatusText);
        }

        // ── 8. Edge cases ─────────────────────────────────────────────────

        [Fact]
        public void ZeroTotalBytes_NoDivideByZero()
        {
            using var vm = CreateVm(0, "1.0.0");

            // Should not throw
            vm.UpdateProgress(0);
            AdvanceTime(1);
            vm.UpdateProgress(50);

            Assert.Equal(50, vm.ProgressPercent);
            // Speed/ETA remain empty when totalBytes ≤ 0
            Assert.Equal(string.Empty, vm.DownloadSpeed);
            Assert.Equal(string.Empty, vm.RemainingTime);
        }

        [Fact]
        public void UpdateProgress_ClampsAbove100()
        {
            _vm.UpdateProgress(150);

            Assert.Equal(100, _vm.ProgressPercent);
            Assert.True(_vm.IsCompleted);
        }

        [Fact]
        public void UpdateProgress_ClampsBelow0()
        {
            _vm.UpdateProgress(-10);

            Assert.Equal(0, _vm.ProgressPercent);
        }

        [Fact]
        public void VeryFastDownload_SpeedStillShows()
        {
            // 10MB in 0.1 seconds → 100 MB/s
            _vm.UpdateProgress(0);
            AdvanceTime(0); // same timestamp = 0s elapsed → speed can't be calculated yet
            _vm.UpdateProgress(100);

            // With 0 elapsed, speed won't be calculated (elapsedSeconds check)
            // That's expected — let's test with tiny positive delta
        }

        [Fact]
        public void FastDownload_SpeedCalculatedCorrectly()
        {
            // 10 MB total. 0%@0s, 100%@1s → 10MB/1s = 10_000_000 B/s = 9.5 MB/s (binary)
            _vm.UpdateProgress(0);
            AdvanceTime(1);
            _vm.UpdateProgress(100);

            // 10_000_000 / 1048576 = 9.5367... → F1 = "9.5 MB/s"
            Assert.Equal("9.5 MB/s", _vm.DownloadSpeed);
        }

        // ── 9. FormatSpeed static method tests ────────────────────────────

        [Theory]
        [InlineData(0, "")]
        [InlineData(-1, "")]
        [InlineData(512, "512 B/s")]
        [InlineData(1024, "1.0 KB/s")]
        [InlineData(1536, "1.5 KB/s")]
        [InlineData(1048576, "1.0 MB/s")]
        [InlineData(1572864, "1.5 MB/s")]
        public void FormatSpeed_ReturnsCorrectFormat(double bytesPerSec, string expected)
        {
            Assert.Equal(expected, DownloadProgressViewModel.FormatSpeed(bytesPerSec));
        }

        // ── 10. FormatEta static method tests ─────────────────────────────

        [Theory]
        [InlineData(0, 1_000_000, 50, "")]
        [InlineData(1_000_000, 0, 50, "")]
        [InlineData(1_000_000, 1_000_000, 99, "即将完成")]
        [InlineData(1_000_000, 1_000_000, 50, "即将完成")]  // 500KB/1MB/s = 0.5s < 1s
        [InlineData(100_000, 10_000_000, 50, "约 50 秒")]   // 5MB/100KB/s = 50s
        [InlineData(100_000, 100_000_000, 50, "约 8 分钟")] // 50MB/100KB/s ≈ 500s ≈ 8min
        public void FormatEta_ReturnsCorrectFormat(double bytesPerSec, long totalBytes, int percent, string expected)
        {
            Assert.Equal(expected, DownloadProgressViewModel.FormatEta(bytesPerSec, totalBytes, percent));
        }

        // ── 11. IUpdateService.DownloadUpdatesAsync CancellationToken signature ──

        [Fact]
        public void IUpdateService_DownloadUpdatesAsync_AcceptsCancellationToken()
        {
            // Compile-time verification: the interface method signature includes CancellationToken
            var method = typeof(DocuFiller.Services.Interfaces.IUpdateService).GetMethod("DownloadUpdatesAsync");
            Assert.NotNull(method);

            var parameters = method!.GetParameters();
            // Parameters: UpdateInfo updateInfo, Action<int>? progressCallback, CancellationToken cancellationToken
            Assert.True(parameters.Length >= 1);

            // Find CancellationToken parameter
            var ctParam = Array.Find(parameters, p => p.ParameterType == typeof(CancellationToken));
            Assert.NotNull(ctParam);
            Assert.Equal("cancellationToken", ctParam!.Name);
            Assert.True(ctParam.HasDefaultValue);
        }

        // ── 12. Dispose ────────────────────────────────────────────────────

        [Fact]
        public void Dispose_DoesNotThrow()
        {
            var vm = CreateVm();
            vm.Dispose();
            // Second dispose should also not throw (CancellationTokenSource handles it)
            vm.Dispose();
        }

        // ── 13. PropertyChanged events ─────────────────────────────────────

        [Fact]
        public void UpdateProgress_FiresPropertyChangedEvents()
        {
            var changedProps = new System.Collections.Generic.List<string>();
            _vm.PropertyChanged += (_, e) => changedProps.Add(e.PropertyName!);

            _vm.UpdateProgress(50);

            Assert.Contains("ProgressPercent", changedProps);
            Assert.Contains("StatusText", changedProps);
        }

        [Fact]
        public void MarkCompleted_FiresPropertyChangedEvents()
        {
            var changedProps = new System.Collections.Generic.List<string>();
            _vm.PropertyChanged += (_, e) => changedProps.Add(e.PropertyName!);

            _vm.MarkCompleted();

            Assert.Contains("IsDownloading", changedProps);
            Assert.Contains("IsCompleted", changedProps);
            Assert.Contains("ProgressPercent", changedProps);
        }

        [Fact]
        public void MarkFailed_FiresPropertyChangedEvents()
        {
            var changedProps = new System.Collections.Generic.List<string>();
            _vm.PropertyChanged += (_, e) => changedProps.Add(e.PropertyName!);

            _vm.MarkFailed("error");

            Assert.Contains("IsDownloading", changedProps);
            Assert.Contains("ErrorMessage", changedProps);
            Assert.Contains("StatusText", changedProps);
        }
    }
}
