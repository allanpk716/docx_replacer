---
estimated_steps: 10
estimated_files: 6
skills_used: []
---

# T02: Implement mini DocuFiller PoC with IPC, file dialog, and progress bar

Build the mini DocuFiller PoC on top of T01's scaffold. Implement:

1. **Frontend UI** (wwwroot/index.html): File selection button triggering native file dialog, progress bar (HTML5 <progress> or styled div), status text area showing processing results. Use fetch() calls to ASP.NET Core API endpoints.

2. **Backend API Controllers**: A ProcessingController with endpoints for: selecting files (triggers Electron native OpenFileDialog via ElectronNET.API), starting simulated processing, and getting progress updates.

3. **IPC Communication**: Use Electron.IpcMain.On to handle messages from the renderer process. Demonstrate bidirectional IPC: frontend sends "select-file" → backend opens native dialog and returns path; frontend sends "start-processing" → backend simulates work with progress updates via window.sendMessage or SSE.

4. **Simulated Processing**: A simple loop that simulates document processing (e.g., copies file bytes, pretends to process) and reports progress (0%, 25%, 50%, 75%, 100%). No actual OpenXml/EPPlus dependency needed — this is a mock.

Constraints:
- Keep it minimal but representative of DocuFiller's core flow
- Use Electron.NET's native dialog API (not HTML input[type=file]) to prove native integration
- Progress updates must flow from .NET backend to the browser frontend
- No modification to any files outside poc/electron-net-docufiller/

## Inputs

- `poc/electron-net-docufiller/electron-net-docufiller.csproj — project scaffold from T01`
- `poc/electron-net-docufiller/Startup.cs — ASP.NET Core startup from T01`
- `poc/electron-net-docufiller/wwwroot/index.html — basic frontend from T01`

## Expected Output

- `poc/electron-net-docufiller/Controllers/ProcessingController.cs — API controller with file selection and processing endpoints`
- `poc/electron-net-docufiller/Services/SimulatedProcessor.cs — Mock processor simulating document work with progress`
- `poc/electron-net-docufiller/wwwroot/index.html — Full mini DocuFiller UI with file picker, progress bar, status display`
- `poc/electron-net-docufiller/wwwroot/css/app.css — Styling for the PoC UI`
- `poc/electron-net-docufiller/wwwroot/js/app.js — Frontend JS handling IPC and progress updates`

## Verification

cd poc/electron-net-docufiller && dotnet build
