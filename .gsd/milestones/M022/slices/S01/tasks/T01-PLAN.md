---
estimated_steps: 2
estimated_files: 6
skills_used: []
---

# T01: Scaffold Electron.NET project and verify toolchain

Initialize an ASP.NET Core + Electron.NET project in poc/electron-net-docufiller/. Set up the project structure, install ElectronNET.API NuGet package and electronize CLI tool, configure Program.cs for Electron.NET hosting, create a basic HTML frontend that opens in an Electron window. Verify the entire toolchain compiles (`dotnet build` succeeds).

This task proves that Electron.NET works with .NET 8 on the current Windows environment — a critical gate since Electron.NET has known compatibility issues with newer .NET versions and requires Node.js.

## Inputs

- `DocuFiller.csproj — reference for .NET 8 target framework and NuGet package versions`

## Expected Output

- `poc/electron-net-docufiller/electron-net-docufiller.csproj — ASP.NET Core + Electron.NET project file targeting net8.0`
- `poc/electron-net-docufiller/Program.cs — Entry point configuring Electron.NET hosting`
- `poc/electron-net-docufiller/Startup.cs — ASP.NET Core startup with ElectronBootstrap`
- `poc/electron-net-docufiller/wwwroot/index.html — Basic HTML frontend`
- `poc/electron-net-docufiller/electron.manifest.json — Electron.NET manifest configuration`

## Verification

cd poc/electron-net-docufiller && dotnet build
