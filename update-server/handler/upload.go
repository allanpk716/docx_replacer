package handler

import (
	"encoding/json"
	"fmt"
	"log"
	"net/http"
	"regexp"

	"docufiller-update-server/model"
	"docufiller-update-server/storage"
)

// channelNamePattern validates channel names: alphanumeric + hyphen only.
var channelNamePattern = regexp.MustCompile(`^[a-zA-Z0-9-]+$`)

// UploadResponse is the JSON response returned after a successful upload.
type UploadResponse struct {
	Channel       string   `json:"channel"`
	FilesReceived int      `json:"files_received"`
	VersionsAdded []string `json:"versions_added"`
}

// UploadHandler handles POST /api/channels/{channel}/releases.
type UploadHandler struct {
	Store *storage.Store
}

// NewUploadHandler creates an UploadHandler backed by the given store.
func NewUploadHandler(store *storage.Store) *UploadHandler {
	return &UploadHandler{Store: store}
}

// ServeHTTP processes multipart file uploads for a channel.
func (h *UploadHandler) ServeHTTP(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodPost {
		http.Error(w, `{"error":"method not allowed"}`, http.StatusMethodNotAllowed)
		return
	}

	// Extract channel from path: /api/channels/{channel}/releases
	channel, err := extractChannelFromUploadPath(r.URL.Path)
	if err != nil {
		log.Printf(`{"event":"upload_bad_path","path":"%s","error":"%s"}`, r.URL.Path, err)
		http.Error(w, fmt.Sprintf(`{"error":"%s"}`, err.Error()), http.StatusBadRequest)
		return
	}

	// Validate channel name
	if !channelNamePattern.MatchString(channel) {
		log.Printf(`{"event":"upload_invalid_channel","channel":"%s"}`, channel)
		http.Error(w, `{"error":"invalid channel name: use alphanumeric and hyphen only"}`, http.StatusBadRequest)
		return
	}

	// Parse multipart form (max 200MB)
	if err := r.ParseMultipartForm(200 << 20); err != nil {
		log.Printf(`{"event":"upload_parse_error","channel":"%s","error":"%s"}`, channel, err)
		http.Error(w, fmt.Sprintf(`{"error":"multipart parse error: %s"}`, err.Error()), http.StatusBadRequest)
		return
	}

	// Ensure channel directory exists
	if err := h.Store.EnsureChannelDir(channel); err != nil {
		log.Printf(`{"event":"upload_mkdir_error","channel":"%s","error":"%s"}`, channel, err)
		http.Error(w, `{"error":"failed to create channel directory"}`, http.StatusInternalServerError)
		return
	}

	versionsAdded := map[string]bool{}
	filesReceived := 0

	// Process each uploaded file
	for _, formFiles := range r.MultipartForm.File {
		for _, fh := range formFiles {
			file, err := fh.Open()
			if err != nil {
				log.Printf(`{"event":"upload_open_error","channel":"%s","file":"%s","error":"%s"}`, channel, fh.Filename, err)
				continue
			}

			data := make([]byte, fh.Size)
			_, err = readFull(file, data)
			file.Close()
			if err != nil {
				log.Printf(`{"event":"upload_read_error","channel":"%s","file":"%s","error":"%s"}`, channel, fh.Filename, err)
				continue
			}

			// Handle releases.win.json specially: merge with existing feed
			if fh.Filename == "releases.win.json" {
				addedVersions, err := h.mergeReleaseFeed(channel, data)
				if err != nil {
					log.Printf(`{"event":"upload_merge_error","channel":"%s","error":"%s"}`, channel, err)
					http.Error(w, fmt.Sprintf(`{"error":"merge releases: %s"}`, err.Error()), http.StatusInternalServerError)
					return
				}
				for _, v := range addedVersions {
					versionsAdded[v] = true
				}
				log.Printf(`{"event":"upload_feed","channel":"%s","size":%d,"versions_added":%d}`, channel, len(data), len(addedVersions))
			} else {
				// Write artifact file directly
				if err := h.Store.WriteFile(channel, fh.Filename, data); err != nil {
					log.Printf(`{"event":"upload_write_error","channel":"%s","file":"%s","error":"%s"}`, channel, fh.Filename, err)
					http.Error(w, fmt.Sprintf(`{"error":"write file: %s"}`, err.Error()), http.StatusInternalServerError)
					return
				}
				// Extract version from .nupkg filename (Velopack naming: PackageId-version-full.nupkg)
				if ver := extractVersionFromNupkg(fh.Filename); ver != "" {
					versionsAdded[ver] = true
				}
				log.Printf(`{"event":"upload_file","channel":"%s","file":"%s","size":%d}`, channel, fh.Filename, len(data))
			}
			filesReceived++
		}
	}

	// Trigger auto-cleanup after upload
	if removed, err := h.Store.CleanupOldVersions(channel, storage.DefaultMaxKeep); err != nil {
		log.Printf(`{"event":"upload_cleanup_error","channel":"%s","error":"%s"}`, channel, err)
	} else if len(removed) > 0 {
		log.Printf(`{"event":"upload_cleanup","channel":"%s","removed_versions":%d}`, channel, len(removed))
	}

	// Build response
	versionList := sortedKeys(versionsAdded)
	resp := UploadResponse{
		Channel:       channel,
		FilesReceived: filesReceived,
		VersionsAdded: versionList,
	}

	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(resp)
}

// mergeReleaseFeed replaces the release feed for a channel with the uploaded feed.
// Velopack expects releases.win.json to contain only the latest version entry.
// Each upload replaces the entire feed, but .nupkg files are retained for delta updates.
func (h *UploadHandler) mergeReleaseFeed(channel string, uploadedData []byte) ([]string, error) {
	// Parse uploaded feed
	var uploaded model.ReleaseFeed
	if err := json.Unmarshal(uploadedData, &uploaded); err != nil {
		return nil, fmt.Errorf("parse uploaded releases: %w", err)
	}

	// Collect versions from uploaded assets
	var addedVersions []string
	for _, asset := range uploaded.Assets {
		if asset.Version != "" {
			addedVersions = append(addedVersions, asset.Version)
		}
	}

	// Replace feed entirely — Velopack expects only the latest release entry
	if err := h.Store.WriteReleaseFeed(channel, &uploaded); err != nil {
		return nil, fmt.Errorf("write release feed: %w", err)
	}

	return addedVersions, nil
}

// extractChannelFromUploadPath parses /api/channels/{channel}/releases.
func extractChannelFromUploadPath(path string) (string, error) {
	// Expected: /api/channels/{channel}/releases
	prefix := "/api/channels/"
	suffix := "/releases"
	if len(path) < len(prefix)+len(suffix)+1 {
		return "", fmt.Errorf("invalid upload path: %s", path)
	}
	if path[:len(prefix)] != prefix {
		return "", fmt.Errorf("invalid upload path: %s", path)
	}
	if path[len(path)-len(suffix):] != suffix {
		return "", fmt.Errorf("invalid upload path: %s", path)
	}
	channel := path[len(prefix) : len(path)-len(suffix)]
	if channel == "" {
		return "", fmt.Errorf("missing channel in path: %s", path)
	}
	return channel, nil
}

// extractVersionFromNupkg extracts the version from a .nupkg filename.
// Velopack naming: PackageId-1.2.3-full.nupkg or PackageId-1.2.3-delta.nupkg
func extractVersionFromNupkg(filename string) string {
	if len(filename) < 7 || filename[len(filename)-6:] != ".nupkg" {
		return ""
	}
	base := filename[:len(filename)-6]
	// Split by dash, version is the second-to-last part (before -full or -delta)
	parts := splitDash(base)
	if len(parts) < 2 {
		return ""
	}
	return parts[len(parts)-2]
}

// splitDash splits string by '-' but returns the whole string as single element if no dash.
func splitDash(s string) []string {
	result := []string{}
	start := 0
	for i := 0; i < len(s); i++ {
		if s[i] == '-' {
			result = append(result, s[start:i])
			start = i + 1
		}
	}
	result = append(result, s[start:])
	return result
}

// readFull reads exactly len(buf) bytes from the reader.
func readFull(r interface{ Read([]byte) (int, error) }, buf []byte) (int, error) {
	total := 0
	for total < len(buf) {
		n, err := r.Read(buf[total:])
		total += n
		if err != nil {
			return total, err
		}
	}
	return total, nil
}

// sortedKeys returns the keys of a map[string]bool as a sorted slice.
func sortedKeys(m map[string]bool) []string {
	keys := make([]string, 0, len(m))
	for k := range m {
		keys = append(keys, k)
	}
	// Simple insertion sort (maps are small)
	for i := 1; i < len(keys); i++ {
		for j := i; j > 0 && keys[j] < keys[j-1]; j-- {
			keys[j], keys[j-1] = keys[j-1], keys[j]
		}
	}
	return keys
}
