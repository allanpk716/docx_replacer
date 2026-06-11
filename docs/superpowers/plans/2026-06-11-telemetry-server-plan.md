# Telemetry Server-Side Implementation Plan (update-hub, Go)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add telemetry event collection API to the existing update-hub Go server, with HMAC-SHA256 verification, SQLite storage, data retention, and a Vue dashboard page.

**Architecture:** New `telemetry` package under `update-hub/` follows existing handler/database patterns. Telemetry events stored in the same SQLite database as release metadata. Dashboard is a new Vue route in the embedded SPA.

**Tech Stack:** Go 1.22, modernc.org/sqlite (pure Go), HMAC-SHA256, Vue 3 + ECharts

**Spec:** `docs/superpowers/specs/2026-06-11-telemetry-design.md`

---

## File Structure

| Action | File | Responsibility |
|--------|------|----------------|
| Modify | `database/db.go` | Add `telemetry_events` table creation to `Init()` |
| Create | `telemetry/model.go` | Event data model (Go struct) |
| Create | `telemetry/verify.go` | HMAC-SHA256 signature verification |
| Create | `telemetry/handler.go` | POST /api/telemetry handler (single + batch) |
| Create | `telemetry/handler_test.go` | Handler tests |
| Create | `telemetry/stats.go` | GET /api/telemetry/stats + /api/telemetry/apps handlers |
| Create | `telemetry/cleanup.go` | Expired event cleanup logic |
| Modify | `main.go` | Parse telemetry flags, register routes, wire handlers |
| Create | `web/src/views/TelemetryView.vue` | Dashboard page |
| Modify | `web/src/router/index.ts` | Add /telemetry route |

---

### Task 1: Database schema migration

**Files:**
- Modify: `database/db.go` — find the existing `CREATE TABLE IF NOT EXISTS` block and append

- [ ] **Step 1: Add telemetry_events table to `database/db.go`**

In the `Init()` function, after the existing `versions` table creation, add:

```go
// Telemetry events table
_, err = db.Exec(`
    CREATE TABLE IF NOT EXISTS telemetry_events (
        id          INTEGER PRIMARY KEY AUTOINCREMENT,
        app_id      TEXT NOT NULL,
        event       TEXT NOT NULL,
        timestamp   TEXT NOT NULL,
        session_id  TEXT NOT NULL,
        user_name   TEXT NOT NULL,
        machine     TEXT NOT NULL,
        version     TEXT NOT NULL,
        properties  TEXT,
        received_at TEXT NOT NULL DEFAULT (datetime('now'))
    )
`)
if err != nil {
    return nil, fmt.Errorf("create telemetry_events: %w", err)
}

_, err = db.Exec(`CREATE INDEX IF NOT EXISTS idx_tel_app_time ON telemetry_events(app_id, timestamp)`)
if err != nil {
    return nil, fmt.Errorf("create idx_tel_app_time: %w", err)
}

_, err = db.Exec(`CREATE INDEX IF NOT EXISTS idx_tel_event ON telemetry_events(app_id, event)`)
if err != nil {
    return nil, fmt.Errorf("create idx_tel_event: %w", err)
}
```

- [ ] **Step 2: Verify existing tests still pass**

Run: `cd /c/WorkSpace/agent/update-hub && go test ./database/...`
Expected: PASS

- [ ] **Step 3: Commit**

```bash
git add database/db.go
git commit -m "feat(update-hub): add telemetry_events table to SQLite schema"
```

---

### Task 2: Telemetry data model

**Files:**
- Create: `telemetry/model.go`

- [ ] **Step 1: Create telemetry package with event model**

```go
package telemetry

import "encoding/json"

// Event represents a telemetry event submitted by a client application.
type Event struct {
    AppID     string            `json:"app_id"`
    Signature string            `json:"signature"`
    EventName string            `json:"event"`
    Timestamp string            `json:"timestamp"`
    SessionID string            `json:"session_id"`
    User      string            `json:"user"`
    Machine   string            `json:"machine"`
    Version   string            `json:"version"`
    Props     json.RawMessage   `json:"properties,omitempty"`
}

// EventRow is an event as stored in (or queried from) telemetry_events.
type EventRow struct {
    ID         int64  `json:"id"`
    AppID      string `json:"app_id"`
    Event      string `json:"event"`
    Timestamp  string `json:"timestamp"`
    SessionID  string `json:"session_id"`
    UserName   string `json:"user_name"`
    Machine    string `json:"machine"`
    Version    string `json:"version"`
    Properties string `json:"properties"`
    ReceivedAt string `json:"received_at"`
}

// StatsResponse is the aggregated stats returned by GET /api/telemetry/stats.
type StatsResponse struct {
    AppID         string        `json:"app_id"`
    TotalEvents   int64         `json:"total_events"`
    UniqueUsers   int64         `json:"unique_users"`
    UniqueMachines int64        `json:"unique_machines"`
    EventCounts   []EventCount  `json:"event_counts"`
    VersionDist   []VersionCount `json:"version_dist"`
    DailyActive   []DailyCount  `json:"daily_active"`
}

type EventCount struct {
    Event string `json:"event"`
    Count int64  `json:"count"`
}

type VersionCount struct {
    Version string `json:"version"`
    Count   int64  `json:"count"`
}

type DailyCount struct {
    Date  string `json:"date"`
    Users int64  `json:"users"`
    Events int64 `json:"events"`
}

// AppInfo lists a registered telemetry app.
type AppInfo struct {
    AppID       string `json:"app_id"`
    EventCount  int64  `json:"event_count"`
    LatestEvent string `json:"latest_event"`
}
```

- [ ] **Step 2: Commit**

```bash
git add telemetry/model.go
git commit -m "feat(update-hub): add telemetry event data model"
```

---

### Task 3: HMAC-SHA256 signature verification

**Files:**
- Create: `telemetry/verify.go`
- Create: `telemetry/verify_test.go`

- [ ] **Step 1: Write verification tests**

```go
package telemetry

import (
    "encoding/hex"
    "testing"
)

func TestVerifySignature_Valid(t *testing.T) {
    key := "test-secret-key-0123456789abcdef"
    event := Event{
        AppID:     "docufiller",
        EventName: "fill_complete",
        Timestamp: "2026-06-11T14:30:00+08:00",
        SessionID: "abc-123",
        User:      "testuser",
        Machine:   "PC-001",
        Version:   "1.0.0",
    }
    sig := computeSignature(t, key, event)
    event.Signature = sig

    if !VerifySignature(key, event) {
        t.Error("expected signature to be valid")
    }
}

func TestVerifySignature_Invalid(t *testing.T) {
    key := "test-secret-key-0123456789abcdef"
    event := Event{
        AppID:     "docufiller",
        Signature: "badsignature",
        EventName: "fill_complete",
        Timestamp: "2026-06-11T14:30:00+08:00",
        SessionID: "abc-123",
        User:      "testuser",
        Machine:   "PC-001",
        Version:   "1.0.0",
    }

    if VerifySignature(key, event) {
        t.Error("expected signature to be invalid")
    }
}

func TestVerifySignature_WrongKey(t *testing.T) {
    key1 := "correct-key"
    key2 := "wrong-key"
    event := Event{
        AppID:     "docufiller",
        EventName: "fill_complete",
        Timestamp: "2026-06-11T14:30:00+08:00",
        SessionID: "abc-123",
        User:      "testuser",
        Machine:   "PC-001",
        Version:   "1.0.0",
    }
    sig := computeSignature(t, key1, event)
    event.Signature = sig

    if VerifySignature(key2, event) {
        t.Error("expected signature to be invalid with wrong key")
    }
}

// computeSignature is a test helper that computes the expected HMAC-SHA256.
func computeSignature(t *testing.T, key string, event Event) string {
    t.Helper()
    // Use the same payload construction as the production code
    payload := buildPayload(event)
    mac := hmac.New(sha256.New, []byte(key))
    mac.Write(payload)
    return hex.EncodeToString(mac.Sum(nil))
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `cd /c/WorkSpace/agent/update-hub && go test ./telemetry/...`
Expected: COMPILE ERROR — `VerifySignature`, `buildPayload` not defined

- [ ] **Step 3: Implement verification logic**

```go
package telemetry

import (
    "crypto/hmac"
    "crypto/sha256"
    "encoding/hex"
    "encoding/json"
)

// VerifySignature checks that the event's signature matches HMAC-SHA256(key, payload).
// Returns true if valid.
func VerifySignature(key string, event Event) bool {
    if event.Signature == "" {
        return false
    }

    payload := buildPayload(event)
    mac := hmac.New(sha256.New, []byte(key))
    mac.Write(payload)
    expected := hex.EncodeToString(mac.Sum(nil))

    return hmac.Equal([]byte(event.Signature), []byte(expected))
}

// buildPayload serializes the event without the signature field for HMAC computation.
// Keys are sorted to ensure deterministic output.
func buildPayload(event Event) []byte {
    // Create a copy without signature
    sansSig := map[string]interface{}{
        "app_id":     event.AppID,
        "event":      event.EventName,
        "timestamp":  event.Timestamp,
        "session_id": event.SessionID,
        "user":       event.User,
        "machine":    event.Machine,
        "version":    event.Version,
    }
    if event.Props != nil {
        sansSig["properties"] = event.Props
    }

    // json.Marshal sorts map keys in Go
    b, _ := json.Marshal(sansSig)
    return b
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `cd /c/WorkSpace/agent/update-hub && go test ./telemetry/...`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add telemetry/verify.go telemetry/verify_test.go
git commit -m "feat(update-hub): add HMAC-SHA256 telemetry signature verification"
```

---

### Task 4: Telemetry POST handler

**Files:**
- Create: `telemetry/handler.go`

- [ ] **Step 1: Write the handler**

```go
package telemetry

import (
    "database/sql"
    "encoding/json"
    "io"
    "log"
    "net/http"
    "time"
)

// KeyLookup returns the HMAC secret key for a given app_id, or empty string if unknown.
type KeyLookup func(appID string) string

// IngestHandler handles POST /api/telemetry.
type IngestHandler struct {
    DB         *sql.DB
    LookupKey  KeyLookup
    RetentionDays int
}

func NewIngestHandler(db *sql.DB, lookupKey KeyLookup, retentionDays int) *IngestHandler {
    return &IngestHandler{DB: db, LookupKey: lookupKey, RetentionDays: retentionDays}
}

func (h *IngestHandler) ServeHTTP(w http.ResponseWriter, r *http.Request) {
    if r.Method != http.MethodPost {
        http.Error(w, `{"error":"method not allowed"}`, http.StatusMethodNotAllowed)
        return
    }

    // Read entire body first (1MB max), then parse
    body, err := io.ReadAll(io.LimitReader(r.Body, 1<<20))
    if err != nil {
        http.Error(w, `{"error":"read failed"}`, http.StatusBadRequest)
        return
    }
    if len(body) == 0 {
        http.Error(w, `{"error":"empty body"}`, http.StatusBadRequest)
        return
    }

    var events []Event

    // Try array first
    if err := json.Unmarshal(body, &events); err != nil {
        // Try single event
        var single Event
        if err2 := json.Unmarshal(body, &single); err2 != nil {
            http.Error(w, `{"error":"invalid json"}`, http.StatusBadRequest)
            return
        }
        events = []Event{single}
    }

    if len(events) == 0 {
        http.Error(w, `{"error":"no events"}`, http.StatusBadRequest)
        return
    }
    if len(events) > 100 {
        http.Error(w, `{"error":"too many events (max 100)"}`, http.StatusBadRequest)
        return
    }

    accepted := 0
    rejected := 0

    for _, evt := range events {
        // Validate required fields
        if evt.AppID == "" || evt.EventName == "" || evt.Timestamp == "" || evt.SessionID == "" {
            rejected++
            continue
        }

        // Validate app_id format (alphanumeric + hyphen)
        if !isValidAppID(evt.AppID) {
            rejected++
            continue
        }

        // Lookup key and verify signature
        key := h.LookupKey(evt.AppID)
        if key == "" {
            rejected++
            continue
        }
        if !VerifySignature(key, evt) {
            rejected++
            continue
        }

        // Insert into database
        _, err := h.DB.Exec(
            `INSERT INTO telemetry_events (app_id, event, timestamp, session_id, user_name, machine, version, properties)
             VALUES (?, ?, ?, ?, ?, ?, ?, ?)`,
            evt.AppID, evt.EventName, evt.Timestamp, evt.SessionID, evt.User, evt.Machine, evt.Version, string(evt.Props),
        )
        if err != nil {
            log.Printf(`{"event":"telemetry_insert_error","app_id":"%s","error":"%s"}`, evt.AppID, err)
            rejected++
            continue
        }
        accepted++
    }

    // Periodic cleanup (1% chance per request to avoid overhead)
    if h.RetentionDays > 0 && accepted > 0 && time.Now().Unix()%100 == 0 {
        go h.cleanup()
    }

    w.Header().Set("Content-Type", "application/json")
    w.WriteHeader(http.StatusOK)
    json.NewEncoder(w).Encode(map[string]int{
        "accepted": accepted,
        "rejected": rejected,
    })
}

func isValidAppID(id string) bool {
    for _, c := range id {
        if !((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '-' || c == '_') {
            return false
        }
    }
    return len(id) > 0 && len(id) <= 64
}

func (h *IngestHandler) cleanup() {
    cutoff := time.Now().AddDate(0, 0, -h.RetentionDays).Format("2006-01-02T15:04:05Z07:00")
    result, err := h.DB.Exec(`DELETE FROM telemetry_events WHERE received_at < ?`, cutoff)
    if err != nil {
        log.Printf(`{"event":"telemetry_cleanup_error","error":"%s"}`, err)
        return
    }
    n, _ := result.RowsAffected()
    if n > 0 {
        log.Printf(`{"event":"telemetry_cleanup","deleted":%d,"cutoff":"%s"}`, n, cutoff)
    }
}
```

- [ ] **Step 2: Fix imports — add `io` to the import list**

The handler uses `io.ReadAll` and `io.LimitReader`. Add `"io"` to imports:

```go
import (
    "database/sql"
    "encoding/json"
    "io"
    "log"
    "net/http"
    "time"
)
```

Remove unused imports (`fmt`, `strings`) from the initial version.

- [ ] **Step 3: Write handler test**

```go
package telemetry

import (
    "bytes"
    "database/sql"
    "encoding/hex"
    "encoding/json"
    "net/http"
    "net/http/httptest"
    "os"
    "testing"

    _ "modernc.org/sqlite"
)

func setupTestDB(t *testing.T) *sql.DB {
    t.Helper()
    db, err := sql.Open("sqlite", ":memory:")
    if err != nil {
        t.Fatal(err)
    }
    t.Cleanup(func() { db.Close() })
    _, err = db.Exec(`
        CREATE TABLE telemetry_events (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            app_id TEXT NOT NULL,
            event TEXT NOT NULL,
            timestamp TEXT NOT NULL,
            session_id TEXT NOT NULL,
            user_name TEXT NOT NULL,
            machine TEXT NOT NULL,
            version TEXT NOT NULL,
            properties TEXT,
            received_at TEXT NOT NULL DEFAULT (datetime('now'))
        )
    `)
    if err != nil {
        t.Fatal(err)
    }
    return db
}

func testKey(appID string) string {
    if appID == "testapp" {
        return "test-secret"
    }
    return ""
}

func makeSignedEvent(t *testing.T, key string, evt Event) Event {
    t.Helper()
    mac := hmac.New(sha256.New, []byte(key))
    payload := buildPayload(evt)
    mac.Write(payload)
    evt.Signature = hex.EncodeToString(mac.Sum(nil))
    return evt
}

func TestIngestHandler_SingleEvent(t *testing.T) {
    db := setupTestDB(t)
    handler := NewIngestHandler(db, testKey, 0)

    evt := makeSignedEvent(t, "test-secret", Event{
        AppID:     "testapp",
        EventName: "fill_complete",
        Timestamp: "2026-06-11T14:30:00+08:00",
        SessionID: "sess-1",
        User:      "testuser",
        Machine:   "PC-001",
        Version:   "1.0.0",
    })

    body, _ := json.Marshal(evt)
    req := httptest.NewRequest(http.MethodPost, "/api/telemetry", bytes.NewReader(body))
    w := httptest.NewRecorder()
    handler.ServeHTTP(w, req)

    if w.Code != http.StatusOK {
        t.Errorf("expected 200, got %d: %s", w.Code, w.Body.String())
    }

    // Verify row in DB
    var count int
    db.QueryRow("SELECT COUNT(*) FROM telemetry_events").Scan(&count)
    if count != 1 {
        t.Errorf("expected 1 row, got %d", count)
    }
}

func TestIngestHandler_BadSignature(t *testing.T) {
    db := setupTestDB(t)
    handler := NewIngestHandler(db, testKey, 0)

    evt := Event{
        AppID:     "testapp",
        Signature: "badsig",
        EventName: "fill_complete",
        Timestamp: "2026-06-11T14:30:00+08:00",
        SessionID: "sess-1",
        User:      "testuser",
        Machine:   "PC-001",
        Version:   "1.0.0",
    }

    body, _ := json.Marshal(evt)
    req := httptest.NewRequest(http.MethodPost, "/api/telemetry", bytes.NewReader(body))
    w := httptest.NewRecorder()
    handler.ServeHTTP(w, req)

    if w.Code != http.StatusOK {
        t.Errorf("expected 200 (partial success), got %d", w.Code)
    }

    var result map[string]int
    json.Unmarshal(w.Body.Bytes(), &result)
    if result["accepted"] != 0 || result["rejected"] != 1 {
        t.Errorf("expected 0 accepted, 1 rejected, got %v", result)
    }
}

func TestIngestHandler_UnknownApp(t *testing.T) {
    db := setupTestDB(t)
    handler := NewIngestHandler(db, testKey, 0)

    evt := makeSignedEvent(t, "some-key", Event{
        AppID:     "unknown-app",
        EventName: "fill_complete",
        Timestamp: "2026-06-11T14:30:00+08:00",
        SessionID: "sess-1",
    })

    body, _ := json.Marshal(evt)
    req := httptest.NewRequest(http.MethodPost, "/api/telemetry", bytes.NewReader(body))
    w := httptest.NewRecorder()
    handler.ServeHTTP(w, req)

    var result map[string]int
    json.Unmarshal(w.Body.Bytes(), &result)
    if result["rejected"] != 1 {
        t.Errorf("expected rejection for unknown app, got %v", result)
    }
}

func TestIngestHandler_Batch(t *testing.T) {
    db := setupTestDB(t)
    handler := NewIngestHandler(db, testKey, 0)

    events := []Event{
        makeSignedEvent(t, "test-secret", Event{
            AppID: "testapp", EventName: "fill_complete", Timestamp: "2026-06-11T14:30:00+08:00",
            SessionID: "s1", User: "u1", Machine: "m1", Version: "1.0.0",
        }),
        makeSignedEvent(t, "test-secret", Event{
            AppID: "testapp", EventName: "cleanup_complete", Timestamp: "2026-06-11T14:31:00+08:00",
            SessionID: "s1", User: "u1", Machine: "m1", Version: "1.0.0",
        }),
    }

    body, _ := json.Marshal(events)
    req := httptest.NewRequest(http.MethodPost, "/api/telemetry", bytes.NewReader(body))
    w := httptest.NewRecorder()
    handler.ServeHTTP(w, req)

    var result map[string]int
    json.Unmarshal(w.Body.Bytes(), &result)
    if result["accepted"] != 2 {
        t.Errorf("expected 2 accepted, got %v", result)
    }
}
```

Add missing imports for test file:

```go
import (
    "bytes"
    "crypto/hmac"
    "crypto/sha256"
    "database/sql"
    "encoding/hex"
    "encoding/json"
    "net/http"
    "net/http/httptest"
    "testing"

    _ "modernc.org/sqlite"
)
```

- [ ] **Step 4: Run tests**

Run: `cd /c/WorkSpace/agent/update-hub && go test ./telemetry/...`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add telemetry/handler.go telemetry/handler_test.go
git commit -m "feat(update-hub): add POST /api/telemetry handler with batch support"
```

---

### Task 5: Stats and app-list endpoints

**Files:**
- Create: `telemetry/stats.go`

- [ ] **Step 1: Write the stats handler**

```go
package telemetry

import (
    "database/sql"
    "encoding/json"
    "net/http"
)

// StatsHandler handles GET /api/telemetry/stats and GET /api/telemetry/apps.
type StatsHandler struct {
    DB *sql.DB
}

func NewStatsHandler(db *sql.DB) *StatsHandler {
    return &StatsHandler{DB: db}
}

func (h *StatsHandler) ServeStats(w http.ResponseWriter, r *http.Request) {
    appID := r.URL.Query().Get("app_id")
    if appID == "" {
        http.Error(w, `{"error":"app_id required"}`, http.StatusBadRequest)
        return
    }

    days := r.URL.Query().Get("days")
    if days == "" {
        days = "30"
    }

    resp := StatsResponse{AppID: appID}

    // Total events
    h.DB.QueryRow(
        `SELECT COUNT(*) FROM telemetry_events WHERE app_id = ? AND timestamp >= datetime('now', '-' || ? || ' days')`,
        appID, days,
    ).Scan(&resp.TotalEvents)

    // Unique users and machines
    h.DB.QueryRow(
        `SELECT COUNT(DISTINCT user_name), COUNT(DISTINCT machine) FROM telemetry_events WHERE app_id = ? AND timestamp >= datetime('now', '-' || ? || ' days')`,
        appID, days,
    ).Scan(&resp.UniqueUsers, &resp.UniqueMachines)

    // Event counts
    rows, err := h.DB.Query(
        `SELECT event, COUNT(*) as cnt FROM telemetry_events WHERE app_id = ? AND timestamp >= datetime('now', '-' || ? || ' days') GROUP BY event ORDER BY cnt DESC`,
        appID, days,
    )
    if err == nil {
        for rows.Next() {
            var ec EventCount
            rows.Scan(&ec.Event, &ec.Count)
            resp.EventCounts = append(resp.EventCounts, ec)
        }
        rows.Close()
    }

    // Version distribution
    rows, err = h.DB.Query(
        `SELECT version, COUNT(*) as cnt FROM telemetry_events WHERE app_id = ? AND timestamp >= datetime('now', '-' || ? || ' days') GROUP BY version ORDER BY cnt DESC`,
        appID, days,
    )
    if err == nil {
        for rows.Next() {
            var vc VersionCount
            rows.Scan(&vc.Version, &vc.Count)
            resp.VersionDist = append(resp.VersionDist, vc)
        }
        rows.Close()
    }

    // Daily active users and events (last N days)
    rows, err = h.DB.Query(
        `SELECT DATE(timestamp) as day, COUNT(DISTINCT user_name), COUNT(*) FROM telemetry_events WHERE app_id = ? AND timestamp >= datetime('now', '-' || ? || ' days') GROUP BY day ORDER BY day`,
        appID, days,
    )
    if err == nil {
        for rows.Next() {
            var dc DailyCount
            rows.Scan(&dc.Date, &dc.Users, &dc.Events)
            resp.DailyActive = append(resp.DailyActive, dc)
        }
        rows.Close()
    }

    w.Header().Set("Content-Type", "application/json")
    json.NewEncoder(w).Encode(resp)
}

func (h *StatsHandler) ServeApps(w http.ResponseWriter, r *http.Request) {
    rows, err := h.DB.Query(
        `SELECT app_id, COUNT(*) as cnt, MAX(timestamp) as latest FROM telemetry_events GROUP BY app_id ORDER BY app_id`,
    )
    if err != nil {
        http.Error(w, `{"error":"query failed"}`, http.StatusInternalServerError)
        return
    }
    defer rows.Close()

    var apps []AppInfo
    for rows.Next() {
        var a AppInfo
        rows.Scan(&a.AppID, &a.EventCount, &a.LatestEvent)
        apps = append(apps, a)
    }

    w.Header().Set("Content-Type", "application/json")
    json.NewEncoder(w).Encode(apps)
}
```

- [ ] **Step 2: Commit**

```bash
git add telemetry/stats.go
git commit -m "feat(update-hub): add telemetry stats and app-list endpoints"
```

---

### Task 6: Wire telemetry into main.go

**Files:**
- Modify: `main.go`

- [ ] **Step 1: Add telemetry CLI flags**

In `main.go`, after the existing flag declarations, add:

```go
telemetryKeys := flag.String("telemetry-keys", "", "HMAC keys for telemetry: app1=key1,app2=key2 (comma-separated)")
telemetryRetention := flag.Int("telemetry-retention-days", 90, "Days to retain telemetry events (0 = keep forever)")
```

- [ ] **Step 2: Parse telemetry keys into a lookup function**

After flag.Parse(), add:

```go
// Parse telemetry HMAC keys
telKeys := parseTelemetryKeys(*telemetryKeys)
telKeyLookup := func(appID string) string {
    return telKeys[appID]
}
```

Add the helper function before `main()`:

```go
func parseTelemetryKeys(s string) map[string]string {
    m := make(map[string]string)
    if s == "" {
        return m
    }
    for _, pair := range strings.Split(s, ",") {
        parts := strings.SplitN(strings.TrimSpace(pair), "=", 2)
        if len(parts) == 2 && parts[1] != "" {
            m[parts[0]] = parts[1]
        }
    }
    return m
}
```

- [ ] **Step 3: Create telemetry handlers and register routes**

After the existing handler creation block, add:

```go
// Telemetry handlers
telemetryIngest := telemetry.NewIngestHandler(db, telKeyLookup, *telemetryRetention)
telemetryStats := telemetry.NewStatsHandler(db)
```

In the route registration block, add:

```go
// Telemetry endpoints
mux.HandleFunc("POST /api/telemetry", telemetryIngest.ServeHTTP)
mux.HandleFunc("GET /api/telemetry/stats", telemetryStats.ServeStats)
mux.HandleFunc("GET /api/telemetry/apps", telemetryStats.ServeApps)
```

Add `"update-hub/telemetry"` and `"strings"` to the import list.

- [ ] **Step 4: Verify build compiles**

Run: `cd /c/WorkSpace/agent/update-hub && go build ./...`
Expected: BUILD SUCCESS

- [ ] **Step 5: Commit**

```bash
git add main.go
git commit -m "feat(update-hub): wire telemetry routes and HMAC key config into main"
```

---

### Task 7: Vue telemetry dashboard page

**Files:**
- Create: `web/src/views/TelemetryView.vue`
- Modify: `web/src/router/index.ts`

- [ ] **Step 1: Add /telemetry route to router**

In `web/src/router/index.ts`, add a new route entry in the routes array:

```typescript
{
    path: '/telemetry',
    name: 'telemetry',
    component: () => import('../views/TelemetryView.vue'),
    meta: { requiresAuth: true }
}
```

- [ ] **Step 2: Add navigation link to layout**

In `AppLayout.vue` (or equivalent nav component), add a "Telemetry" link alongside the existing navigation:

```html
<router-link to="/telemetry">Telemetry</router-link>
```

- [ ] **Step 3: Create TelemetryView.vue with ECharts dashboard**

```vue
<template>
  <div class="telemetry-view">
    <div class="toolbar">
      <select v-model="selectedApp" @change="fetchStats">
        <option v-for="app in apps" :key="app.app_id" :value="app.app_id">
          {{ app.app_id }} ({{ app.event_count }} events)
        </option>
      </select>
      <select v-model="selectedDays" @change="fetchStats">
        <option value="7">Last 7 days</option>
        <option value="30">Last 30 days</option>
        <option value="90">Last 90 days</option>
      </select>
    </div>

    <div v-if="stats" class="overview-cards">
      <div class="card">
        <div class="card-value">{{ stats.total_events }}</div>
        <div class="card-label">Total Events</div>
      </div>
      <div class="card">
        <div class="card-value">{{ stats.unique_users }}</div>
        <div class="card-label">Unique Users</div>
      </div>
      <div class="card">
        <div class="card-value">{{ stats.unique_machines }}</div>
        <div class="card-label">Unique Machines</div>
      </div>
    </div>

    <div class="charts">
      <div class="chart-container">
        <h3>Daily Active Users & Events</h3>
        <div ref="dailyChart" style="height: 300px"></div>
      </div>
      <div class="chart-container">
        <h3>Event Distribution</h3>
        <div ref="eventChart" style="height: 300px"></div>
      </div>
      <div class="chart-container">
        <h3>Version Distribution</h3>
        <div ref="versionChart" style="height: 300px"></div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted, nextTick } from 'vue'
import * as echarts from 'echarts'

const API_BASE = ''  // Same origin

const apps = ref<any[]>([])
const selectedApp = ref('')
const selectedDays = ref('30')
const stats = ref<any>(null)

const dailyChart = ref<HTMLElement>()
const eventChart = ref<HTMLElement>()
const versionChart = ref<HTMLElement>()

let dailyInstance: echarts.ECharts | null = null
let eventInstance: echarts.ECharts | null = null
let versionInstance: echarts.ECharts | null = null

async function fetchApps() {
  const res = await fetch(`${API_BASE}/api/telemetry/apps`, { credentials: 'include' })
  apps.value = await res.json()
  if (apps.value.length > 0 && !selectedApp.value) {
    selectedApp.value = apps.value[0].app_id
    fetchStats()
  }
}

async function fetchStats() {
  if (!selectedApp.value) return
  const res = await fetch(
    `${API_BASE}/api/telemetry/stats?app_id=${selectedApp.value}&days=${selectedDays.value}`,
    { credentials: 'include' }
  )
  stats.value = await res.json()
  await nextTick()
  renderCharts()
}

function renderCharts() {
  if (!stats.value) return

  // Daily active
  if (dailyChart.value && stats.value.daily_active) {
    if (!dailyInstance) dailyInstance = echarts.init(dailyChart.value)
    dailyInstance.setOption({
      tooltip: { trigger: 'axis' },
      xAxis: { type: 'category', data: stats.value.daily_active.map((d: any) => d.date) },
      yAxis: [
        { type: 'value', name: 'Users' },
        { type: 'value', name: 'Events' },
      ],
      series: [
        { name: 'Users', type: 'bar', data: stats.value.daily_active.map((d: any) => d.users) },
        { name: 'Events', type: 'line', yAxisIndex: 1, data: stats.value.daily_active.map((d: any) => d.events) },
      ],
    })
  }

  // Event distribution
  if (eventChart.value && stats.value.event_counts) {
    if (!eventInstance) eventInstance = echarts.init(eventChart.value)
    eventInstance.setOption({
      tooltip: { trigger: 'item' },
      xAxis: { type: 'category', data: stats.value.event_counts.map((e: any) => e.event) },
      yAxis: { type: 'value' },
      series: [{ type: 'bar', data: stats.value.event_counts.map((e: any) => e.count) }],
    })
  }

  // Version distribution
  if (versionChart.value && stats.value.version_dist) {
    if (!versionInstance) versionInstance = echarts.init(versionChart.value)
    versionInstance.setOption({
      tooltip: { trigger: 'item' },
      series: [{
        type: 'pie',
        radius: ['40%', '70%'],
        data: stats.value.version_dist.map((v: any) => ({ name: v.version, value: v.count })),
      }],
    })
  }
}

onMounted(() => {
  fetchApps()
})

onUnmounted(() => {
  dailyInstance?.dispose()
  eventInstance?.dispose()
  versionInstance?.dispose()
})
</script>

<style scoped>
.telemetry-view { padding: 24px; }
.toolbar { display: flex; gap: 12px; margin-bottom: 24px; }
.toolbar select { padding: 8px 12px; border: 1px solid #ddd; border-radius: 6px; }
.overview-cards { display: flex; gap: 16px; margin-bottom: 24px; }
.card { flex: 1; padding: 20px; border: 1px solid #eee; border-radius: 8px; text-align: center; }
.card-value { font-size: 2em; font-weight: bold; }
.card-label { color: #666; margin-top: 4px; }
.charts { display: grid; grid-template-columns: 1fr 1fr; gap: 24px; }
.chart-container { border: 1px solid #eee; border-radius: 8px; padding: 16px; }
.chart-container h3 { margin: 0 0 12px; }
.charts .chart-container:first-child { grid-column: 1 / -1; }
</style>
```

- [ ] **Step 4: Install ECharts dependency**

Run: `cd update-hub/web && npm install echarts`

- [ ] **Step 5: Build and verify**

Run: `cd update-hub/web && npm run build`
Expected: BUILD SUCCESS

- [ ] **Step 6: Commit**

```bash
git add web/src/views/TelemetryView.vue web/src/router/index.ts web/package.json web/package-lock.json
git commit -m "feat(update-hub): add telemetry dashboard Vue page with ECharts"
```

---

### Task 8: Integration smoke test

**Files:**
- None (manual verification)

- [ ] **Step 1: Build the Go server**

Run: `cd /c/WorkSpace/agent/update-hub && go build -o update-hub-test.exe .`
Expected: BUILD SUCCESS

- [ ] **Step 2: Start the server with a test telemetry key**

Run: `./update-hub-test.exe --port 30099 --data-dir ./test-telemetry-data --telemetry-keys testapp=mysecret --password admin`

- [ ] **Step 3: Send a test event using curl**

```bash
# First, compute a valid signature (use a quick script or the test code)
# For manual testing, send an unsigned event and verify rejection:
curl -s -X POST http://localhost:30099/api/telemetry \
  -H "Content-Type: application/json" \
  -d '{"app_id":"testapp","event":"test","timestamp":"2026-06-11T14:30:00+08:00","session_id":"s1","user":"test","machine":"PC1","version":"1.0.0","signature":"invalid"}'
```
Expected: `{"accepted":0,"rejected":1}`

- [ ] **Step 4: Verify dashboard loads**

Open browser: `http://localhost:30099/`
Login with password "admin", navigate to Telemetry page.

- [ ] **Step 5: Clean up test artifacts**

```bash
rm -rf ./test-telemetry-data ./update-hub-test.exe
```

- [ ] **Step 6: Final commit if any fixes needed**

```bash
git add -A
git commit -m "fix(update-hub): telemetry integration fixes from smoke test"
```
