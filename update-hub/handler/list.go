package handler

import (
	"encoding/json"
	"log"
	"net/http"
	"sort"
	"strings"

	"update-hub/storage"
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
	Channel       string        `json:"channel"`
	Versions      []VersionInfo `json:"versions"`
	TotalVersions int           `json:"total_versions"`
}

// ListHandler handles GET /api/apps/{appId}/channels/{channel}/releases.
// It reads all releases.*.json feed files in the channel and merges them
// into a single version list sorted descending by semver.
type ListHandler struct {
	Store *storage.Store
}

// NewListHandler creates a ListHandler backed by the given store.
func NewListHandler(store *storage.Store) *ListHandler {
	return &ListHandler{Store: store}
}

// ServeHTTP returns the version list for an app/channel, merging all OS feeds.
func (h *ListHandler) ServeHTTP(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodGet {
		writeJSONError(w, http.StatusMethodNotAllowed, "method not allowed")
		return
	}

	appId := r.PathValue("appId")
	channel := r.PathValue("channel")

	if appId == "" || channel == "" {
		log.Printf(`{"event":"list_bad_path","path":"%s"}`, r.URL.Path)
		writeJSONError(w, http.StatusBadRequest, "missing appId or channel")
		return
	}

	// Find all feed files in the channel
	feedFiles, err := h.Store.ListFeedFiles(appId, channel)
	if err != nil {
		log.Printf(`{"event":"list_error","appId":"%s","channel":"%s","error":"%s"}`, appId, channel, err)
		writeJSONError(w, http.StatusInternalServerError, "failed to list feed files")
		return
	}

	if len(feedFiles) == 0 {
		// No feeds yet — return empty list
		resp := ListResponse{
			Channel:       channel,
			Versions:      []VersionInfo{},
			TotalVersions: 0,
		}
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(resp)
		return
	}

	// Read and merge assets from all OS feed files
	versionMap := map[string]*VersionInfo{}
	for _, ff := range feedFiles {
		feed, err := h.Store.ReadReleaseFeed(appId, channel, ff)
		if err != nil {
			log.Printf(`{"event":"list_feed_error","appId":"%s","channel":"%s","file":"%s","error":"%s"}`, appId, channel, ff, err)
			continue
		}
		for _, a := range feed.Assets {
			vi, ok := versionMap[a.Version]
			if !ok {
				vi = &VersionInfo{Version: a.Version}
				versionMap[a.Version] = vi
			}
			vi.Files = append(vi.Files, a.FileName)
			vi.TotalSize += a.Size
			vi.FileCount++
		}
	}

	// Build response sorted by semver descending
	versions := make([]VersionInfo, 0, len(versionMap))
	for _, vi := range versionMap {
		versions = append(versions, *vi)
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
