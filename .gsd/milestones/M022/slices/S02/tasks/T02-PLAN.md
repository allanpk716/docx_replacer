---
estimated_steps: 33
estimated_files: 6
skills_used: []
---

# T02: Implement mini DocuFiller PoC with native dialog, sidecar IPC, and progress bar

Build the mini DocuFiller PoC on top of T01's scaffold. Implement the complete interaction flow: user clicks to select a file via Tauri native dialog → Tauri command sends file path to .NET sidecar HTTP API → sidecar simulates processing with SSE progress updates → frontend displays real-time progress bar.

This task proves the Tauri ↔ .NET sidecar integration pattern works, which is the core research question for this slice.

Steps:
1. Implement Tauri Rust commands in src-tauri/src/lib.rs:
   - `open_file_dialog`: Use tauri-plugin-dialog to show native file picker (filter .docx/.xlsx)
   - `start_sidecar`: Use tauri-plugin-shell to launch the .NET sidecar process
   Return results via Tauri command response
2. Implement .NET sidecar HTTP API in sidecar-dotnet/Program.cs:
   - POST /api/process?filePath=...: Accept file path, start simulated processing
   - GET /api/process/stream?filePath=...: SSE endpoint streaming progress events (0→25→50→75→100%)
   - GET /api/health: Health check
   - SimulatedProcessor: 5-step mock pipeline (like Electron.NET PoC), each step reports progress
3. Build frontend UI in src/index.html + src/app.js + src/styles.css:
   - File selection button (calls Tauri invoke('open_file_dialog'))
   - Start processing button (triggers SSE fetch to .NET sidecar)
   - HTML5 progress bar updating in real-time from SSE events
   - Event log area showing processing steps
   - Sidecar connection status indicator
4. Configure Tauri CSP in tauri.conf.json to allow:
   - connect-src http://localhost:5000 (sidecar API)
   - script-src 'self' (frontend JS)
5. Verify `dotnet build` and `cargo build` both succeed with all new code

Must-Haves:
- [ ] Tauri native file dialog working (via tauri-plugin-dialog)
- [ ] .NET sidecar HTTP API with SSE progress streaming
- [ ] Frontend progress bar updating from SSE events
- [ ] Complete flow: select file → process → see progress → completion
- [ ] Both projects compile without errors

Key constraints:
- SSE progress pattern must match Electron.NET PoC (for fair S05 comparison)
- Sidecar port: 5000 (fixed, documented as PoC limitation)
- Use Tauri's invoke() API for Rust commands, direct fetch() for .NET sidecar HTTP
- No modification to any files outside poc/tauri-docufiller/

## Inputs

- `poc/tauri-docufiller/src-tauri/src/lib.rs — T01 scaffold with basic plugin setup`
- `poc/tauri-docufiller/src-tauri/tauri.conf.json — T01 Tauri config with CSP and plugin settings`
- `poc/tauri-docufiller/src/index.html — T01 minimal HTML frontend`
- `poc/tauri-docufiller/sidecar-dotnet/Program.cs — T01 minimal Kestrel server`
- `poc/tauri-docufiller/sidecar-dotnet/sidecar-dotnet.csproj — T01 .NET project file`

## Expected Output

- `poc/tauri-docufiller/src-tauri/src/lib.rs — Tauri commands: open_file_dialog, sidecar management`
- `poc/tauri-docufiller/src/index.html — Full mini DocuFiller UI with file picker, progress bar, status`
- `poc/tauri-docufiller/src/app.js — Frontend JS: Tauri invoke for dialogs, fetch for sidecar, SSE progress`
- `poc/tauri-docufiller/src/styles.css — UI styling for progress bar and layout`
- `poc/tauri-docufiller/sidecar-dotnet/Program.cs — HTTP API with SSE progress endpoint and simulated processor`

## Verification

powershell -Command "cd poc/tauri-docufiller/sidecar-dotnet; dotnet build" (exit code 0) AND cd poc/tauri-docufiller/src-tauri && cargo build (exit code 0)
