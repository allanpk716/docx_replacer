use tauri_plugin_dialog::DialogExt;

/// Opens a native file dialog for selecting .docx or .xlsx files.
/// Returns the selected file path as a string, or None if the user cancelled.
#[tauri::command]
fn open_file_dialog(app: tauri::AppHandle) -> Result<Option<String>, String> {
    let file_path = app
        .dialog()
        .file()
        .add_filter("Documents", &["docx", "xlsx"])
        .set_title("Select a document to process")
        .blocking_pick_file();
    Ok(file_path.map(|p| p.to_string()))
}

/// Starts the .NET sidecar process (dotnet run --project ../sidecar-dotnet).
/// Returns the PID of the started process.
/// Note: For the PoC, the sidecar can also be started manually with:
///   cd sidecar-dotnet && dotnet run
#[tauri::command]
fn start_sidecar() -> Result<String, String> {
    let child = std::process::Command::new("dotnet")
        .args(["run", "--project", "../sidecar-dotnet"])
        .spawn()
        .map_err(|e| format!("Failed to start sidecar: {}", e))?;
    Ok(format!("Sidecar started with PID {}", child.id()))
}

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    tauri::Builder::default()
        .plugin(tauri_plugin_dialog::init())
        .plugin(tauri_plugin_shell::init())
        .invoke_handler(tauri::generate_handler![open_file_dialog, start_sidecar])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
