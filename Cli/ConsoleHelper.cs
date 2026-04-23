using System.Runtime.InteropServices;

namespace DocuFiller.Cli;

/// <summary>
/// WinExe stdout P/Invoke 解决方案。
/// WinExe 类型应用默认不附加到父控制台，需要通过 AttachConsole(-1) 显式附加。
/// </summary>
internal static class ConsoleHelper
{
    private const int AttachParentProcess = -1;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AttachConsole(int dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AllocConsole();

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool FreeConsole();

    private static bool _initialized;

    /// <summary>
    /// 附加到父进程的控制台。如果父进程没有控制台（例如双击启动），则不做任何操作。
    /// </summary>
    public static void Initialize()
    {
        if (_initialized) return;

        // AttachConsole(-1) 尝试附加到父进程的控制台
        // 从 cmd/PowerShell 调用时会成功；双击启动时会失败（ERROR_ACCESS_DENIED）
        bool attached = AttachConsole(AttachParentProcess);
        if (attached)
        {
            _initialized = true;
        }
        // 不调用 AllocConsole —— 我们不希望在双击启动时弹出一个控制台窗口
    }

    /// <summary>
    /// 释放控制台附加。应在 CLI 模式退出前调用。
    /// </summary>
    public static void Cleanup()
    {
        if (!_initialized) return;

        FreeConsole();
        _initialized = false;
    }

    /// <summary>
    /// 控制台是否已成功初始化（即是否有可用的控制台输出）。
    /// </summary>
    public static bool IsConsoleAvailable => _initialized;
}
