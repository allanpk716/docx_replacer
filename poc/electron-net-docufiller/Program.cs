using ElectronNET.API;
using ElectronNET.API.Entities;
using ElectronNetDocufiller.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddRazorPages();
builder.Services.AddSingleton<IProcessingService, SimulatedProcessor>();

// Configure Electron.NET
builder.WebHost.UseElectron(args);

var app = builder.Build();

// Configure middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();
app.MapControllers();
app.MapGet("/", () => Results.Redirect("/index.html"));

// Set up Electron IPC handlers (demonstrates bidirectional IPC)
if (HybridSupport.IsElectronActive)
{
    SetupIpcHandlers(app.Services);
    await ElectronBootstrap();
}

app.Run();

void SetupIpcHandlers(IServiceProvider services)
{
    var ipcLogger = services.GetRequiredService<ILogger<Program>>();

    // IPC: renderer sends "select-file-ipc" → backend opens native dialog
    Electron.IpcMain.On("select-file-ipc", async (args) =>
    {
        ipcLogger.LogInformation("[IPC] Received 'select-file-ipc' message from renderer");

        try
        {
            var mainWindow = Electron.WindowManager.BrowserWindows.FirstOrDefault();
            if (mainWindow == null)
            {
                var noWindowReply = System.Text.Json.JsonSerializer.Serialize(new { path = (string?)null, message = "No window" });
                // Send to all windows if we can't find the specific one
                ipcLogger.LogWarning("[IPC] No window found for IPC reply");
                return;
            }

            var options = new OpenDialogOptions
            {
                Title = "选择要处理的文档 (IPC)",
                Properties = new[] { OpenDialogProperty.openFile },
                Filters = new[]
                {
                    new FileFilter
                    {
                        Name = "文档文件",
                        Extensions = new[] { "docx", "xlsx", "pdf", "txt" }
                    }
                }
            };

            var filePaths = await Electron.Dialog.ShowOpenDialogAsync(mainWindow, options);
            var filePath = filePaths?.FirstOrDefault();

            var reply = System.Text.Json.JsonSerializer.Serialize(new
            {
                path = filePath ?? "",
                message = string.IsNullOrEmpty(filePath) ? "未选择文件" : "文件已选择"
            });

            // IPC: backend sends reply to renderer
            Electron.IpcMain.Send(mainWindow, "select-file-ipc-reply", reply);
            ipcLogger.LogInformation("[IPC] Sent 'select-file-ipc-reply' to renderer: {Reply}", reply);
        }
        catch (Exception ex)
        {
            ipcLogger.LogError(ex, "[IPC] Error in select-file-ipc handler");
        }
    });

    // IPC: renderer sends "ping" → backend replies "pong" (basic IPC test)
    Electron.IpcMain.On("ping", (args) =>
    {
        ipcLogger.LogInformation("[IPC] Received 'ping' from renderer, sending 'pong'");
        var mainWindow = Electron.WindowManager.BrowserWindows.FirstOrDefault();
        if (mainWindow != null)
        {
            Electron.IpcMain.Send(mainWindow, "pong", $"pong at {DateTime.UtcNow:O}");
        }
    });

    Console.WriteLine("[Electron.NET] IPC handlers registered: select-file-ipc, ping");
}

async Task ElectronBootstrap()
{
    var window = await Electron.WindowManager.CreateWindowAsync(new BrowserWindowOptions
    {
        Width = 900,
        Height = 650,
        Title = "DocuFiller Electron.NET PoC",
        Show = false,
        WebPreferences = new WebPreferences
        {
            NodeIntegration = false,
            ContextIsolation = true
        }
    });

    await window.WebContents.Session.ClearCacheAsync();

    window.OnReadyToShow += () => window.Show();
    window.OnClosed += () =>
    {
        Electron.App.Quit();
    };

    Console.WriteLine("[Electron.NET] Bootstrap complete — window created.");
}
