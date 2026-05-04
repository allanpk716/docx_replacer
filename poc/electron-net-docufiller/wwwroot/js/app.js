/**
 * DocuFiller Electron.NET PoC — Frontend Application
 *
 * Demonstrates:
 * 1. Native file dialog via /api/select-file (Electron.NET API)
 * 2. Server-Sent Events (SSE) for progress updates from backend
 * 3. Electron IPC bridge for bidirectional communication
 */

// ---- DOM refs ----
const selectBtn = document.getElementById('selectBtn');
const processBtn = document.getElementById('processBtn');
const cancelBtn = document.getElementById('cancelBtn');
const statusEl = document.getElementById('status');
const fileInfoEl = document.getElementById('fileInfo');
const fileNameEl = document.getElementById('fileName');
const filePathEl = document.getElementById('filePath');
const progressSection = document.getElementById('progressSection');
const progressFill = document.getElementById('progressFill');
const progressPercent = document.getElementById('progressPercent');
const progressStep = document.getElementById('progressStep');
const logArea = document.getElementById('logArea');
const ipcDot = document.getElementById('ipcDot');
const ipcLabel = document.getElementById('ipcLabel');

// ---- State ----
let selectedFilePath = null;
let currentEventSource = null;
let isProcessing = false;

// ---- Logging ----
function log(message, level = 'info') {
    const time = new Date().toLocaleTimeString('zh-CN', { hour12: false });
    const entry = document.createElement('div');
    entry.className = `log-entry log-${level}`;
    entry.innerHTML = `<span class="log-time">[${time}]</span> ${message}`;
    logArea.appendChild(entry);
    logArea.scrollTop = logArea.scrollHeight;
    console.log(`[${level.toUpperCase()}] ${message}`);
}

// ---- Status updates ----
function setStatus(text, level = '') {
    statusEl.textContent = text;
    statusEl.className = `status-text ${level}`;
}

function setProgress(percent, stepText = '') {
    progressFill.style.width = `${percent}%`;
    progressPercent.textContent = `${percent}%`;
    if (stepText) {
        progressStep.textContent = stepText;
    }
}

function showFileInfo(path) {
    const name = path.split(/[\\/]/).pop();
    fileNameEl.textContent = name;
    filePathEl.textContent = path;
    fileInfoEl.classList.add('visible');
}

function hideFileInfo() {
    fileInfoEl.classList.remove('visible');
}

function setProcessingState(processing) {
    isProcessing = processing;
    selectBtn.disabled = processing;
    processBtn.disabled = processing || !selectedFilePath;
    cancelBtn.disabled = !processing;

    if (processing) {
        progressSection.classList.add('visible');
        processBtn.textContent = '⏳ 处理中...';
    } else {
        processBtn.textContent = '🚀 开始处理';
    }
}

// ---- File selection (native dialog via API) ----
async function selectFile() {
    selectBtn.disabled = true;
    setStatus('正在打开文件对话框...');

    try {
        log('调用 /api/select-file 打开原生对话框...');
        const resp = await fetch('/api/select-file');
        const data = await resp.json();

        if (data.path) {
            selectedFilePath = data.path;
            showFileInfo(data.path);
            setStatus(`已选择文件: ${data.path.split(/[\\/]/).pop()}`);
            processBtn.disabled = false;
            log(`文件已选择: ${data.path}`, 'success');
        } else {
            setStatus(data.message || '未选择文件');
            log(data.message || '用户取消选择', 'warn');
        }
    } catch (err) {
        setStatus('错误: ' + err.message, 'error');
        log(`文件选择失败: ${err.message}`, 'error');
    } finally {
        selectBtn.disabled = isProcessing;
    }
}

// ---- Processing via SSE ----
async function startProcessing() {
    if (!selectedFilePath) return;

    setProcessingState(true);
    setStatus('开始处理...');
    setProgress(0, '初始化...');
    log(`开始处理: ${selectedFilePath}`);

    try {
        // Use Server-Sent Events for progress updates
        const encodedPath = encodeURIComponent(selectedFilePath);
        const url = `/api/process?filePath=${encodedPath}`;
        log(`连接 SSE: ${url}`);

        currentEventSource = new EventSource(url);

        currentEventSource.onmessage = (event) => {
            try {
                const data = JSON.parse(event.data);

                switch (data.type) {
                    case 'progress':
                        const stepNames = ['读取文件', '解析文档', '填充模板', '验证输出', '保存结果'];
                        const stepIndex = Math.min(Math.floor(data.percent / 20), stepNames.length - 1);
                        setProgress(data.percent, stepNames[stepIndex]);
                        setStatus(`处理中: ${data.percent}%`);
                        log(`进度: ${data.percent}% — ${stepNames[stepIndex]}`);
                        break;

                    case 'complete':
                        setProgress(100, '完成');
                        if (data.success) {
                            const duration = data.durationMs ? Math.round(data.durationMs) : '?';
                            setStatus(`处理完成! 耗时 ${duration}ms, ${data.totalSteps} 步`, 'success');
                            log(`✅ 处理完成: ${data.fileName}, ${data.fileSizeBytes} bytes, ${duration}ms`, 'success');
                        } else {
                            setStatus(`处理失败: ${data.errorMessage}`, 'error');
                            log(`❌ 处理失败: ${data.errorMessage}`, 'error');
                        }
                        cleanupEventSource();
                        setProcessingState(false);
                        break;

                    case 'error':
                        setStatus(`错误: ${data.message}`, 'error');
                        log(`❌ 错误: ${data.message}`, 'error');
                        cleanupEventSource();
                        setProcessingState(false);
                        break;
                }
            } catch (parseErr) {
                log(`SSE 解析错误: ${parseErr.message}`, 'error');
            }
        };

        currentEventSource.onerror = (err) => {
            log('SSE 连接关闭', 'warn');
            cleanupEventSource();
            if (isProcessing) {
                setStatus('连接中断', 'warning');
                setProcessingState(false);
            }
        };

    } catch (err) {
        setStatus('处理失败: ' + err.message, 'error');
        log(`处理异常: ${err.message}`, 'error');
        setProcessingState(false);
    }
}

function cancelProcessing() {
    cleanupEventSource();
    setStatus('已取消', 'warning');
    log('用户取消处理', 'warn');
    setProcessingState(false);
}

function cleanupEventSource() {
    if (currentEventSource) {
        currentEventSource.close();
        currentEventSource = null;
    }
}

// ---- IPC bridge (Electron-specific) ----
function setupIpcBridge() {
    // Check if running in Electron
    if (typeof window !== 'undefined' && window.__electron_ipc__) {
        log('检测到 Electron IPC 桥接', 'success');
        ipcDot.classList.add('active');
        ipcDot.classList.remove('inactive');
        ipcLabel.textContent = 'Electron IPC: 已连接';
    } else {
        log('运行在浏览器模式 (无 Electron IPC)', 'warn');
        ipcDot.classList.add('inactive');
        ipcDot.classList.remove('active');
        ipcLabel.textContent = 'Electron IPC: 浏览器模式';
    }

    // Check IPC status via API
    fetch('/api/ipc/status')
        .then(r => r.json())
        .then(data => {
            log(`IPC 状态: electronActive=${data.electronActive}, version=${data.version || 'N/A'}`);
            if (data.electronActive) {
                ipcDot.classList.add('active');
                ipcDot.classList.remove('inactive');
                ipcLabel.textContent = `Electron IPC: 已连接 (v${data.version || '?'})`;
            }
        })
        .catch(err => {
            log(`IPC 状态检查失败: ${err.message}`, 'warn');
        });
}

// ---- IPC ping test ----
function testIpcPing() {
    log('发送 IPC ping...');
    fetch('/api/ipc/status')
        .then(r => r.json())
        .then(data => {
            log(`IPC pong 收到: electron=${data.electronActive}, time=${data.timestamp}`, 'success');
        })
        .catch(err => {
            log(`IPC ping 失败: ${err.message}`, 'error');
        });
}

// ---- Init ----
document.addEventListener('DOMContentLoaded', () => {
    log('DocuFiller Electron.NET PoC 已加载');
    log('前端初始化完成，等待用户操作...');
    setupIpcBridge();
});

// Wire up button events
selectBtn.addEventListener('click', selectFile);
processBtn.addEventListener('click', startProcessing);
cancelBtn.addEventListener('click', cancelProcessing);
