package handler

import (
	"fmt"
	"log"
	"net/http"
	"path/filepath"
	"strings"

	"docufiller-update-server/storage"
)

// validChannels tracks which channel names are allowed.
var validChannels = map[string]bool{
	"stable": true,
	"beta":   true,
}

// StaticHandler serves release artifacts from the file system.
type StaticHandler struct {
	Store *storage.Store
}

// NewStaticHandler creates a StaticHandler backed by the given store.
func NewStaticHandler(store *storage.Store) *StaticHandler {
	return &StaticHandler{Store: store}
}

// ServeHTTP handles GET requests for release artifacts:
//   - /{channel}/releases.win.json → releases feed
//   - /{channel}/*.nupkg → package files
func (h *StaticHandler) ServeHTTP(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodGet {
		http.Error(w, "method not allowed", http.StatusMethodNotAllowed)
		return
	}

	// Path is /{channel}/{filename}
	// Strip leading /
	cleanPath := strings.TrimPrefix(r.URL.Path, "/")
	parts := strings.SplitN(cleanPath, "/", 2)
	if len(parts) != 2 {
		http.NotFound(w, r)
		return
	}

	channel := parts[0]
	filename := parts[1]

	if !validChannels[channel] {
		http.Error(w, fmt.Sprintf("unknown channel: %s", channel), http.StatusNotFound)
		return
	}

	// Velopack SimpleWebSource requests the release feed by channel name.
	// With ExplicitChannel="stable", it requests "releases.stable.json".
	// We store the feed as "releases.win.json" (the OS-based name from vpk pack).
	// Map all known feed names to releases.win.json transparently.
	switch filename {
	case "RELEASES", "releases", "releases.stable.json", "releases.beta.json", "releases.win.json":
		filename = "releases.win.json"
	}

	filePath := filepath.Join(h.Store.DataDir, channel, filename)

	// Security: prevent path traversal
	if strings.Contains(filename, "..") {
		http.Error(w, "invalid filename", http.StatusBadRequest)
		return
	}

	// Check file exists
	if _, err := filepath.Abs(filePath); err != nil {
		http.Error(w, "invalid path", http.StatusBadRequest)
		return
	}

	data, err := h.Store.ReadFile(channel, filename)
	if err != nil {
		log.Printf("static: file not found channel=%s file=%s err=%v", channel, filename, err)
		http.NotFound(w, r)
		return
	}

	// Set content type
	switch {
	case strings.HasSuffix(filename, ".json"):
		w.Header().Set("Content-Type", "application/json")
	case strings.HasSuffix(filename, ".nupkg"):
		w.Header().Set("Content-Type", "application/octet-stream")
	default:
		w.Header().Set("Content-Type", "application/octet-stream")
	}

	w.Header().Set("Content-Length", fmt.Sprintf("%d", len(data)))
	w.Write(data)
}
