package handler

import (
	"encoding/json"
	"fmt"
	"log"
	"net/http"
	"os"
	"sort"
	"strings"

	"docufiller-update-server/storage"
)

// VersionInfo describes a single version in a channel.
type VersionInfo struct {
	Version   string   `json:"version"`
	Files     []string `json:"files"`
	TotalSize int64    `json:"total_size"`
	FileCount int      `json:"file_count"`
}

// ListResponse is the JSON response for the version list endpoint.
type ListResponse struct {
	Channel        string        `json:"channel"`
	Versions       []VersionInfo `json:"versions"`
	TotalVersions  int           `json:"total_versions"`
}

// ListHandler handles GET /api/channels/{channel}/releases.
type ListHandler struct {
	Store *storage.Store
}

// NewListHandler creates a ListHandler backed by the given store.
func NewListHandler(store *storage.Store) *ListHandler {
	return &ListHandler{Store: store}
}

// ServeHTTP returns the version list for a channel.
func (h *ListHandler) ServeHTTP(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodGet {
		http.Error(w, `{"error":"method not allowed"}`, http.StatusMethodNotAllowed)
		return
	}

	// Extract channel from path: /api/channels/{channel}/releases
	channel, err := extractChannelFromListPath(r.URL.Path)
	if err != nil {
		log.Printf(`{"event":"list_bad_path","path":"%s","error":"%s"}`, r.URL.Path, err)
		http.Error(w, fmt.Sprintf(`{"error":"%s"}`, err.Error()), http.StatusBadRequest)
		return
	}

	// Validate channel
	if !validChannels[channel] {
		log.Printf(`{"event":"list_invalid_channel","channel":"%s"}`, channel)
		http.Error(w, fmt.Sprintf(`{"error":"unknown channel: %s"}`, channel), http.StatusNotFound)
		return
	}

	// Check channel directory exists
	dir := h.Store.ChannelDir(channel)
	if _, err := os.Stat(dir); os.IsNotExist(err) {
		http.Error(w, fmt.Sprintf(`{"error":"channel directory not found: %s"}`, channel), http.StatusNotFound)
		return
	}

	// Read release feed
	feed, err := h.Store.ReadReleaseFeed(channel)
	if err != nil {
		log.Printf(`{"event":"list_read_error","channel":"%s","error":"%s"}`, channel, err)
		http.Error(w, `{"error":"failed to read release feed"}`, http.StatusInternalServerError)
		return
	}

	// Group assets by version
	versionMap := map[string]*VersionInfo{}
	// Use insertion order for versions
	var versionOrder []string

	for _, a := range feed.Assets {
		vi, ok := versionMap[a.Version]
		if !ok {
			vi = &VersionInfo{Version: a.Version}
			versionMap[a.Version] = vi
			versionOrder = append(versionOrder, a.Version)
		}
		vi.Files = append(vi.Files, a.FileName)
		vi.TotalSize += a.Size
		vi.FileCount++
	}

	// Build response sorted by semver descending
	versions := make([]VersionInfo, 0, len(versionMap))
	for _, v := range versionOrder {
		versions = append(versions, *versionMap[v])
	}
	sort.Slice(versions, func(i, j int) bool {
		return compareSemverDescending(versions[i].Version, versions[j].Version)
	})

	resp := ListResponse{
		Channel:       channel,
		Versions:      versions,
		TotalVersions: len(versions),
	}

	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(resp)
}

// compareSemverDescending returns true if a > b (for descending sort).
func compareSemverDescending(a, b string) bool {
	ap := parseSemverParts(a)
	bp := parseSemverParts(b)
	for i := 0; i < 3; i++ {
		if ap[i] != bp[i] {
			return ap[i] > bp[i]
		}
	}
	return false
}

// parseSemverParts parses "1.2.3" into [1, 2, 3].
func parseSemverParts(v string) [3]int {
	var result [3]int
	parts := strings.SplitN(v, ".", 4)
	for i, p := range parts {
		if i >= 3 {
			break
		}
		num := 0
		for _, c := range p {
			if c >= '0' && c <= '9' {
				num = num*10 + int(c-'0')
			} else {
				break
			}
		}
		result[i] = num
	}
	return result
}

// extractChannelFromListPath parses /api/channels/{channel}/releases.
func extractChannelFromListPath(path string) (string, error) {
	prefix := "/api/channels/"
	suffix := "/releases"
	if len(path) < len(prefix)+len(suffix)+1 {
		return "", fmt.Errorf("invalid list path: %s", path)
	}
	if path[:len(prefix)] != prefix {
		return "", fmt.Errorf("invalid list path: %s", path)
	}
	if path[len(path)-len(suffix):] != suffix {
		return "", fmt.Errorf("invalid list path: %s", path)
	}
	channel := path[len(prefix) : len(path)-len(suffix)]
	if channel == "" {
		return "", fmt.Errorf("missing channel in path: %s", path)
	}
	return channel, nil
}
