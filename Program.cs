using System;
using System.Windows;
using Velopack;

namespace DocuFiller
{
    /// <summary>
    /// 自定义入口点，支持 CLI/GUI 双模式。
    /// 当有命令行参数时直接走 CLI 路径，绕过 WPF Application 的 InitializeComponent()。
    /// 无参数时启动标准 WPF GUI。
    /// </summary>
    public static class Program
    {
        [STAThread]
        public static int Main(string[] args)
        {
            // Velopack 必须在 Main 最先调用，处理安装/更新/卸载钩子
            VelopackApp.Build().Run();

            if (args.Length > 0)
            {
                // CLI 模式：完全绕过 WPF Application 初始化
                // 使用 App 类的静态方法创建 CLI 服务容器
                var serviceProvider = App.CreateCliServices();
                try
                {
                    var cliRunner = new Cli.CliRunner(serviceProvider);
                    int exitCode = cliRunner.RunAsync(args).GetAwaiter().GetResult();
                    return exitCode;
                }
                finally
                {
                    serviceProvider.Dispose();
                }
            }

            // GUI 模式：启动标准 WPF Application
            var app = new App();
            app.InitializeComponent();
            app.Run();
            return 0;
        }
    }
}
