using System.IO;
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

        /// <summary>
        /// 创建使用临时目录作为持久化配置路径的 UpdateService，避免测试间互相污染。
        /// </summary>
        private static UpdateService CreateTestService(
            Dictionary<string, string?> configData,
            string? appSettingsPath = null)
        {
            var config = BuildConfig(configData);
            // 使用临时路径避免读写真实 ~/.docx_replacer/update-config.json
            var tempDir = Path.Combine(Path.GetTempPath(), "DocuFillerTest_" + Guid.NewGuid().ToString("N")[..8]);
            Directory.CreateDirectory(tempDir);
            var persistentPath = Path.Combine(tempDir, "update-config.json");

            var service = new UpdateService(CreateLogger(), config, persistentPath);

            if (appSettingsPath != null)
                service.AppSettingsPath = appSettingsPath;

            return service;
        }

        /// <summary>
        /// 清理测试用的临时持久化配置目录
        /// </summary>
        private static void CleanupTestService(UpdateService service)
        {
            if (service.PersistentConfigPath != null)
            {
                try
                {
                    var dir = Path.GetDirectoryName(service.PersistentConfigPath);
                    if (dir != null && Directory.Exists(dir))
                        Directory.Delete(dir, true);
                }
                catch { }
            }
        }

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
        public void Channel_defaults_to_stable_when_empty()
        {
            var service = CreateTestService(new Dictionary<string, string?>
            {
                { "Update:UpdateUrl", "http://server/updates" },
                { "Update:Channel", "" }
            });
            try
            {
                Assert.Equal("stable", service.Channel);
                Assert.Contains("/stable/", service.EffectiveUpdateUrl);
            }
            finally { CleanupTestService(service); }
        }

        [Fact]
        public void Channel_explicit_beta()
        {
            var service = CreateTestService(new Dictionary<string, string?>
            {
                { "Update:UpdateUrl", "http://server/updates" },
                { "Update:Channel", "beta" }
            });
            try
            {
                Assert.Equal("beta", service.Channel);
                Assert.Contains("/beta/", service.EffectiveUpdateUrl);
            }
            finally { CleanupTestService(service); }
        }

        [Fact]
        public void Channel_missing_key()
        {
            // No Channel key in config at all — should default to stable
            var service = CreateTestService(new Dictionary<string, string?>
            {
                { "Update:UpdateUrl", "http://server/updates" }
            });
            try
            {
                Assert.Equal("stable", service.Channel);
                Assert.Contains("/stable/", service.EffectiveUpdateUrl);
            }
            finally { CleanupTestService(service); }
        }

        [Fact]
        public void UpdateUrl_empty_uses_github_source()
        {
            // UpdateUrl 为空时，GitHub Releases 作为备选源始终可用
            var service = CreateTestService(new Dictionary<string, string?>
            {
                { "Update:UpdateUrl", "" },
                { "Update:Channel", "stable" }
            });
            try
            {
                Assert.True(service.IsUpdateUrlConfigured);
                Assert.Equal("GitHub", service.UpdateSourceType);
                Assert.Equal("", service.EffectiveUpdateUrl);
            }
            finally { CleanupTestService(service); }
        }

        [Fact]
        public void UpdateUrl_with_trailing_slash()
        {
            var service = CreateTestService(new Dictionary<string, string?>
            {
                { "Update:UpdateUrl", "http://server/" },
                { "Update:Channel", "stable" }
            });
            try
            {
                // Should not produce double slashes
                Assert.Equal("http://server/stable/", service.EffectiveUpdateUrl);
                Assert.DoesNotContain("//", service.EffectiveUpdateUrl.Replace("http://", ""));
                Assert.Equal("HTTP", service.UpdateSourceType);
            }
            finally { CleanupTestService(service); }
        }

        [Fact]
        public void UpdateUrl_without_trailing_slash()
        {
            var service = CreateTestService(new Dictionary<string, string?>
            {
                { "Update:UpdateUrl", "http://server" },
                { "Update:Channel", "stable" }
            });
            try
            {
                Assert.Equal("http://server/stable/", service.EffectiveUpdateUrl);
                Assert.Equal("HTTP", service.UpdateSourceType);
            }
            finally { CleanupTestService(service); }
        }

        [Fact]
        public void UpdateUrl_empty_uses_stable_channel_for_github()
        {
            // UpdateUrl 为空，Channel 为空时默认走 GitHub stable 通道
            var service = CreateTestService(new Dictionary<string, string?>
            {
                { "Update:UpdateUrl", "" },
                { "Update:Channel", "" }
            });
            try
            {
                Assert.Equal("stable", service.Channel);
                Assert.Equal("GitHub", service.UpdateSourceType);
            }
            finally { CleanupTestService(service); }
        }

        [Fact]
        public void UpdateUrl_nonempty_uses_http_source()
        {
            // UpdateUrl 有值时应使用 HTTP 源
            var service = CreateTestService(new Dictionary<string, string?>
            {
                { "Update:UpdateUrl", "http://server/updates" },
                { "Update:Channel", "stable" }
            });
            try
            {
                Assert.Equal("HTTP", service.UpdateSourceType);
                Assert.True(service.IsUpdateUrlConfigured);
            }
            finally { CleanupTestService(service); }
        }

        [Fact]
        public void IsInstalled_returns_false_in_test_env()
        {
            // 测试环境中没有 Velopack 安装，IsInstalled 应为 false
            var service = CreateTestService(new Dictionary<string, string?>
            {
                { "Update:UpdateUrl", "http://server/updates" },
                { "Update:Channel", "stable" }
            });
            try
            {
                Assert.False(service.IsInstalled);
            }
            finally { CleanupTestService(service); }
        }

        [Fact]
        public void Both_url_and_github_available_prefers_http()
        {
            // UpdateUrl 有值时，即使 GitHub 也可用，也应使用 HTTP 源
            var service = CreateTestService(new Dictionary<string, string?>
            {
                { "Update:UpdateUrl", "http://internal-server/updates" },
                { "Update:Channel", "stable" }
            });
            try
            {
                Assert.Equal("HTTP", service.UpdateSourceType);
                Assert.True(service.IsUpdateUrlConfigured);
                Assert.Equal("http://internal-server/updates/stable/", service.EffectiveUpdateUrl);
            }
            finally { CleanupTestService(service); }
        }

        // --- ReloadSource 内存热重载测试 ---

        [Fact]
        public void ReloadSource_http_changes_source_type_to_HTTP()
        {
            // 构造 GitHub 模式服务（空 URL）→ 切换到 HTTP
            var service = CreateTestService(new Dictionary<string, string?>
            {
                { "Update:UpdateUrl", "" },
                { "Update:Channel", "stable" }
            });
            try
            {
                Assert.Equal("GitHub", service.UpdateSourceType);

                service.ReloadSource("http://server", "stable");

                Assert.Equal("HTTP", service.UpdateSourceType);
                Assert.Equal("http://server/stable/", service.EffectiveUpdateUrl);
            }
            finally { CleanupTestService(service); }
        }

        [Fact]
        public void ReloadSource_empty_changes_source_type_to_GitHub()
        {
            // 构造 HTTP 模式服务 → 切换到 GitHub
            var service = CreateTestService(new Dictionary<string, string?>
            {
                { "Update:UpdateUrl", "http://server" },
                { "Update:Channel", "stable" }
            });
            try
            {
                Assert.Equal("HTTP", service.UpdateSourceType);

                service.ReloadSource("", "stable");

                Assert.Equal("GitHub", service.UpdateSourceType);
                Assert.Equal("", service.EffectiveUpdateUrl);
            }
            finally { CleanupTestService(service); }
        }

        [Fact]
        public void ReloadSource_updates_channel()
        {
            var service = CreateTestService(new Dictionary<string, string?>
            {
                { "Update:UpdateUrl", "http://server" },
                { "Update:Channel", "stable" }
            });
            try
            {
                service.ReloadSource("http://server", "beta");

                Assert.Equal("beta", service.Channel);
                Assert.Contains("/beta/", service.EffectiveUpdateUrl);
            }
            finally { CleanupTestService(service); }
        }

        [Fact]
        public void ReloadSource_null_url_treated_as_empty()
        {
            var service = CreateTestService(new Dictionary<string, string?>
            {
                { "Update:UpdateUrl", "http://server" },
                { "Update:Channel", "stable" }
            });
            try
            {
                service.ReloadSource(null, "stable");

                Assert.Equal("GitHub", service.UpdateSourceType);
                Assert.Equal("", service.EffectiveUpdateUrl);
            }
            finally { CleanupTestService(service); }
        }

        [Fact]
        public void ReloadSource_null_channel_defaults_to_stable()
        {
            var service = CreateTestService(new Dictionary<string, string?>
            {
                { "Update:UpdateUrl", "" },
                { "Update:Channel", "beta" }
            });
            try
            {
                service.ReloadSource("http://server", null);

                Assert.Equal("stable", service.Channel);
                Assert.Equal("HTTP", service.UpdateSourceType);
            }
            finally { CleanupTestService(service); }
        }

        [Fact]
        public void ReloadSource_with_trailing_slash_no_double_slash()
        {
            var service = CreateTestService(new Dictionary<string, string?>
            {
                { "Update:UpdateUrl", "" },
                { "Update:Channel", "stable" }
            });
            try
            {
                service.ReloadSource("http://server/", "stable");

                Assert.Equal("http://server/stable/", service.EffectiveUpdateUrl);
                Assert.DoesNotContain("//", service.EffectiveUpdateUrl.Replace("http://", ""));
            }
            finally { CleanupTestService(service); }
        }

        [Fact]
        public void ReloadSource_channel_with_whitespace_trimmed()
        {
            var service = CreateTestService(new Dictionary<string, string?>
            {
                { "Update:UpdateUrl", "" },
                { "Update:Channel", "stable" }
            });
            try
            {
                service.ReloadSource("http://server", "  beta  ");

                Assert.Equal("beta", service.Channel);
                Assert.Contains("/beta/", service.EffectiveUpdateUrl);
            }
            finally { CleanupTestService(service); }
        }

        // --- PersistToAppSettings 持久化测试 ---

        // --- 路径验证测试 (T02) ---

        [Fact]
        public void GetPersistentConfigPath_returns_user_profile_path()
        {
            // 静态方法，无需创建 service 实例
            var path = UpdateService.GetPersistentConfigPath();

            // 应包含用户目录 + .docx_replacer + update-config.json
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            Assert.True(path.StartsWith(userProfile, StringComparison.OrdinalIgnoreCase),
                $"Expected path under {userProfile}, got {path}");
            Assert.Contains(".docx_replacer", path);
            Assert.True(path.EndsWith("update-config.json"),
                $"Expected path ending with update-config.json, got {path}");

            // 路径的目录部分应正好是 ~/.docx_replacer/
            var dir = Path.GetDirectoryName(path);
            Assert.Equal(Path.Combine(userProfile, ".docx_replacer"), dir);
        }

        [Fact]
        public void EnsurePersistentConfigSync_creates_directory_and_file()
        {
            // 使用测试注入的临时路径验证目录和文件的自动创建
            var tempBase = Path.Combine(Path.GetTempPath(), "DocuFillerTest_EnsureSync_" + Guid.NewGuid().ToString("N")[..8]);
            var tempPath = Path.Combine(tempBase, "sub", "update-config.json");
            try
            {
                // CreateTestService 会触发构造函数中的 EnsurePersistentConfigSync
                var service = CreateTestService(new Dictionary<string, string?>
                {
                    { "Update:UpdateUrl", "http://server/updates" },
                    { "Update:Channel", "beta" }
                }, appSettingsPath: null);
                // 但 CreateTestService 使用了自己的 tempDir，所以我们需要手动设置路径
                // 实际上 CreateTestService 已经通过 internal 构造函数注入了 persistentPath
                // 让我们直接验证 CreateTestService 创建的文件
                var createdPath = service.PersistentConfigPath;
                Assert.NotNull(createdPath);
                Assert.True(File.Exists(createdPath),
                    $"EnsurePersistentConfigSync should have created {createdPath}");

                // 验证文件内容
                var json = File.ReadAllText(createdPath);
                var node = System.Text.Json.Nodes.JsonNode.Parse(json)!;
                Assert.Equal("http://server/updates", node["UpdateUrl"]!.GetValue<string>());
                Assert.Equal("beta", node["Channel"]!.GetValue<string>());

                CleanupTestService(service);
            }
            finally
            {
                if (Directory.Exists(tempBase))
                    Directory.Delete(tempBase, true);
            }
        }

        [Fact]
        public void ReadPersistentConfig_reads_from_persistent_path()
        {
            // 创建一个临时配置文件，验证构造时 ReadPersistentConfig 正确读取
            var tempDir = Path.Combine(Path.GetTempPath(), "DocuFillerTest_ReadPersist_" + Guid.NewGuid().ToString("N")[..8]);
            Directory.CreateDirectory(tempDir);
            var tempPath = Path.Combine(tempDir, "update-config.json");

            // 写入持久化配置：HTTP 源 + beta 通道
            var configJson = new System.Text.Json.Nodes.JsonObject
            {
                ["UpdateUrl"] = "http://custom-server:9090/updates",
                ["Channel"] = "beta"
            };
            File.WriteAllText(tempPath, configJson.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));

            try
            {
                // IConfiguration 中只有空 URL + stable 通道，但持久化配置应优先
                var config = BuildConfig(new Dictionary<string, string?>
                {
                    { "Update:UpdateUrl", "" },
                    { "Update:Channel", "stable" }
                });
                var service = new UpdateService(CreateLogger(), config, tempPath);

                // 持久化配置应覆盖 appsettings.json 的值
                Assert.Equal("HTTP", service.UpdateSourceType);
                Assert.Equal("beta", service.Channel);
                Assert.Contains("custom-server", service.EffectiveUpdateUrl);
                Assert.Contains("/beta/", service.EffectiveUpdateUrl);
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void ReloadSource_persists_to_appsettings_json()
        {
            // 创建临时 appsettings.json
            var tempPath = CreateTempAppSettings(@"{""Update"":{""UpdateUrl"":"""",""Channel"":""stable""}}");
            try
            {
                var service = CreateTestService(
                    new Dictionary<string, string?>
                    {
                        { "Update:UpdateUrl", "" },
                        { "Update:Channel", "stable" }
                    },
                    appSettingsPath: tempPath);

                service.ReloadSource("http://update-server.example.com:8080", "beta");

                // 验证文件内容
                var json = File.ReadAllText(tempPath);
                var node = System.Text.Json.Nodes.JsonNode.Parse(json)!;
                Assert.Equal("http://update-server.example.com:8080", node["Update"]!["UpdateUrl"]!.GetValue<string>());
                Assert.Equal("beta", node["Update"]!["Channel"]!.GetValue<string>());

                CleanupTestService(service);
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
                var service = CreateTestService(
                    new Dictionary<string, string?>
                    {
                        { "Update:UpdateUrl", "http://old/" },
                        { "Update:Channel", "stable" }
                    },
                    appSettingsPath: tempPath);

                service.ReloadSource("", "stable");

                // 内存字段已更新
                Assert.Equal("GitHub", service.UpdateSourceType);

                // 文件内容：UpdateUrl 为空字符串
                var json = File.ReadAllText(tempPath);
                var node = System.Text.Json.Nodes.JsonNode.Parse(json)!;
                Assert.Equal("", node["Update"]!["UpdateUrl"]!.GetValue<string>());
                Assert.Equal("stable", node["Update"]!["Channel"]!.GetValue<string>());

                CleanupTestService(service);
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
            var service = CreateTestService(new Dictionary<string, string?>
            {
                { "Update:UpdateUrl", "" },
                { "Update:Channel", "stable" }
            });
            service.AppSettingsPath = Path.Combine(Path.GetTempPath(), "nonexistent_dir_" + Guid.NewGuid(), "appsettings.json");
            try
            {
                // 不应抛异常
                service.ReloadSource("http://server", "beta");

                // 内存字段已更新
                Assert.Equal("HTTP", service.UpdateSourceType);
                Assert.Equal("http://server/beta/", service.EffectiveUpdateUrl);
                Assert.Equal("beta", service.Channel);
            }
            finally { CleanupTestService(service); }
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
                var service = CreateTestService(
                    new Dictionary<string, string?>
                    {
                        { "Update:UpdateUrl", "" },
                        { "Update:Channel", "stable" }
                    },
                    appSettingsPath: tempPath);

                service.ReloadSource("http://server", "beta");

                // 验证 Update 节已更新
                var json = File.ReadAllText(tempPath);
                var node = System.Text.Json.Nodes.JsonNode.Parse(json)!;
                Assert.Equal("http://server", node["Update"]!["UpdateUrl"]!.GetValue<string>());
                Assert.Equal("beta", node["Update"]!["Channel"]!.GetValue<string>());

                // 验证其他配置节未被破坏
                Assert.Equal("Information", node["Logging"]!["LogLevel"]!["Default"]!.GetValue<string>());
                Assert.Equal(4, node["Performance"]!["MaxParallelism"]!.GetValue<int>());

                CleanupTestService(service);
            }
            finally
            {
                Directory.Delete(Path.GetDirectoryName(tempPath)!, true);
            }
        }

        // --- 持久化配置边界测试 ---

        [Fact]
        public void ReadPersistentConfig_malformed_json_falls_back_to_appsettings()
        {
            // 配置文件包含非法 JSON 时，ReadPersistentConfig 返回 null fallback，不崩溃
            var tempDir = Path.Combine(Path.GetTempPath(), "DocuFillerTest_MalformedJson_" + Guid.NewGuid().ToString("N")[..8]);
            Directory.CreateDirectory(tempDir);
            var tempPath = Path.Combine(tempDir, "update-config.json");
            File.WriteAllText(tempPath, "{invalid");

            try
            {
                // IConfiguration 中有明确的 HTTP URL + beta 通道
                var config = BuildConfig(new Dictionary<string, string?>
                {
                    { "Update:UpdateUrl", "http://fallback-server/updates" },
                    { "Update:Channel", "beta" }
                });
                // 不应抛异常，应 fallback 到 appsettings.json 的值
                var service = new UpdateService(CreateLogger(), config, tempPath);

                Assert.Equal("HTTP", service.UpdateSourceType);
                Assert.Equal("http://fallback-server/updates/beta/", service.EffectiveUpdateUrl);
                Assert.Equal("beta", service.Channel);
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void ReadPersistentConfig_missing_UpdateUrl_field_falls_back_to_appsettings()
        {
            // JSON 中只有 Channel 没有 UpdateUrl → url 为 null → fallback 到 appsettings.json
            var tempDir = Path.Combine(Path.GetTempPath(), "DocuFillerTest_MissingUrl_" + Guid.NewGuid().ToString("N")[..8]);
            Directory.CreateDirectory(tempDir);
            var tempPath = Path.Combine(tempDir, "update-config.json");
            File.WriteAllText(tempPath, @"{""Channel"":""beta""}");

            try
            {
                var config = BuildConfig(new Dictionary<string, string?>
                {
                    { "Update:UpdateUrl", "http://appsettings-url/updates" },
                    { "Update:Channel", "stable" }
                });
                var service = new UpdateService(CreateLogger(), config, tempPath);

                // UpdateUrl 从 appsettings.json fallback
                Assert.Equal("HTTP", service.UpdateSourceType);
                Assert.Contains("appsettings-url", service.EffectiveUpdateUrl);
                // Channel 从持久化配置读取（非空）
                Assert.Equal("beta", service.Channel);
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void ReadPersistentConfig_missing_Channel_field_defaults_to_stable()
        {
            // JSON 中只有 UpdateUrl 没有 Channel → channel 为 null → IConfiguration 也无 Channel → 默认 stable
            var tempDir = Path.Combine(Path.GetTempPath(), "DocuFillerTest_MissingChannel_" + Guid.NewGuid().ToString("N")[..8]);
            Directory.CreateDirectory(tempDir);
            var tempPath = Path.Combine(tempDir, "update-config.json");
            File.WriteAllText(tempPath, @"{""UpdateUrl"":""http://custom-server/updates""}");

            try
            {
                // IConfiguration 中不传 Channel，这样 Channel 全链路 fallback 到 "stable"
                var config = BuildConfig(new Dictionary<string, string?>
                {
                    { "Update:UpdateUrl", "" }
                });
                var service = new UpdateService(CreateLogger(), config, tempPath);

                // Channel 从持久化配置读取为 null，IConfiguration 也无 Channel，构造函数默认 "stable"
                Assert.Equal("stable", service.Channel);
                Assert.Contains("/stable/", service.EffectiveUpdateUrl);
                // UpdateUrl 从持久化配置读取
                Assert.Equal("HTTP", service.UpdateSourceType);
                Assert.Contains("custom-server", service.EffectiveUpdateUrl);
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void ReadPersistentConfig_empty_file_does_not_crash()
        {
            // 配置文件为空文件（0 bytes）时不崩溃，fallback 到 appsettings.json
            var tempDir = Path.Combine(Path.GetTempPath(), "DocuFillerTest_EmptyFile_" + Guid.NewGuid().ToString("N")[..8]);
            Directory.CreateDirectory(tempDir);
            var tempPath = Path.Combine(tempDir, "update-config.json");
            File.WriteAllText(tempPath, "");

            try
            {
                var config = BuildConfig(new Dictionary<string, string?>
                {
                    { "Update:UpdateUrl", "http://fallback/updates" },
                    { "Update:Channel", "stable" }
                });
                var service = new UpdateService(CreateLogger(), config, tempPath);

                // 空 JSON 解析失败，被 catch 捕获，fallback 到 appsettings.json
                Assert.Equal("HTTP", service.UpdateSourceType);
                Assert.Equal("http://fallback/updates/stable/", service.EffectiveUpdateUrl);
                Assert.Equal("stable", service.Channel);
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void EnsurePersistentConfigSync_does_not_overwrite_existing_file()
        {
            // 目录已存在且文件已存在时，EnsurePersistentConfigSync 不覆盖已有内容
            var tempDir = Path.Combine(Path.GetTempPath(), "DocuFillerTest_NoOverwrite_" + Guid.NewGuid().ToString("N")[..8]);
            Directory.CreateDirectory(tempDir);
            var tempPath = Path.Combine(tempDir, "update-config.json");

            // 预写一个自定义配置（模拟用户手动配置的内容）
            var originalContent = @"{""UpdateUrl"":""http://custom-original:9999"",""Channel"":""beta""}";
            File.WriteAllText(tempPath, originalContent);

            try
            {
                // 构造函数会调用 EnsurePersistentConfigSync，但文件已存在时应跳过
                var config = BuildConfig(new Dictionary<string, string?>
                {
                    { "Update:UpdateUrl", "http://different-url/updates" },
                    { "Update:Channel", "stable" }
                });
                var service = new UpdateService(CreateLogger(), config, tempPath);

                // 文件内容应保持不变（未被覆盖为构造函数的 appsettings 值）
                var content = File.ReadAllText(tempPath);
                Assert.Equal(originalContent, content);

                // 服务应使用持久化配置中的值，而非 appsettings 中的值
                Assert.Contains("custom-original", service.EffectiveUpdateUrl);
                Assert.Equal("beta", service.Channel);
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}
