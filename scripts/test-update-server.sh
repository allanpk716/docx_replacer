#!/bin/bash
# Integration test script for update-server
# Requires: curl, jq, bash (git-bash on Windows)
# Usage: bash scripts/test-update-server.sh [/path/to/update-server.exe]

set -euo pipefail

PASS=0
FAIL=0
SKIP=0

pass() { echo "  PASS: $1"; PASS=$((PASS+1)); }
fail() { echo "  FAIL: $1"; FAIL=$((FAIL+1)); }
skip() { echo "  SKIP: $1"; SKIP=$((SKIP+1)); }

# Locate binary
BINARY="${1:-}"
if [ -z "$BINARY" ]; then
    # Try common locations
    for candidate in ./update-server/bin/update-server.exe ./bin/update-server.exe ./update-server.exe; do
        if [ -f "$candidate" ]; then
            BINARY="$candidate"
            break
        fi
    done
fi

if [ -z "$BINARY" ] || [ ! -f "$BINARY" ]; then
    echo "ERROR: update-server binary not found."
    echo "Usage: $0 [/path/to/update-server.exe]"
    echo "Build first: cd update-server && go build -o bin/update-server.exe ."
    exit 1
fi

# Check for curl
if ! command -v curl &>/dev/null; then
    echo "ERROR: curl is required but not found in PATH"
    exit 1
fi

# Create temp data directory
DATA_DIR=$(mktemp -d)
trap 'rm -rf "$DATA_DIR"' EXIT

TOKEN="test-integration-token"
PORT=18080

echo "=== update-server integration test ==="
echo "Binary: $BINARY"
echo "Data dir: $DATA_DIR"
echo ""

# Start server in background
"$BINARY" -port "$PORT" -data-dir "$DATA_DIR" -token "$TOKEN" > "$DATA_DIR/server.log" 2>&1 &
SERVER_PID=$!
echo "Server started (PID=$SERVER_PID) on port $PORT"

# Wait for server to be ready
RETRIES=0
MAX_RETRIES=20
while [ $RETRIES -lt $MAX_RETRIES ]; do
    if curl -s -o /dev/null "http://localhost:$PORT/beta/releases.win.json" 2>/dev/null; then
        break
    fi
    sleep 0.5
    RETRIES=$((RETRIES+1))
done

if [ $RETRIES -eq $MAX_RETRIES ]; then
    echo "ERROR: server did not start within timeout"
    cat "$DATA_DIR/server.log"
    kill $SERVER_PID 2>/dev/null || true
    exit 1
fi

echo ""
echo "--- Test 1: Auth rejection (no token) ---"
# Create a dummy file for upload test
echo "dummy" > "$DATA_DIR/dummy.nupkg"
HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" -X POST "http://localhost:$PORT/api/channels/beta/releases" \
    -F "file=@$DATA_DIR/dummy.nupkg")
if [ "$HTTP_CODE" = "401" ]; then pass "no token rejected (401)"; else fail "expected 401, got $HTTP_CODE"; fi

echo ""
echo "--- Test 2: Auth rejection (bad token) ---"
HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" -X POST "http://localhost:$PORT/api/channels/beta/releases" \
    -H "Authorization: Bearer wrong-token" \
    -F "file=@$DATA_DIR/dummy.nupkg")
if [ "$HTTP_CODE" = "401" ]; then pass "bad token rejected (401)"; else fail "expected 401, got $HTTP_CODE"; fi

echo ""
echo "--- Test 3: Upload .nupkg + feed to beta channel ---"
# Create a test .nupkg file
TEST_PKG="$DATA_DIR/App-1.0.0-full.nupkg"
echo "test-package-data-v1" > "$TEST_PKG"

# Create a releases.win.json
TEST_FEED="$DATA_DIR/releases.win.json"
cat > "$TEST_FEED" << 'FEED_EOF'
{"Assets":[{"PackageId":"App","Version":"1.0.0","Type":"Full","FileName":"App-1.0.0-full.nupkg","SHA1":"abc123","Size":21}]}
FEED_EOF

HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" -X POST "http://localhost:$PORT/api/channels/beta/releases" \
    -H "Authorization: Bearer $TOKEN" \
    -F "package=@$TEST_PKG" \
    -F "feed=@$TEST_FEED")
if [ "$HTTP_CODE" = "200" ]; then pass "upload to beta succeeded (200)"; else fail "expected 200, got $HTTP_CODE"; fi

echo ""
echo "--- Test 4: GET /beta/releases.win.json (static, no auth) ---"
RESP=$(curl -s "http://localhost:$PORT/beta/releases.win.json")
if echo "$RESP" | grep -q "1.0.0"; then pass "beta feed contains 1.0.0"; else fail "beta feed missing 1.0.0: $RESP"; fi

echo ""
echo "--- Test 5: List versions for beta (API, with auth) ---"
RESP=$(curl -s -H "Authorization: Bearer $TOKEN" "http://localhost:$PORT/api/channels/beta/releases")
if echo "$RESP" | grep -q "1.0.0"; then pass "beta list contains 1.0.0"; else fail "beta list missing 1.0.0: $RESP"; fi

echo ""
echo "--- Test 6: Promote 1.0.0 from beta to stable ---"
HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" -X POST \
    "http://localhost:$PORT/api/channels/stable/promote?from=beta&version=1.0.0" \
    -H "Authorization: Bearer $TOKEN")
if [ "$HTTP_CODE" = "200" ]; then pass "promote succeeded (200)"; else fail "expected 200, got $HTTP_CODE"; fi

echo ""
echo "--- Test 7: GET /stable/releases.win.json ---"
RESP=$(curl -s "http://localhost:$PORT/stable/releases.win.json")
if echo "$RESP" | grep -q "1.0.0"; then pass "stable feed contains 1.0.0 after promote"; else fail "stable feed missing 1.0.0: $RESP"; fi

echo ""
echo "--- Test 8: Promote missing version ---"
HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" -X POST \
    "http://localhost:$PORT/api/channels/stable/promote?from=beta&version=9.9.9" \
    -H "Authorization: Bearer $TOKEN")
if [ "$HTTP_CODE" = "404" ]; then pass "missing version returns 404"; else fail "expected 404, got $HTTP_CODE"; fi

echo ""
echo "--- Test 9: Upload 11 versions and verify cleanup ---"
CLEANUP_OK=true
for i in $(seq 1 10); do
    VER="1.0.$i"
    PKG_FILE="$DATA_DIR/App-${VER}-full.nupkg"
    echo "package-data-${VER}" > "$PKG_FILE"

    FEED_FILE="$DATA_DIR/releases.win.json"
    cat > "$FEED_FILE" << FEOF
{"Assets":[{"PackageId":"App","Version":"${VER}","Type":"Full","FileName":"App-${VER}-full.nupkg","Size":20}]}
FEOF

    HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" -X POST "http://localhost:$PORT/api/channels/beta/releases" \
        -H "Authorization: Bearer $TOKEN" \
        -F "package=@$PKG_FILE" \
        -F "feed=@$FEED_FILE")
    if [ "$HTTP_CODE" != "200" ]; then
        fail "upload version $VER failed: $HTTP_CODE"
        CLEANUP_OK=false
        break
    fi
done

if $CLEANUP_OK; then
    # Check that version 1.0.0 was cleaned up (oldest of 11 total)
    RESP=$(curl -s "http://localhost:$PORT/beta/releases.win.json")
    if echo "$RESP" | grep -q '"1.0.0"'; then
        fail "version 1.0.0 should have been cleaned up"
    else
        pass "auto-cleanup removed oldest version (1.0.0)"
    fi
fi

echo ""
echo "--- Test 10: GET static .nupkg file ---"
HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" "http://localhost:$PORT/beta/App-1.0.1-full.nupkg")
if [ "$HTTP_CODE" = "200" ]; then pass "static .nupkg served (200)"; else fail "expected 200, got $HTTP_CODE"; fi

echo ""
echo "--- Test 11: List stable channel ---"
RESP=$(curl -s -H "Authorization: Bearer $TOKEN" "http://localhost:$PORT/api/channels/stable/releases")
if echo "$RESP" | grep -q '"stable"'; then pass "stable list works"; else fail "stable list failed: $RESP"; fi

echo ""
echo "--- Test 12: Invalid channel ---"
HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" "http://localhost:$PORT/dev/releases.win.json")
if [ "$HTTP_CODE" = "404" ]; then pass "invalid channel returns 404"; else fail "expected 404, got $HTTP_CODE"; fi

# Cleanup
kill $SERVER_PID 2>/dev/null || true
wait $SERVER_PID 2>/dev/null || true

echo ""
echo "================================"
echo "Results: $PASS passed, $FAIL failed, $SKIP skipped"
echo "================================"

if [ $FAIL -gt 0 ]; then
    echo ""
    echo "Server log (last 30 lines):"
    tail -30 "$DATA_DIR/server.log"
    exit 1
fi

exit 0
