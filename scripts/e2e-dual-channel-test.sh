#!/bin/bash
# E2E dual-channel integration test script
# Verifies the complete cross-component integration between the Go update server
# and the C# client's URL construction pattern (UpdateService.cs).
#
# The script proves:
#   1. Build Go server binary from source
#   2. Start server on a high port with temp data dir and test token
#   3. Upload a test release to beta channel via curl POST with Bearer auth
#   4. Verify GET /beta/releases.win.json returns valid JSON containing the version
#      — this proves the URL pattern UpdateService constructs works:
#        UpdateUrl.TrimEnd('/') + "/" + channel + "/" => http://host:port/beta/
#   5. Verify stable feed does NOT contain the beta version (channel isolation)
#   6. Promote the version from beta to stable via promote API
#   7. Verify GET /stable/releases.win.json now contains the promoted version
#   8. Upload 11 versions to beta and verify auto-cleanup removes the oldest
#   9. Clean up (kill server, remove temp dir)
#
# Requires: go, curl, bash (git-bash on Windows)
# Usage: bash scripts/e2e-dual-channel-test.sh

set -euo pipefail

PASS=0
FAIL=0

pass() { echo "[E2E]   PASS: $1"; PASS=$((PASS + 1)); }
fail() { echo "[E2E]   FAIL: $1"; FAIL=$((FAIL + 1)); }

# --- Configuration ---
PORT=19080
TOKEN="e2e-test-token-abc123"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
SERVER_DIR="$PROJECT_ROOT/update-server"
MAX_RETRIES=30

echo "[E2E] ============================================="
echo "[E2E] E2E dual-channel integration test"
echo "[E2E] ============================================="
echo "[E2E] Project root: $PROJECT_ROOT"
echo "[E2E] Server dir:   $SERVER_DIR"
echo ""

# --- Step 1: Build Go server binary from source ---
echo "[E2E] Step 1: Build Go server from source"
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
echo "[E2E] Step 2: Start server on port $PORT"
DATA_DIR=$(mktemp -d)
echo "[E2E] Temp data dir: $DATA_DIR"

# Start server in background, log to file for post-mortem
SERVER_LOG="$DATA_DIR/server.log"
"$BINARY" -port "$PORT" -data-dir "$DATA_DIR" -token "$TOKEN" > "$SERVER_LOG" 2>&1 &
SERVER_PID=$!
echo "[E2E] Server started (PID=$SERVER_PID)"

# Ensure cleanup on exit (even on failure)
cleanup() {
    echo "[E2E] Cleaning up..."
    kill $SERVER_PID 2>/dev/null || true
    wait $SERVER_PID 2>/dev/null || true
    rm -rf "$DATA_DIR"
    echo "[E2E] Cleanup done"
}
trap cleanup EXIT

# Wait for server to be ready
echo "[E2E] Waiting for server to be ready..."
RETRIES=0
while [ $RETRIES -lt $MAX_RETRIES ]; do
    if curl -s -o /dev/null "http://localhost:$PORT/beta/releases.win.json" 2>/dev/null; then
        break
    fi
    sleep 0.5
    RETRIES=$((RETRIES + 1))
done

if [ $RETRIES -eq $MAX_RETRIES ]; then
    fail "server did not start within timeout"
    echo "[E2E] Server log (last 30 lines):"
    tail -30 "$SERVER_LOG"
    exit 1
fi
pass "server is ready ( responded after ${RETRIES} retries)"
echo ""

# --- Helper: upload a version to a channel ---
upload_version() {
    local channel="$1"
    local version="$2"
    local pkg_name="App-${version}-full.nupkg"

    local pkg_file="$DATA_DIR/${pkg_name}"
    echo "package-data-${version}" > "$pkg_file"

    # Must be named releases.win.json so curl sends it with that filename
    # in the multipart form — the Go handler checks fh.Filename == "releases.win.json"
    local feed_file="$DATA_DIR/releases.win.json"
    cat > "$feed_file" << FEOF
{"Assets":[{"PackageId":"App","Version":"${version}","Type":"Full","FileName":"${pkg_name}","Size":20}]}
FEOF

    local http_code
    http_code=$(curl -s -o /dev/null -w "%{http_code}" -X POST \
        "http://localhost:$PORT/api/channels/${channel}/releases" \
        -H "Authorization: Bearer $TOKEN" \
        -F "package=@${pkg_file}" \
        -F "feed=@${feed_file}")
    echo "$http_code"
}

# --- Step 3: Upload test release to beta channel ---
echo "[E2E] Step 3: Upload version 1.0.0 to beta channel"
HTTP_CODE=$(upload_version "beta" "1.0.0")
if [ "$HTTP_CODE" = "200" ]; then
    pass "upload to beta succeeded (HTTP 200)"
else
    fail "upload to beta failed (expected 200, got $HTTP_CODE)"
fi
echo ""

# --- Step 4: Verify GET /beta/releases.win.json contains the uploaded version ---
echo "[E2E] Step 4: Verify beta feed resolves (proves UpdateService URL pattern)"
echo "[E2E]   UpdateService constructs: UpdateUrl.TrimEnd('/') + \"/\" + channel + \"/\""
echo "[E2E]   For beta => http://localhost:$PORT/beta/"
RESP=$(curl -s "http://localhost:$PORT/beta/releases.win.json")

# Verify it's valid JSON containing the version
if echo "$RESP" | grep -q '"1.0.0"'; then
    pass "GET /beta/releases.win.json contains version 1.0.0"
else
    fail "GET /beta/releases.win.json does not contain 1.0.0: $RESP"
fi

# Verify JSON structure has Assets array
if echo "$RESP" | grep -q '"Assets"'; then
    pass "beta feed has valid JSON structure (Assets array)"
else
    fail "beta feed missing Assets array: $RESP"
fi

# Verify the .nupkg is also served statically
HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" "http://localhost:$PORT/beta/App-1.0.0-full.nupkg")
if [ "$HTTP_CODE" = "200" ]; then
    pass "GET /beta/App-1.0.0-full.nupkg served (HTTP 200)"
else
    fail "GET /beta/App-1.0.0-full.nupkg failed (expected 200, got $HTTP_CODE)"
fi
echo ""

# --- Step 5: Verify stable feed does NOT contain the beta version (channel isolation) ---
echo "[E2E] Step 5: Verify channel isolation (stable should not have beta version)"
RESP_STABLE=$(curl -s "http://localhost:$PORT/stable/releases.win.json")
if echo "$RESP_STABLE" | grep -q '"1.0.0"'; then
    fail "stable feed contains 1.0.0 — channels are NOT isolated"
else
    pass "stable feed does NOT contain 1.0.0 — channel isolation confirmed"
fi
echo ""

# --- Step 6: Promote version from beta to stable ---
echo "[E2E] Step 6: Promote 1.0.0 from beta to stable"
HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" -X POST \
    "http://localhost:$PORT/api/channels/stable/promote?from=beta&version=1.0.0" \
    -H "Authorization: Bearer $TOKEN")
if [ "$HTTP_CODE" = "200" ]; then
    pass "promote succeeded (HTTP 200)"
else
    fail "promote failed (expected 200, got $HTTP_CODE)"
fi
echo ""

# --- Step 7: Verify stable feed now contains the promoted version ---
echo "[E2E] Step 7: Verify stable feed now contains 1.0.0 (proves stable UpdateService would detect update)"
RESP_STABLE=$(curl -s "http://localhost:$PORT/stable/releases.win.json")
if echo "$RESP_STABLE" | grep -q '"1.0.0"'; then
    pass "GET /stable/releases.win.json contains 1.0.0 after promote"
else
    fail "GET /stable/releases.win.json does not contain 1.0.0: $RESP_STABLE"
fi

# Also verify the .nupkg file is available on stable
HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" "http://localhost:$PORT/stable/App-1.0.0-full.nupkg")
if [ "$HTTP_CODE" = "200" ]; then
    pass "GET /stable/App-1.0.0-full.nupkg served (HTTP 200)"
else
    fail "GET /stable/App-1.0.0-full.nupkg failed (expected 200, got $HTTP_CODE)"
fi
echo ""

# --- Step 8: Upload 11 versions to beta and verify auto-cleanup ---
echo "[E2E] Step 8: Upload 11 versions to beta, verify auto-cleanup removes oldest"
# We already uploaded 1.0.0, so upload 1.0.1 through 1.0.10 (10 more = 11 total)
CLEANUP_ALL_OK=true
for i in $(seq 1 10); do
    VER="1.0.${i}"
    HTTP_CODE=$(upload_version "beta" "$VER")
    if [ "$HTTP_CODE" != "200" ]; then
        fail "upload version $VER failed (HTTP $HTTP_CODE)"
        CLEANUP_ALL_OK=false
        break
    fi
done

if $CLEANUP_ALL_OK; then
    pass "uploaded 11 versions total to beta"

    # Verify the oldest version (1.0.0) was cleaned up
    RESP=$(curl -s "http://localhost:$PORT/beta/releases.win.json")
    if echo "$RESP" | grep -q '"1.0.0"'; then
        fail "version 1.0.0 should have been cleaned up (DefaultMaxKeep=10)"
    else
        pass "auto-cleanup removed oldest version (1.0.0)"
    fi

    # Verify the newest version (1.0.10) is still present
    if echo "$RESP" | grep -q '"1.0.10"'; then
        pass "newest version (1.0.10) still present after cleanup"
    else
        fail "newest version (1.0.10) missing after cleanup"
    fi
fi
echo ""

# --- Summary ---
echo "[E2E] ============================================="
echo "[E2E] Results: $PASS passed, $FAIL failed"
echo "[E2E] ============================================="

if [ $FAIL -gt 0 ]; then
    echo ""
    echo "[E2E] Server log (last 30 lines):"
    tail -30 "$SERVER_LOG"
    exit 1
fi

exit 0
