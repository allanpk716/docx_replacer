using ElectronNET.API;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Configure Electron.NET
builder.WebHost.UseElectron(args);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();
app.MapGet("/", () => Results.Redirect("/index.html"));

// Bootstrap Electron window
if (HybridSupport.IsElectronActive)
{
    await ElectronBootstrap();
}

app.Run();

async Task ElectronBootstrap()
{
    var window = await Electron.WindowManager.CreateWindowAsync(new ElectronNET.API.Entities.BrowserWindowOptions
    {
        Width = 800,
        Height = 600,
        Title = "DocuFiller Electron.NET PoC",
        Show = false
    });

    await window.WebContents.Session.ClearCacheAsync();

    window.OnReadyToShow += () => window.Show();
    window.OnClosed += () =>
    {
        Electron.App.Quit();
    };

    Console.WriteLine("[Electron.NET] Bootstrap complete — window created.");
}
