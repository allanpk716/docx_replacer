package handler

import (
	"fmt"
	"log"
	"net/http"
	"strings"

	"update-hub/storage"
)

// StaticHandler serves release artifacts from the file system.
// It handles GET /{appId}/{channel}/{filename} for Velopack SimpleWebSource compatibility.
// No authentication required — this is the public-facing download endpoint.
type StaticHandler struct {
	Store *storage.Store
}

// NewStaticHandler creates a StaticHandler backed by the given store.
func NewStaticHandler(store *storage.Store) *StaticHandler {
	return &StaticHandler{Store: store}
}

// ServeHTTP handles GET requests for release artifacts:
//   - /{appId}/{channel}/releases.{os}.json → releases feed
//   - /{appId}/{channel}/*.nupkg → package files
//
// No channel/appId validation — just serve files, 404 if not found.
// Dynamic channels are supported per D059.
func (h *StaticHandler) ServeHTTP(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodGet {
		writeJSONError(w, http.StatusMethodNotAllowed, "method not allowed")
		return
	}

	// Parse path: /{appId}/{channel}/{filename}
	cleanPath := strings.TrimPrefix(r.URL.Path, "/")
	parts := strings.SplitN(cleanPath, "/", 3)
	if len(parts) != 3 || parts[0] == "" || parts[1] == "" || parts[2] == "" {
		http.NotFound(w, r)
		return
	}

	appId := parts[0]
	channel := parts[1]
	filename := parts[2]

	// Path traversal protection
	if strings.Contains(filename, "..") || strings.Contains(channel, "..") || strings.Contains(appId, "..") {
		log.Printf(`{"event":"static_path_traversal","path":"%s"}`, r.URL.Path)
		writeJSONError(w, http.StatusBadRequest, "invalid path")
		return
	}

	// Read file from store
	data, err := h.Store.ReadFile(appId, channel, filename)
	if err != nil {
		http.NotFound(w, r)
		return
	}

	// Set content type based on extension
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
