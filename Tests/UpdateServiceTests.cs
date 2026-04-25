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
        public void UpdateUrl_empty_not_configured()
        {
            var config = BuildConfig(new Dictionary<string, string?>
            {
                { "Update:UpdateUrl", "" },
                { "Update:Channel", "stable" }
            });
            var service = new UpdateService(CreateLogger(), config);

            Assert.False(service.IsUpdateUrlConfigured);
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
        }
    }
}
