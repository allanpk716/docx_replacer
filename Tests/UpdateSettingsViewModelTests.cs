using DocuFiller.Services.Interfaces;
using DocuFiller.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DocuFiller.Tests
{
    /// <summary>
    /// UpdateSettingsViewModel 单元测试
    /// 验证从 IConfiguration 直接读取 URL 和 Channel 原始值的逻辑
    /// </summary>
    public class UpdateSettingsViewModelTests
    {
        private readonly Mock<IUpdateService> _mockUpdateService;
        private readonly ILogger<UpdateSettingsViewModel> _logger;

        public UpdateSettingsViewModelTests()
        {
            _mockUpdateService = new Mock<IUpdateService>();
            _mockUpdateService.Setup(s => s.Channel).Returns("stable");
            _mockUpdateService.Setup(s => s.UpdateSourceType).Returns("HTTP");
            _mockUpdateService.Setup(s => s.EffectiveUpdateUrl).Returns("http://<INTERNAL_SERVER_IP>:30001/stable/");

            using var loggerFactory = LoggerFactory.Create(builder => { });
            _logger = new Logger<UpdateSettingsViewModel>(loggerFactory);
        }

        /// <summary>
        /// 构建内存中的 IConfiguration，模拟 appsettings.json 的 Update 节
        /// </summary>
        private static IConfiguration BuildConfiguration(string? updateUrl = null, string? channel = null)
        {
            var dict = new Dictionary<string, string?>
            {
                { "Update:UpdateUrl", updateUrl },
                { "Update:Channel", channel }
            };
            return new ConfigurationBuilder()
                .AddInMemoryCollection(dict)
                .Build();
        }

        [Fact]
        public void Constructor_HttpUrl_ReturnsRawUrlFromConfig()
        {
            // Arrange
            var config = BuildConfiguration(updateUrl: "http://<INTERNAL_SERVER_IP>:30001", channel: "stable");

            // Act
            var vm = new UpdateSettingsViewModel(_mockUpdateService.Object, _logger, config);

            // Assert
            Assert.Equal("http://<INTERNAL_SERVER_IP>:30001", vm.UpdateUrl);
        }

        [Fact]
        public void Constructor_GitHubMode_EmptyUrl_ReturnsEmptyString()
        {
            // Arrange
            _mockUpdateService.Setup(s => s.UpdateSourceType).Returns("GitHub");
            var config = BuildConfiguration(updateUrl: "", channel: null);

            // Act
            var vm = new UpdateSettingsViewModel(_mockUpdateService.Object, _logger, config);

            // Assert
            Assert.Equal(string.Empty, vm.UpdateUrl);
        }

        [Fact]
        public void Constructor_GitHubMode_NullUrl_ReturnsEmptyString()
        {
            // Arrange
            _mockUpdateService.Setup(s => s.UpdateSourceType).Returns("GitHub");
            var config = BuildConfiguration(updateUrl: null, channel: null);

            // Act
            var vm = new UpdateSettingsViewModel(_mockUpdateService.Object, _logger, config);

            // Assert
            Assert.Equal(string.Empty, vm.UpdateUrl);
        }

        [Fact]
        public void Constructor_ChannelFromConfig_ReturnsConfigValue()
        {
            // Arrange
            var config = BuildConfiguration(updateUrl: "http://example.com", channel: "beta");

            // Act
            var vm = new UpdateSettingsViewModel(_mockUpdateService.Object, _logger, config);

            // Assert
            Assert.Equal("beta", vm.Channel);
        }

        [Fact]
        public void Constructor_ChannelEmptyInConfig_FallsBackToServiceChannel()
        {
            // Arrange
            _mockUpdateService.Setup(s => s.Channel).Returns("stable");
            var config = BuildConfiguration(updateUrl: "http://example.com", channel: "");

            // Act
            var vm = new UpdateSettingsViewModel(_mockUpdateService.Object, _logger, config);

            // Assert
            Assert.Equal("stable", vm.Channel);
        }

        [Fact]
        public void Constructor_ChannelNullInConfig_FallsBackToServiceChannel()
        {
            // Arrange
            _mockUpdateService.Setup(s => s.Channel).Returns("stable");
            var config = BuildConfiguration(updateUrl: "http://example.com", channel: null);

            // Act
            var vm = new UpdateSettingsViewModel(_mockUpdateService.Object, _logger, config);

            // Assert
            Assert.Equal("stable", vm.Channel);
        }

        [Fact]
        public void Constructor_SourceTypeDisplay_ReturnsServiceSourceType()
        {
            // Arrange
            _mockUpdateService.Setup(s => s.UpdateSourceType).Returns("HTTP");
            var config = BuildConfiguration(updateUrl: "http://example.com", channel: "stable");

            // Act
            var vm = new UpdateSettingsViewModel(_mockUpdateService.Object, _logger, config);

            // Assert
            Assert.Equal("HTTP", vm.SourceTypeDisplay);
        }

        [Fact]
        public void Constructor_SourceTypeDisplay_GitHub()
        {
            // Arrange
            _mockUpdateService.Setup(s => s.UpdateSourceType).Returns("GitHub");
            var config = BuildConfiguration(updateUrl: "", channel: null);

            // Act
            var vm = new UpdateSettingsViewModel(_mockUpdateService.Object, _logger, config);

            // Assert
            Assert.Equal("GitHub", vm.SourceTypeDisplay);
        }

        [Fact]
        public void Constructor_UrlWithWhitespace_Trimmed()
        {
            // Arrange
            var config = BuildConfiguration(updateUrl: "  http://example.com  ", channel: "  beta  ");

            // Act
            var vm = new UpdateSettingsViewModel(_mockUpdateService.Object, _logger, config);

            // Assert
            Assert.Equal("http://example.com", vm.UpdateUrl);
            Assert.Equal("beta", vm.Channel);
        }

        [Fact]
        public void Constructor_Channels_ContainsStableAndBeta()
        {
            // Arrange
            var config = BuildConfiguration(updateUrl: "http://example.com", channel: "stable");

            // Act
            var vm = new UpdateSettingsViewModel(_mockUpdateService.Object, _logger, config);

            // Assert
            Assert.Equal(2, vm.Channels.Count);
            Assert.Contains("stable", vm.Channels);
            Assert.Contains("beta", vm.Channels);
        }

        [Fact]
        public void Constructor_NullConfiguration_DoesNotThrow()
        {
            // Act & Assert — null IConfiguration should not throw, just use defaults
            var vm = new UpdateSettingsViewModel(_mockUpdateService.Object, _logger, null!);

            Assert.Equal(string.Empty, vm.UpdateUrl);
            Assert.Equal("stable", vm.Channel); // falls back to service channel
        }
    }
}
