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

        // --- ReloadSource 内存热重载测试 ---

        [Fact]
        public void ReloadSource_http_changes_source_type_to_HTTP()
        {
            // 构造 GitHub 模式服务（空 URL）→ 切换到 HTTP
            var config = BuildConfig(new Dictionary<string, string?>
            {
                { "Update:UpdateUrl", "" },
                { "Update:Channel", "stable" }
            });
            var service = new UpdateService(CreateLogger(), config);

            Assert.Equal("GitHub", service.UpdateSourceType);

            service.ReloadSource("http://server", "stable");

            Assert.Equal("HTTP", service.UpdateSourceType);
            Assert.Equal("http://server/stable/", service.EffectiveUpdateUrl);
        }

        [Fact]
        public void ReloadSource_empty_changes_source_type_to_GitHub()
        {
            // 构造 HTTP 模式服务 → 切换到 GitHub
            var config = BuildConfig(new Dictionary<string, string?>
            {
                { "Update:UpdateUrl", "http://server" },
                { "Update:Channel", "stable" }
            });
            var service = new UpdateService(CreateLogger(), config);

            Assert.Equal("HTTP", service.UpdateSourceType);

            service.ReloadSource("", "stable");

            Assert.Equal("GitHub", service.UpdateSourceType);
            Assert.Equal("", service.EffectiveUpdateUrl);
        }

        [Fact]
        public void ReloadSource_updates_channel()
        {
            var config = BuildConfig(new Dictionary<string, string?>
            {
                { "Update:UpdateUrl", "http://server" },
                { "Update:Channel", "stable" }
            });
            var service = new UpdateService(CreateLogger(), config);

            service.ReloadSource("http://server", "beta");

            Assert.Equal("beta", service.Channel);
            Assert.Contains("/beta/", service.EffectiveUpdateUrl);
        }

        [Fact]
        public void ReloadSource_null_url_treated_as_empty()
        {
            var config = BuildConfig(new Dictionary<string, string?>
            {
                { "Update:UpdateUrl", "http://server" },
                { "Update:Channel", "stable" }
            });
            var service = new UpdateService(CreateLogger(), config);

            service.ReloadSource(null, "stable");

            Assert.Equal("GitHub", service.UpdateSourceType);
            Assert.Equal("", service.EffectiveUpdateUrl);
        }

        [Fact]
        public void ReloadSource_null_channel_defaults_to_stable()
        {
            var config = BuildConfig(new Dictionary<string, string?>
            {
                { "Update:UpdateUrl", "" },
                { "Update:Channel", "beta" }
            });
            var service = new UpdateService(CreateLogger(), config);

            service.ReloadSource("http://server", null);

            Assert.Equal("stable", service.Channel);
            Assert.Equal("HTTP", service.UpdateSourceType);
        }

        [Fact]
        public void ReloadSource_with_trailing_slash_no_double_slash()
        {
            var config = BuildConfig(new Dictionary<string, string?>
            {
                { "Update:UpdateUrl", "" },
                { "Update:Channel", "stable" }
            });
            var service = new UpdateService(CreateLogger(), config);

            service.ReloadSource("http://server/", "stable");

            Assert.Equal("http://server/stable/", service.EffectiveUpdateUrl);
            Assert.DoesNotContain("//", service.EffectiveUpdateUrl.Replace("http://", ""));
        }

        [Fact]
        public void ReloadSource_channel_with_whitespace_trimmed()
        {
            var config = BuildConfig(new Dictionary<string, string?>
            {
                { "Update:UpdateUrl", "" },
                { "Update:Channel", "stable" }
            });
            var service = new UpdateService(CreateLogger(), config);

            service.ReloadSource("http://server", "  beta  ");

            Assert.Equal("beta", service.Channel);
            Assert.Contains("/beta/", service.EffectiveUpdateUrl);
        }

        // --- PersistToAppSettings 持久化测试 ---

        /// <summary>
        /// 创建临时 appsettings.json 文件，返回文件路径
        /// </summary>
        private static string CreateTempAppSettings(string jsonContent)
        {
            var dir = Path.Combine(Path.GetTempPath(), "DocuFillerTest_" + Guid.NewGuid().ToString("N")[..8]);
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, "appsettings.json");
            File.WriteAllText(path, jsonContent);
            return path;
        }

        [Fact]
        public void ReloadSource_persists_to_appsettings_json()
        {
            // 创建临时 appsettings.json
            var tempPath = CreateTempAppSettings(@"{""Update"":{""UpdateUrl"":"""",""Channel"":""stable""}}");
            try
            {
                var config = BuildConfig(new Dictionary<string, string?>
                {
                    { "Update:UpdateUrl", "" },
                    { "Update:Channel", "stable" }
                });
                var service = new UpdateService(CreateLogger(), config);
                service.AppSettingsPath = tempPath;

                service.ReloadSource("http://192.168.1.100:8080", "beta");

                // 验证文件内容
                var json = File.ReadAllText(tempPath);
                var node = System.Text.Json.Nodes.JsonNode.Parse(json)!;
                Assert.Equal("http://192.168.1.100:8080", node["Update"]!["UpdateUrl"]!.GetValue<string>());
                Assert.Equal("beta", node["Update"]!["Channel"]!.GetValue<string>());
            }
            finally
            {
                Directory.Delete(Path.GetDirectoryName(tempPath)!, true);
            }
        }

        [Fact]
        public void ReloadSource_empty_url_persists_empty_string()
        {
            var tempPath = CreateTempAppSettings(@"{""Update"":{""UpdateUrl"":""http://old/"",""Channel"":""stable""}}");
            try
            {
                var config = BuildConfig(new Dictionary<string, string?>
                {
                    { "Update:UpdateUrl", "http://old/" },
                    { "Update:Channel", "stable" }
                });
                var service = new UpdateService(CreateLogger(), config);
                service.AppSettingsPath = tempPath;

                service.ReloadSource("", "stable");

                // 内存字段已更新
                Assert.Equal("GitHub", service.UpdateSourceType);

                // 文件内容：UpdateUrl 为空字符串
                var json = File.ReadAllText(tempPath);
                var node = System.Text.Json.Nodes.JsonNode.Parse(json)!;
                Assert.Equal("", node["Update"]!["UpdateUrl"]!.GetValue<string>());
                Assert.Equal("stable", node["Update"]!["Channel"]!.GetValue<string>());
            }
            finally
            {
                Directory.Delete(Path.GetDirectoryName(tempPath)!, true);
            }
        }

        [Fact]
        public void ReloadSource_persistence_failure_does_not_throw()
        {
            // 设置 AppSettingsPath 为不存在的目录下的文件
            var config = BuildConfig(new Dictionary<string, string?>
            {
                { "Update:UpdateUrl", "" },
                { "Update:Channel", "stable" }
            });
            var service = new UpdateService(CreateLogger(), config);
            service.AppSettingsPath = Path.Combine(Path.GetTempPath(), "nonexistent_dir_" + Guid.NewGuid(), "appsettings.json");

            // 不应抛异常
            service.ReloadSource("http://server", "beta");

            // 内存字段已更新
            Assert.Equal("HTTP", service.UpdateSourceType);
            Assert.Equal("http://server/beta/", service.EffectiveUpdateUrl);
            Assert.Equal("beta", service.Channel);
        }

        [Fact]
        public void ReloadSource_preserves_other_settings()
        {
            // 创建包含其他配置节的 appsettings.json
            var tempPath = CreateTempAppSettings(@"{
  ""Logging"": {
    ""LogLevel"": {
      ""Default"": ""Information""
    }
  },
  ""Update"": {
    ""UpdateUrl"": """",
    ""Channel"": ""stable""
  },
  ""Performance"": {
    ""MaxParallelism"": 4
  }
}");
            try
            {
                var config = BuildConfig(new Dictionary<string, string?>
                {
                    { "Update:UpdateUrl", "" },
                    { "Update:Channel", "stable" }
                });
                var service = new UpdateService(CreateLogger(), config);
                service.AppSettingsPath = tempPath;

                service.ReloadSource("http://server", "beta");

                // 验证 Update 节已更新
                var json = File.ReadAllText(tempPath);
                var node = System.Text.Json.Nodes.JsonNode.Parse(json)!;
                Assert.Equal("http://server", node["Update"]!["UpdateUrl"]!.GetValue<string>());
                Assert.Equal("beta", node["Update"]!["Channel"]!.GetValue<string>());

                // 验证其他配置节未被破坏
                Assert.Equal("Information", node["Logging"]!["LogLevel"]!["Default"]!.GetValue<string>());
                Assert.Equal(4, node["Performance"]!["MaxParallelism"]!.GetValue<int>());
            }
            finally
            {
                Directory.Delete(Path.GetDirectoryName(tempPath)!, true);
            }
        }
    }
}
