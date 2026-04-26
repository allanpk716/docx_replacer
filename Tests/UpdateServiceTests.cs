using System.Collections.Generic;
using DocuFiller.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Xunit;

namespace DocuFiller.Tests
{
    public class UpdateServiceTests
    {
        private static IConfiguration BuildConfig(Dictionary<string, string?> data)
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(data)
                .Build();
        }

        private static ILogger<UpdateService> CreateLogger()
        {
            using var factory = LoggerFactory.Create(b => { });
            return factory.CreateLogger<UpdateService>();
        }

        [Fact]
        public void Channel_defaults_to_stable_when_empty()
        {
            var config = BuildConfig(new Dictionary<string, string?>
            {
                { "Update:UpdateUrl", "http://server/updates" },
                { "Update:Channel", "" }
            });
            var service = new UpdateService(CreateLogger(), config);

            Assert.Equal("stable", service.Channel);
            Assert.Contains("/stable/", service.EffectiveUpdateUrl);
        }

        [Fact]
        public void Channel_explicit_beta()
        {
            var config = BuildConfig(new Dictionary<string, string?>
            {
                { "Update:UpdateUrl", "http://server/updates" },
                { "Update:Channel", "beta" }
            });
            var service = new UpdateService(CreateLogger(), config);

            Assert.Equal("beta", service.Channel);
            Assert.Contains("/beta/", service.EffectiveUpdateUrl);
        }

        [Fact]
        public void Channel_missing_key()
        {
            // No Channel key in config at all — should default to stable
            var config = BuildConfig(new Dictionary<string, string?>
            {
                { "Update:UpdateUrl", "http://server/updates" }
            });
            var service = new UpdateService(CreateLogger(), config);

            Assert.Equal("stable", service.Channel);
            Assert.Contains("/stable/", service.EffectiveUpdateUrl);
        }

        [Fact]
        public void UpdateUrl_empty_uses_github_source()
        {
            // UpdateUrl 为空时，GitHub Releases 作为备选源始终可用
            var config = BuildConfig(new Dictionary<string, string?>
            {
                { "Update:UpdateUrl", "" },
                { "Update:Channel", "stable" }
            });
            var service = new UpdateService(CreateLogger(), config);

            Assert.True(service.IsUpdateUrlConfigured);
            Assert.Equal("GitHub", service.UpdateSourceType);
            Assert.Equal("", service.EffectiveUpdateUrl);
        }

        [Fact]
        public void UpdateUrl_with_trailing_slash()
        {
            var config = BuildConfig(new Dictionary<string, string?>
            {
                { "Update:UpdateUrl", "http://server/" },
                { "Update:Channel", "stable" }
            });
            var service = new UpdateService(CreateLogger(), config);

            // Should not produce double slashes
            Assert.Equal("http://server/stable/", service.EffectiveUpdateUrl);
            Assert.DoesNotContain("//", service.EffectiveUpdateUrl.Replace("http://", ""));
            Assert.Equal("HTTP", service.UpdateSourceType);
        }

        [Fact]
        public void UpdateUrl_without_trailing_slash()
        {
            var config = BuildConfig(new Dictionary<string, string?>
            {
                { "Update:UpdateUrl", "http://server" },
                { "Update:Channel", "stable" }
            });
            var service = new UpdateService(CreateLogger(), config);

            Assert.Equal("http://server/stable/", service.EffectiveUpdateUrl);
            Assert.Equal("HTTP", service.UpdateSourceType);
        }

        [Fact]
        public void UpdateUrl_empty_uses_stable_channel_for_github()
        {
            // UpdateUrl 为空，Channel 为空时默认走 GitHub stable 通道
            var config = BuildConfig(new Dictionary<string, string?>
            {
                { "Update:UpdateUrl", "" },
                { "Update:Channel", "" }
            });
            var service = new UpdateService(CreateLogger(), config);

            Assert.Equal("stable", service.Channel);
            Assert.Equal("GitHub", service.UpdateSourceType);
        }

        [Fact]
        public void UpdateUrl_nonempty_uses_http_source()
        {
            // UpdateUrl 有值时应使用 HTTP 源
            var config = BuildConfig(new Dictionary<string, string?>
            {
                { "Update:UpdateUrl", "http://server/updates" },
                { "Update:Channel", "stable" }
            });
            var service = new UpdateService(CreateLogger(), config);

            Assert.Equal("HTTP", service.UpdateSourceType);
            Assert.True(service.IsUpdateUrlConfigured);
        }

        [Fact]
        public void IsInstalled_returns_false_in_test_env()
        {
            // 测试环境中没有 Velopack 安装，IsInstalled 应为 false
            var config = BuildConfig(new Dictionary<string, string?>
            {
                { "Update:UpdateUrl", "http://server/updates" },
                { "Update:Channel", "stable" }
            });
            var service = new UpdateService(CreateLogger(), config);

            Assert.False(service.IsInstalled);
        }

        [Fact]
        public void Both_url_and_github_available_prefers_http()
        {
            // UpdateUrl 有值时，即使 GitHub 也可用，也应使用 HTTP 源
            var config = BuildConfig(new Dictionary<string, string?>
            {
                { "Update:UpdateUrl", "http://internal-server/updates" },
                { "Update:Channel", "stable" }
            });
            var service = new UpdateService(CreateLogger(), config);

            Assert.Equal("HTTP", service.UpdateSourceType);
            Assert.True(service.IsUpdateUrlConfigured);
            Assert.Equal("http://internal-server/updates/stable/", service.EffectiveUpdateUrl);
        }
    }
}
