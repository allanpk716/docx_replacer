(function () {
    'use strict';

    const SIDECAR_URL = 'http://localhost:5000';

    // DOM elements
    const sidecarStatus = document.getElementById('sidecar-status');
    const statusText = sidecarStatus.querySelector('.status-text');
    const selectFileBtn = document.getElementById('select-file-btn');
    const startSidecarBtn = document.getElementById('start-sidecar-btn');
    const startBtn = document.getElementById('start-btn');
    const filePathEl = document.getElementById('file-path');
    const progressContainer = document.getElementById('progress-container');
    const progressFill = document.getElementById('progress-fill');
    const progressTextEl = document.getElementById('progress-text');
    const eventLog = document.getElementById('event-log');

    let selectedFilePath = null;
    let sidecarHealthy = false;

    // --- Utility ---
    function log(message, type) {
        type = type || 'info';
        var entry = document.createElement('div');
        entry.className = 'log-entry ' + type;
        var now = new Date();
        var ts = now.toLocaleTimeString();
        entry.textContent = '[' + ts + '] ' + message;
        eventLog.appendChild(entry);
        eventLog.scrollTop = eventLog.scrollHeight;
    }

    function setSidecarStatus(state, text) {
        sidecarStatus.className = 'status-bar ' + state;
        statusText.textContent = text;
    }

    // --- Sidecar Health Check ---
    async function checkSidecarHealth() {
        setSidecarStatus('checking', 'Checking sidecar...');
        try {
            var resp = await fetch(SIDECAR_URL + '/api/health');
            if (resp.ok) {
                var data = await resp.json();
                setSidecarStatus('connected', 'Sidecar connected (v' + data.version + ')');
                sidecarHealthy = true;
                return true;
            }
        } catch (e) {
            // Sidecar not reachable
        }
        setSidecarStatus('disconnected', 'Sidecar not running \u2014 start with: cd sidecar-dotnet && dotnet run');
        sidecarHealthy = false;
        return false;
    }

    // --- Start Sidecar ---
    startSidecarBtn.addEventListener('click', async function () {
        if (!window.__TAURI__) {
            log('Tauri runtime required to start sidecar', 'error');
            return;
        }
        startSidecarBtn.disabled = true;
        log('Starting .NET sidecar...', 'info');
        try {
            var invoke = window.__TAURI__.core.invoke;
            var result = await invoke('start_sidecar');
            log(result, 'success');
            // Wait a moment for sidecar to start, then check health
            setTimeout(checkSidecarHealth, 2000);
        } catch (e) {
            log('Failed to start sidecar: ' + e, 'error');
        } finally {
            startSidecarBtn.disabled = false;
        }
    });

    // --- File Selection ---
    selectFileBtn.addEventListener('click', async function () {
        if (!window.__TAURI__) {
            log('Tauri runtime required for file dialog', 'error');
            return;
        }
        try {
            var invoke = window.__TAURI__.core.invoke;
            var result = await invoke('open_file_dialog');
            if (result) {
                selectedFilePath = result;
                filePathEl.textContent = result;
                filePathEl.classList.add('selected');
                startBtn.disabled = false;
                log('Selected file: ' + result, 'info');
            } else {
                log('File selection cancelled', 'info');
            }
        } catch (e) {
            log('Dialog error: ' + e, 'error');
        }
    });

    // --- Start Processing (SSE) ---
    startBtn.addEventListener('click', async function () {
        if (!selectedFilePath) return;

        startBtn.disabled = true;
        selectFileBtn.disabled = true;
        progressContainer.classList.remove('hidden');
        progressFill.style.width = '0%';
        progressTextEl.textContent = '0%';
        log('Starting processing: ' + selectedFilePath, 'info');

        try {
            var encodedPath = encodeURIComponent(selectedFilePath);
            var resp = await fetch(SIDECAR_URL + '/api/process/stream?filePath=' + encodedPath);

            if (!resp.ok) {
                throw new Error('HTTP ' + resp.status + ': ' + resp.statusText);
            }

            var reader = resp.body.getReader();
            var decoder = new TextDecoder();
            var buffer = '';

            while (true) {
                var chunk = await reader.read();
                if (chunk.done) break;

                buffer += decoder.decode(chunk.value, { stream: true });

                // Parse SSE events from buffer
                var lines = buffer.split('\n');
                buffer = lines.pop() || '';

                for (var i = 0; i < lines.length; i++) {
                    var line = lines[i];
                    if (line.indexOf('data: ') === 0) {
                        try {
                            var data = JSON.parse(line.slice(6));
                            handleProgressEvent(data);
                        } catch (parseErr) {
                            // Ignore malformed SSE data lines
                        }
                    }
                }
            }

            log('Processing complete!', 'success');
        } catch (e) {
            log('Processing error: ' + e.message, 'error');
        } finally {
            startBtn.disabled = false;
            selectFileBtn.disabled = false;
        }
    });

    function handleProgressEvent(data) {
        var progress = data.progress || 0;
        progressFill.style.width = progress + '%';
        progressTextEl.textContent = progress + '%';

        if (data.step === 'Complete') {
            log('\u2705 ' + data.step + ': ' + (data.output || 'done'), 'success');
        } else if (data.step === 'Starting') {
            log('\u23f3 Processing ' + data.fileName + '...', 'step');
        } else {
            log('\u2699\ufe0f ' + data.step + ' (' + progress + '%)', 'step');
        }
    }

    // --- Init ---
    // Check Tauri runtime
    if (!window.__TAURI__) {
        log('\u26a0\ufe0f Tauri runtime not detected. Running in browser mode.', 'error');
        log('For full functionality, run via: cargo tauri dev', 'info');
    } else {
        log('Tauri runtime loaded', 'success');
    }

    // Check sidecar status on load
    setTimeout(checkSidecarHealth, 500);
})();
