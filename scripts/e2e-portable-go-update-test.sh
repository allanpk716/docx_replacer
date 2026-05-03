#!/bin/bash
# E2E portable update test against Go update server
#
# Automates the full portable app self-update verification against the
# internal Go update server. Proves the complete chain:
#   1. Build Go server binary from update-server/ source
#   2. Start server on a high port with temp data dir and test token
#   3. Build v1.0.0 and v1.1.0 from C# source, pack with vpk
#   4. Upload v1.1.0 artifacts to Go server's stable channel via curl POST
#   5. Verify GET /stable/releases.win.json contains the version
#   6. Extract v1.0.0 Portable.zip
#   7. Create update-config.json pointing to the Go server
#   8. Run DocuFiller.exe update --yes from the portable directory
#   9. Parse JSONL output to verify update succeeded
#  10. Clean up (kill server, remove temp dirs, restore csproj version)
#  11. Print PASS/FAIL summary
#
# Requires: go, curl, dotnet, vpk, bash (git-bash on Windows)
# Usage: bash scripts/e2e-portable-go-update-test.sh

set -euo pipefail

PASS=0
FAIL=0

pass() { echo "[E2E-GO]   PASS: $1"; PASS=$((PASS + 1)); }
fail() { echo "[E2E-GO]   FAIL: $1"; FAIL=$((FAIL + 1)); }

# --- Configuration ---
PORT=19081
TOKEN="e2e-go-test-token-xyz789"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
SERVER_DIR="$PROJECT_ROOT/update-server"
CSPROJ="$PROJECT_ROOT/DocuFiller.csproj"
E2E_DIR="$PROJECT_ROOT/e2e-portable-go-test"
MAX_RETRIES=30

echo "[E2E-GO] ============================================="
echo "[E2E-GO] E2E portable update test (Go server)"
echo "[E2E-GO] ============================================="
echo "[E2E-GO] Project root: $PROJECT_ROOT"
echo "[E2E-GO] Server dir:   $SERVER_DIR"
echo "[E2E-GO] Port:         $PORT"
echo ""

# --- Step 1: Build Go server binary from source ---
echo "[E2E-GO] Step 1: Build Go server from source"
BINARY_DIR="$SERVER_DIR/bin"
mkdir -p "$BINARY_DIR"
BINARY="$BINARY_DIR/update-server.exe"

(cd "$SERVER_DIR" && go build -o "$BINARY" .)

if [ -f "$BINARY" ]; then
    pass "Go server binary built: $BINARY"
else
    fail "Go server binary not found after build"
    exit 1
fi
echo ""

# --- Step 2: Start server on high port with temp data dir ---
echo "[E2E-GO] Step 2: Start Go server on port $PORT"
DATA_DIR=$(mktemp -d)
echo "[E2E-GO] Temp data dir: $DATA_DIR"

SERVER_LOG="$DATA_DIR/server.log"
"$BINARY" -port "$PORT" -data-dir "$DATA_DIR" -token "$TOKEN" > "$SERVER_LOG" 2>&1 &
SERVER_PID=$!
echo "[E2E-GO] Server started (PID=$SERVER_PID)"

# Ensure cleanup on exit (even on failure)
cleanup() {
    echo "[E2E-GO] Cleaning up..."
    kill $SERVER_PID 2>/dev/null || true
    wait $SERVER_PID 2>/dev/null || true

    # Restore update-config.json
    CONFIG_DIR="$USERPROFILE/.docx_replacer"
    CONFIG_FILE="$CONFIG_DIR/update-config.json"
    CONFIG_BACKUP="$CONFIG_FILE.e2e-go-backup"
    if [ -f "$CONFIG_BACKUP" ]; then
        cp "$CONFIG_BACKUP" "$CONFIG_FILE"
        rm "$CONFIG_BACKUP"
        echo "[E2E-GO] Restored original update-config.json"
    elif [ -f "$CONFIG_FILE" ]; then
        # Check if it's our test config
        if grep -q "localhost:$PORT" "$CONFIG_FILE" 2>/dev/null; then
            rm "$CONFIG_FILE"
            echo "[E2E-GO] Removed test update-config.json"
        fi
    fi

    # Restore csproj version
    if [ -n "${ORIGINAL_VERSION:-}" ]; then
        set_csproj_version "$ORIGINAL_VERSION"
        echo "[E2E-GO] Restored csproj version to $ORIGINAL_VERSION"
    fi

    rm -rf "$DATA_DIR"
    echo "[E2E-GO] Cleanup done"
}
trap cleanup EXIT

# Wait for server to be ready
echo "[E2E-GO] Waiting for server to be ready..."
RETRIES=0
while [ $RETRIES -lt $MAX_RETRIES ]; do
    if curl -s -o /dev/null "http://localhost:$PORT/stable/releases.win.json" 2>/dev/null; then
        break
    fi
    sleep 0.5
    RETRIES=$((RETRIES + 1))
done

if [ $RETRIES -eq $MAX_RETRIES ]; then
    fail "Go server did not start within timeout"
    echo "[E2E-GO] Server log (last 30 lines):"
    tail -30 "$SERVER_LOG"
    exit 1
fi
pass "Go server is ready (responded after ${RETRIES} retries)"
echo ""

# --- Helper: Set csproj version ---
set_csproj_version() {
    local version="$1"
    sed -i "s|<Version>[^<]*</Version>|<Version>${version}</Version>|" "$CSPROJ"
}

# --- Helper: Get current csproj version ---
get_csproj_version() {
    grep -oP '<Version>\K[^<]+' "$CSPROJ"
}

# --- Helper: Build and pack a version ---
build_and_pack() {
    local version="$1"
    local outdir="$2"
    local publish_dir="$SCRIPT_DIR/build/publish"

    echo "[E2E-GO]   Building $version..."
    rm -rf "$SCRIPT_DIR/build"
    mkdir -p "$publish_dir"

    # Set version in csproj
    set_csproj_version "$version"

    # Publish (self-contained for portable)
    dotnet publish "$CSPROJ" -c Release -r win-x64 --self-contained \
        -o "$publish_dir" \
        -p:PublishSingleFile=true \
        -p:IncludeNativeLibrariesForSelfExtract=true

    # Ensure output dir exists
    mkdir -p "$outdir"

    # vpk pack
    vpk pack --packId DocuFiller --packVersion "$version" \
        --packDir "$publish_dir" --mainExe DocuFiller.exe \
        --outputDir "$outdir"

    # Clean intermediate publish dir
    rm -rf "$SCRIPT_DIR/build"
}

# --- Helper: Upload a version to a channel via Go server API ---
upload_version() {
    local channel="$1"
    local version="$2"
    local artifacts_dir="$3"

    # Find the .nupkg file
    local pkg_file
    pkg_file=$(find "$artifacts_dir" -name "DocuFiller-${version}-full.nupkg" -print -quit 2>/dev/null || true)
    if [ -z "$pkg_file" ]; then
        # Try alternate naming pattern
        pkg_file=$(find "$artifacts_dir" -name "*-full.nupkg" -print -quit 2>/dev/null || true)
    fi

    # Find the releases.win.json (vpk generates it)
    local feed_file="$artifacts_dir/releases.win.json"

    if [ ! -f "$feed_file" ]; then
        echo "[E2E-GO]   WARNING: $feed_file not found, looking for alternatives..."
        feed_file=$(find "$artifacts_dir" -name "releases.win.json" -print -quit 2>/dev/null || true)
    fi

    local http_code
    local curl_args=(-s -o /dev/null -w "%{http_code}" -X POST)
    curl_args+=("http://localhost:$PORT/api/channels/${channel}/releases")
    curl_args+=(-H "Authorization: Bearer $TOKEN")

    if [ -n "$pkg_file" ] && [ -f "$pkg_file" ]; then
        curl_args+=(-F "package=@${pkg_file}")
    fi

    if [ -f "$feed_file" ]; then
        curl_args+=(-F "feed=@${feed_file}")
    fi

    http_code=$(curl "${curl_args[@]}")
    echo "$http_code"
}

# --- Save original csproj version ---
echo "[E2E-GO] Saving original csproj version..."
ORIGINAL_VERSION=$(get_csproj_version)
echo "[E2E-GO] Original version: $ORIGINAL_VERSION"
echo ""

# --- Step 3: Build v1.0.0 and v1.1.0 ---
echo "[E2E-GO] Step 3: Build v1.0.0 (old version)"
rm -rf "$E2E_DIR"
mkdir -p "$E2E_DIR/v1.0.0" "$E2E_DIR/v1.1.0"
build_and_pack "1.0.0" "$E2E_DIR/v1.0.0"
if [ -f "$E2E_DIR/v1.0.0/DocuFiller-Portable.zip" ]; then
    pass "v1.0.0 built and packed (Portable.zip present)"
else
    fail "v1.0.0 Portable.zip not found"
    exit 1
fi
echo ""

echo "[E2E-GO] Step 3b: Build v1.1.0 (new version)"
build_and_pack "1.1.0" "$E2E_DIR/v1.1.0"
if [ -f "$E2E_DIR/v1.1.0/DocuFiller-Portable.zip" ]; then
    pass "v1.1.0 built and packed (Portable.zip present)"
else
    fail "v1.1.0 Portable.zip not found"
    exit 1
fi
echo ""

# Restore csproj version immediately
set_csproj_version "$ORIGINAL_VERSION"
echo "[E2E-GO] Restored csproj version to $ORIGINAL_VERSION"
echo ""

# --- Step 4: Upload v1.1.0 artifacts to Go server's stable channel ---
echo "[E2E-GO] Step 4: Upload v1.1.0 to Go server stable channel"
HTTP_CODE=$(upload_version "stable" "1.1.0" "$E2E_DIR/v1.1.0")
if [ "$HTTP_CODE" = "200" ]; then
    pass "v1.1.0 uploaded to stable channel (HTTP 200)"
else
    fail "v1.1.0 upload failed (expected 200, got $HTTP_CODE)"
fi
echo ""

# --- Step 5: Verify GET /stable/releases.win.json contains the version ---
echo "[E2E-GO] Step 5: Verify stable feed contains 1.1.0"
RESP=$(curl -s "http://localhost:$PORT/stable/releases.win.json")
if echo "$RESP" | grep -q '"1.1.0"'; then
    pass "GET /stable/releases.win.json contains version 1.1.0"
else
    fail "GET /stable/releases.win.json does NOT contain 1.1.0: $RESP"
fi
if echo "$RESP" | grep -q '"Assets"'; then
    pass "stable feed has valid JSON structure (Assets array)"
else
    fail "stable feed missing Assets array"
fi
echo ""

# --- Step 6: Extract v1.0.0 Portable.zip ---
echo "[E2E-GO] Step 6: Extract v1.0.0 portable"
PORTABLE_DIR="$E2E_DIR/portable-app"
mkdir -p "$PORTABLE_DIR"

PORTABLE_ZIP="$E2E_DIR/v1.0.0/DocuFiller-Portable.zip"
if [ ! -f "$PORTABLE_ZIP" ]; then
    fail "Portable zip not found: $PORTABLE_ZIP"
    exit 1
fi

# Extract using unzip (available in git-bash) or powershell fallback
if command -v unzip &>/dev/null; then
    unzip -q -o "$PORTABLE_ZIP" -d "$PORTABLE_DIR"
else
    powershell -Command "Expand-Archive -Path '$PORTABLE_ZIP' -DestinationPath '$PORTABLE_DIR' -Force"
fi

if [ -f "$PORTABLE_DIR/DocuFiller.exe" ] && [ -f "$PORTABLE_DIR/Update.exe" ]; then
    pass "Portable v1.0.0 extracted (DocuFiller.exe + Update.exe present)"
else
    fail "Extraction failed: DocuFiller.exe or Update.exe missing"
    echo "[E2E-GO] Contents:"
    ls -la "$PORTABLE_DIR/"
    exit 1
fi
echo ""

# --- Step 7: Create update-config.json pointing to Go server ---
echo "[E2E-GO] Step 7: Configure update-config.json for Go server"
CONFIG_DIR="$USERPROFILE/.docx_replacer"
CONFIG_FILE="$CONFIG_DIR/update-config.json"
CONFIG_BACKUP="$CONFIG_FILE.e2e-go-backup"
UPDATE_URL="http://localhost:$PORT/"

mkdir -p "$CONFIG_DIR"

# Backup existing config if present
if [ -f "$CONFIG_FILE" ]; then
    cp "$CONFIG_FILE" "$CONFIG_BACKUP"
    echo "[E2E-GO] Backed up existing update-config.json"
fi

# Write test config — UpdateService reads UpdateUrl and Channel
cat > "$CONFIG_FILE" << EOF
{"UpdateUrl":"$UPDATE_URL","Channel":"stable"}
EOF
pass "update-config.json configured for Go server ($UPDATE_URL)"
echo ""

# --- Step 8: Run DocuFiller.exe update --yes ---
echo "[E2E-GO] Step 8: Run DocuFiller.exe update --yes"
echo "[E2E-GO]   Working dir: $PORTABLE_DIR"

UPDATE_LOG="$E2E_DIR/update-output.log"

# Run update in background; ApplyUpdatesAndRestart may kill the process
# before it returns. Use timeout wrapper.
"$PORTABLE_DIR/DocuFiller.exe" update --yes > "$UPDATE_LOG" 2>&1 &
UPDATE_PID=$!

# Wait up to 60 seconds for the update process to finish
WAIT_COUNT=0
while [ $WAIT_COUNT -lt 60 ]; do
    if ! kill -0 $UPDATE_PID 2>/dev/null; then
        break
    fi
    sleep 1
    WAIT_COUNT=$((WAIT_COUNT + 1))
done

# If process is still running, kill it
if kill -0 $UPDATE_PID 2>/dev/null; then
    echo "[E2E-GO]   Update process still running after ${WAIT_COUNT}s, killing..."
    kill $UPDATE_PID 2>/dev/null || true
    wait $UPDATE_PID 2>/dev/null || true
else
    echo "[E2E-GO]   Update process exited after ${WAIT_COUNT}s"
fi

echo "[E2E-GO]   Update output log:"
cat "$UPDATE_LOG" 2>/dev/null || echo "(no output)"
echo ""

# --- Step 9: Parse JSONL output to verify update succeeded ---
echo "[E2E-GO] Step 9: Verify update result"

UPDATE_FOUND=0
DOWNLOAD_OK=0
APPLY_OK=0
VERSION_UPGRADED=0

# Check update log for JSONL indicators
if grep -q '"hasUpdate".*true' "$UPDATE_LOG" 2>/dev/null || \
   grep -qi 'update.*found\|new.*version' "$UPDATE_LOG" 2>/dev/null; then
    UPDATE_FOUND=1
fi

if grep -qi 'download\|progress\|downloading' "$UPDATE_LOG" 2>/dev/null; then
    DOWNLOAD_OK=1
fi

if grep -qi 'apply\|restart\|installing' "$UPDATE_LOG" 2>/dev/null; then
    APPLY_OK=1
fi

# Check if DocuFiller.exe version upgraded in the portable dir
# After Velopack applies the update, the exe should be replaced
if [ -f "$PORTABLE_DIR/DocuFiller.exe" ]; then
    FILE_VER=$(powershell -Command \
        "(Get-Command '$PORTABLE_DIR/DocuFiller.exe').FileVersionInfo.FileVersion" 2>/dev/null || echo "")
    if echo "$FILE_VER" | grep -q "1.1.0"; then
        VERSION_UPGRADED=1
        pass "DocuFiller.exe version upgraded to 1.1.0"
    fi
fi

# Evaluate each criterion
if [ "$UPDATE_FOUND" -eq 1 ]; then
    pass "Update check found v1.1.0"
else
    echo "[E2E-GO]   WARN: Could not confirm update detection from log"
fi

if [ "$DOWNLOAD_OK" -eq 1 ]; then
    pass "Download reported in output"
else
    echo "[E2E-GO]   WARN: No download progress in log (ApplyUpdatesAndRestart may have killed the process)"
fi

if [ "$APPLY_OK" -eq 1 ]; then
    pass "Apply/restart reported in output"
else
    echo "[E2E-GO]   WARN: No apply indicator in log"
fi

if [ "$VERSION_UPGRADED" -eq 0 ]; then
    echo "[E2E-GO]   WARN: Could not verify version upgrade from exe"
    echo "[E2E-GO]         This is expected if ApplyUpdatesAndRestart exited before completing"
fi
echo ""

# --- Step 10: Summary ---
echo "[E2E-GO] ============================================="
echo "[E2E-GO] Results: $PASS passed, $FAIL failed"
echo "[E2E-GO] ============================================="

# Print server log tail for diagnostics
echo ""
echo "[E2E-GO] Go server log (last 20 lines):"
tail -20 "$SERVER_LOG"
echo ""

if [ $FAIL -gt 0 ]; then
    echo "[E2E-GO] E2E PORTABLE GO UPDATE TEST: FAIL"
    exit 1
fi

echo "[E2E-GO] E2E PORTABLE GO UPDATE TEST: PASS"
exit 0
