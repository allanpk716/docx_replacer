using System.Reflection;
using Velopack;

namespace DocuFiller.Utils
{
    /// <summary>
    /// 版本信息辅助工具类
    /// </summary>
    public static class VersionHelper
    {
        private static readonly Lazy<string> _currentVersion = new(() =>
        {
            // 策略1：Velopack 安装环境 — 使用 UpdateManager 获取当前安装版本
            try
            {
                var updateManager = new Velopack.UpdateManager("");
                if (updateManager.IsInstalled)
                {
                    return updateManager.CurrentVersion?.ToString() ?? "1.0.0";
                }
            }
            catch
            {
                // 非 Velopack 环境，fallback
            }

            // 策略2：开发环境 — 读取入口程序集的 AssemblyVersion
            try
            {
                var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version;
                if (version != null)
                {
                    return $"{version.Major}.{version.Minor}.{version.Build}";
                }
            }
            catch
            {
                // fallback
            }

            return "1.0.0";
        });

        /// <summary>
        /// 获取当前应用程序版本
        /// </summary>
        public static string GetCurrentVersion() => _currentVersion.Value;

        /// <summary>
        /// 获取完整的版本信息
        /// </summary>
        public static string GetFullVersion() => GetCurrentVersion();

        /// <summary>
        /// 判断是否为开发版本
        /// </summary>
        public static bool IsDevelopmentVersion()
        {
            var version = GetCurrentVersion();
            return version.Contains("-dev.") || version.Contains("-dev");
        }

        /// <summary>
        /// 获取更新通道（从版本号推断）
        /// </summary>
        public static string GetChannel()
        {
            var version = GetCurrentVersion();

            if (version.Contains("-beta")) return "beta";
            if (version.Contains("-alpha")) return "alpha";
            if (version.Contains("-dev")) return "dev";

            return "stable";
        }
    }
}
