using System.Reflection;

namespace DocuFiller.Utils
{
    /// <summary>
    /// 版本信息辅助工具类
    /// </summary>
    public static class VersionHelper
    {
        /// <summary>
        /// 获取当前应用程序版本
        /// </summary>
        public static string GetCurrentVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var assemblyName = assembly.GetName();
            var version = assemblyName.Version?.ToString() ?? "1.0.0.0";

            // 返回主版本.次版本.修订号（去掉构建号）
            var parts = version.Split('.');
            if (parts.Length >= 3)
            {
                return $"{parts[0]}.{parts[1]}.{parts[2]}";
            }
            return version;
        }

        /// <summary>
        /// 获取完整的版本信息（包含构建号）
        /// </summary>
        public static string GetFullVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            return assembly.GetName().Version?.ToString() ?? "1.0.0.0";
        }

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
