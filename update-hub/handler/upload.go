package handler

import (
	"context"
	"encoding/json"
	"fmt"
	"io"
	"log"
	"net/http"
	"regexp"
	"sort"
	"strings"

	"update-hub/database"
	"update-hub/model"
	"update-hub/storage"
)

// namePattern validates appId and channel names: alphanumeric + hyphen only.
var namePattern = regexp.MustCompile(`^[a-zA-Z0-9-]+$`)

// UploadResponse is the JSON response returned after a successful upload.
type UploadResponse struct {
	Channel       string   `json:"channel"`
	FilesReceived int      `json:"files_received"`
	VersionsAdded []string `json:"versions_added"`
}

// UploadHandler handles POST /api/apps/{appId}/channels/{channel}/releases.
type UploadHandler struct {
	Store *storage.Store
	DB    *database.DB
}

// NewUploadHandler creates an UploadHandler backed by the given store and optional database.
func NewUploadHandler(store *storage.Store, db *database.DB) *UploadHandler {
	return &UploadHandler{Store: store, DB: db}
}

// ServeHTTP processes multipart file uploads for an app/channel.
// It auto-registers apps from the uploaded feed's PackageId and validates
// that the PackageId matches the appId in the URL path.
func (h *UploadHandler) ServeHTTP(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodPost {
		writeJSONError(w, http.StatusMethodNotAllowed, "method not allowed")
		return
	}

	// Extract path parameters using Go 1.22 PathValue
	appId := r.PathValue("appId")
	channel := r.PathValue("channel")

	if appId == "" || channel == "" {
		log.Printf(`{"event":"upload_bad_path","path":"%s","error":"missing appId or channel"}`, r.URL.Path)
		writeJSONError(w, http.StatusBadRequest, "missing appId or channel in path")
		return
	}

	// Validate appId
	if !namePattern.MatchString(appId) {
		log.Printf(`{"event":"upload_invalid_appId","appId":"%s"}`, appId)
		writeJSONError(w, http.StatusBadRequest, "invalid appId: use alphanumeric and hyphen only")
		return
	}

	// Validate channel name
	if !namePattern.MatchString(channel) {
		log.Printf(`{"event":"upload_invalid_channel","channel":"%s","appId":"%s"}`, channel, appId)
		writeJSONError(w, http.StatusBadRequest, "invalid channel name: use alphanumeric and hyphen only")
		return
	}

	// Parse multipart form (max 200MB)
	if err := r.ParseMultipartForm(200 << 20); err != nil {
		log.Printf(`{"event":"upload_parse_error","channel":"%s","appId":"%s","error":"%s"}`, channel, appId, err)
		writeJSONError(w, http.StatusBadRequest, fmt.Sprintf("multipart parse error: %s", err.Error()))
		return
	}

	// Ensure channel directory exists
	if err := h.Store.EnsureDir(appId, channel); err != nil {
		log.Printf(`{"event":"upload_mkdir_error","channel":"%s","appId":"%s","error":"%s"}`, channel, appId, err)
		writeJSONError(w, http.StatusInternalServerError, "failed to create channel directory")
		return
	}

	versionsAdded := map[string]bool{}
	filesReceived := 0
	feedFilename := ""

	// Process each uploaded file
	for _, formFiles := range r.MultipartForm.File {
		for _, fh := range formFiles {
			file, err := fh.Open()
			if err != nil {
				log.Printf(`{"event":"upload_open_error","channel":"%s","appId":"%s","file":"%s","error":"%s"}`, channel, appId, fh.Filename, err)
				continue
			}

			data, err := io.ReadAll(file)
			file.Close()
			if err != nil {
				log.Printf(`{"event":"upload_read_error","channel":"%s","appId":"%s","file":"%s","error":"%s"}`, channel, appId, fh.Filename, err)
				continue
			}

			// Handle feed files (releases.*.json) specially
			if model.IsFeedFilename(fh.Filename) {
				addedVersions, err := h.mergeReleaseFeed(appId, channel, fh.Filename, data)
				if err != nil {
					log.Printf(`{"event":"upload_merge_error","channel":"%s","appId":"%s","file":"%s","error":"%s"}`, channel, appId, fh.Filename, err)
					writeJSONError(w, http.StatusInternalServerError, fmt.Sprintf("merge releases: %s", err.Error()))
					return
				}
				for _, v := range addedVersions {
					versionsAdded[v] = true
				}
				feedFilename = fh.Filename
				log.Printf(`{"event":"upload_feed","channel":"%s","appId":"%s","file":"%s","size":%d,"versions_added":%d}`, channel, appId, fh.Filename, len(data), len(addedVersions))
			} else {
				// Write artifact file directly
				if err := h.Store.WriteFile(appId, channel, fh.Filename, data); err != nil {
					log.Printf(`{"event":"upload_write_error","channel":"%s","appId":"%s","file":"%s","error":"%s"}`, channel, appId, fh.Filename, err)
					writeJSONError(w, http.StatusInternalServerError, fmt.Sprintf("write file: %s", err.Error()))
					return
				}
				// Extract version from .nupkg filename
				if ver := extractVersionFromNupkg(fh.Filename); ver != "" {
					versionsAdded[ver] = true
				}
				log.Printf(`{"event":"upload_file","channel":"%s","appId":"%s","file":"%s","size":%d}`, channel, appId, fh.Filename, len(data))
			}
			filesReceived++
		}
	}

	// Trigger auto-cleanup after upload if we have a feed filename
	if feedFilename != "" {
		if removed, err := h.Store.CleanupOldVersions(appId, channel, storage.DefaultMaxKeep, feedFilename); err != nil {
			log.Printf(`{"event":"upload_cleanup_error","channel":"%s","appId":"%s","error":"%s"}`, channel, appId, err)
		} else if len(removed) > 0 {
			log.Printf(`{"event":"upload_cleanup","channel":"%s","appId":"%s","removed_versions":%d}`, channel, appId, len(removed))
		}
	}

	// Persist metadata to SQLite (best-effort, does not block file operations)
	if h.DB != nil {
		notes := ""
		if vals := r.MultipartForm.Value["notes"]; len(vals) > 0 {
			notes = vals[0]
		}
		ctx := context.Background()
		if err := h.DB.UpsertApp(ctx, appId); err != nil {
			log.Printf(`{"event":"metadata_upsert_error","op":"upsert_app","appId":"%s","error":"%s"}`, appId, err)
		}
		for v := range versionsAdded {
			if err := h.DB.UpsertVersion(ctx, appId, channel, v, notes); err != nil {
				log.Printf(`{"event":"metadata_upsert_error","op":"upsert_version","appId":"%s","channel":"%s","version":"%s","error":"%s"}`, appId, channel, v, err)
			}
		}
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

// mergeReleaseFeed replaces the release feed for an app/channel with the uploaded feed.
// It validates that the feed's PackageId matches the appId in the URL (case-insensitive).
// Returns the list of versions found in the uploaded feed.
func (h *UploadHandler) mergeReleaseFeed(appId, channel, filename string, uploadedData []byte) ([]string, error) {
	// Parse uploaded feed
	var uploaded model.ReleaseFeed
	if err := json.Unmarshal(uploadedData, &uploaded); err != nil {
		return nil, fmt.Errorf("parse uploaded releases: %w", err)
	}

	// Auto-registration: validate PackageId matches appId
	if len(uploaded.Assets) > 0 {
		packageId := ""
		for _, asset := range uploaded.Assets {
			if asset.PackageId != "" {
				packageId = asset.PackageId
				break
			}
		}
		if packageId != "" && !strings.EqualFold(packageId, appId) {
			return nil, fmt.Errorf("package ID mismatch: feed has %s, URL has %s", packageId, appId)
		}
	}

	// Collect versions from uploaded assets
	var addedVersions []string
	for _, asset := range uploaded.Assets {
		if asset.Version != "" {
			addedVersions = append(addedVersions, asset.Version)
		}
	}

	// Replace feed entirely — Velopack expects only the latest release entry
	if err := h.Store.WriteReleaseFeed(appId, channel, filename, &uploaded); err != nil {
		return nil, fmt.Errorf("write release feed: %w", err)
	}

	return addedVersions, nil
}

// extractVersionFromNupkg extracts the version from a .nupkg filename.
// Velopack naming: PackageId-1.2.3-full.nupkg or PackageId-1.2.3-delta.nupkg
func extractVersionFromNupkg(filename string) string {
	if !strings.HasSuffix(filename, ".nupkg") {
		return ""
	}
	base := filename[:len(filename)-6] // strip .nupkg
	// Split by dash, version is the second-to-last part (before -full or -delta)
	parts := strings.Split(base, "-")
	if len(parts) < 2 {
		return ""
	}
	return parts[len(parts)-2]
}

// sortedKeys returns the keys of a map[string]bool as a sorted slice.
func sortedKeys(m map[string]bool) []string {
	keys := make([]string, 0, len(m))
	for k := range m {
		keys = append(keys, k)
	}
	sort.Strings(keys)
	return keys
}

// writeJSONError writes a JSON error response with the given status code.
func writeJSONError(w http.ResponseWriter, status int, message string) {
	w.Header().Set("Content-Type", "application/json")
	w.WriteHeader(status)
	json.NewEncoder(w).Encode(map[string]string{"error": message})
}
