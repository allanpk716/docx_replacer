# S02: CleanupViewModel 统一 + CT.Mvvm 迁移 — UAT

**Milestone:** M021
**Written:** 2026-05-04T10:52:17.934Z

# UAT: S02 CleanupViewModel 统一 + CT.Mvvm 迁移

## UAT Type
Integration — verifies that shared CleanupViewModel correctly routes both MainWindow Tab and standalone CleanupWindow interactions.

## Not Proven By This UAT
- Runtime visual verification of WPF windows (requires display)
- Actual file cleanup end-to-end with real .docx files

## Test Cases

### TC1: MainWindow cleanup Tab displays and binds correctly
**Precondition:** Application launched, cleanup Tab visible
**Steps:**
1. Click cleanup Tab
2. Verify output directory text box shows default path "Documents\DocuFiller输出\清理"
3. Verify file list is empty and "开始清理" button is disabled
**Expected:** All bindings resolve correctly, no binding errors in debug output

### TC2: Drag-drop files onto cleanup Tab
**Precondition:** Application launched, cleanup Tab active
**Steps:**
1. Drag a .docx file onto the cleanup drop zone border
2. Verify file appears in the file list
3. Verify "开始清理" button becomes enabled
4. Drag a folder onto the drop zone
5. Verify all .docx files from folder appear in list
**Expected:** cleanupVM.AddFiles and cleanupVM.AddFolder called correctly, UI updates

### TC3: Browse output directory dialog
**Precondition:** Application launched, cleanup Tab active
**Steps:**
1. Click "浏览" (Browse) button next to output directory
2. Select a directory in the folder dialog
3. Verify output directory text updates to selected path
**Expected:** OpenFolderDialog opens, OutputDirectory property updates

### TC4: In-place cleanup via CleanupWindow (standalone)
**Precondition:** Application launched
**Steps:**
1. Open CleanupWindow (via menu or code path)
2. Add a .docx file
3. Click "开始清理"
4. Verify cleanup runs in-place (no separate output directory)
**Expected:** CleanupWindow uses CleanupViewModel with empty OutputDirectory, dispatches to 1-param CleanupAsync

### TC5: Output-directory cleanup via MainWindow Tab
**Precondition:** Application launched, cleanup Tab active, file added
**Steps:**
1. Set output directory to a valid path
2. Click "开始清理"
3. Verify cleanup runs with output directory
4. Click "打开输出文件夹"
**Expected:** Output directory created if needed, cleaned files written there, folder opens

### TC6: MainWindowViewModel has no cleanup leakage
**Precondition:** Source code access
**Steps:**
1. Search MainWindowViewModel.cs for any cleanup-related identifiers
2. Search for IDocumentCleanupService dependency
**Expected:** Zero matches for cleanup fields, properties, commands, methods, or service dependency
