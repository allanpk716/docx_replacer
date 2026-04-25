package handler

import (
	"encoding/json"
	"fmt"
	"io"
	"log"
	"net/http"
	"os"
	"path/filepath"
	"strings"

	"docufiller-update-server/model"
	"docufiller-update-server/storage"
)

// PromoteResponse is returned after a successful promote operation.
type PromoteResponse struct {
	Promoted    string `json:"promoted"`
	From        string `json:"from"`
	To          string `json:"to"`
	FilesCopied int    `json:"files_copied"`
}

// PromoteHandler handles POST /api/channels/stable/promote.
type PromoteHandler struct {
	Store *storage.Store
}

// NewPromoteHandler creates a PromoteHandler backed by the given store.
func NewPromoteHandler(store *storage.Store) *PromoteHandler {
	return &PromoteHandler{Store: store}
}

// ServeHTTP processes promote requests.
// Expected path: POST /api/channels/stable/promote?from=beta&version=1.2.0
func (h *PromoteHandler) ServeHTTP(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodPost {
		http.Error(w, `{"error":"method not allowed"}`, http.StatusMethodNotAllowed)
		return
	}

	// Extract target channel from path: /api/channels/{target}/promote
	targetChannel, err := extractChannelFromPromotePath(r.URL.Path)
	if err != nil {
		log.Printf(`{"event":"promote_bad_path","path":"%s","error":"%s"}`, r.URL.Path, err)
		http.Error(w, fmt.Sprintf(`{"error":"%s"}`, err.Error()), http.StatusBadRequest)
		return
	}

	// Validate target channel
	if !validChannels[targetChannel] {
		log.Printf(`{"event":"promote_invalid_target","channel":"%s"}`, targetChannel)
		http.Error(w, fmt.Sprintf(`{"error":"invalid target channel: %s"}`, targetChannel), http.StatusBadRequest)
		return
	}

	// Parse query params
	fromChannel := r.URL.Query().Get("from")
	version := r.URL.Query().Get("version")

	if fromChannel == "" {
		http.Error(w, `{"error":"missing 'from' query parameter"}`, http.StatusBadRequest)
		return
	}
	if version == "" {
		http.Error(w, `{"error":"missing 'version' query parameter"}`, http.StatusBadRequest)
		return
	}

	// Validate source channel
	if !validChannels[fromChannel] {
		log.Printf(`{"event":"promote_invalid_source","channel":"%s"}`, fromChannel)
		http.Error(w, fmt.Sprintf(`{"error":"invalid source channel: %s"}`, fromChannel), http.StatusBadRequest)
		return
	}

	if fromChannel == targetChannel {
		http.Error(w, `{"error":"source and target channels must differ"}`, http.StatusBadRequest)
		return
	}

	// Read source feed
	srcFeed, err := h.Store.ReadReleaseFeed(fromChannel)
	if err != nil {
		log.Printf(`{"event":"promote_read_source_error","channel":"%s","error":"%s"}`, fromChannel, err)
		http.Error(w, `{"error":"failed to read source channel"}`, http.StatusInternalServerError)
		return
	}

	// Find assets matching the requested version
	var matchingAssets []model.ReleaseAsset
	for _, a := range srcFeed.Assets {
		if a.Version == version {
			matchingAssets = append(matchingAssets, a)
		}
	}

	if len(matchingAssets) == 0 {
		log.Printf(`{"event":"promote_version_not_found","from":"%s","version":"%s"}`, fromChannel, version)
		http.Error(w, fmt.Sprintf(`{"error":"version %s not found in %s"}`, version, fromChannel), http.StatusNotFound)
		return
	}

	// Ensure target channel dir exists
	if err := h.Store.EnsureChannelDir(targetChannel); err != nil {
		log.Printf(`{"event":"promote_mkdir_error","channel":"%s","error":"%s"}`, targetChannel, err)
		http.Error(w, `{"error":"failed to create target channel directory"}`, http.StatusInternalServerError)
		return
	}

	// Copy .nupkg files from source to target
	filesCopied := 0
	for _, a := range matchingAssets {
		if a.FileName == "" || !strings.HasSuffix(a.FileName, ".nupkg") {
			continue
		}
		srcPath := filepath.Join(h.Store.DataDir, fromChannel, a.FileName)
		dstPath := filepath.Join(h.Store.DataDir, targetChannel, a.FileName)

		if err := copyFile(srcPath, dstPath); err != nil {
			if os.IsNotExist(err) {
				log.Printf(`{"event":"promote_skip_missing_file","from":"%s","file":"%s"}`, fromChannel, a.FileName)
				continue
			}
			log.Printf(`{"event":"promote_copy_error","from":"%s","to":"%s","file":"%s","error":"%s"}`, fromChannel, targetChannel, a.FileName, err)
			http.Error(w, fmt.Sprintf(`{"error":"copy failed: %s"}`, err.Error()), http.StatusInternalServerError)
			return
		}
		filesCopied++
		log.Printf(`{"event":"promote_copy","from":"%s","to":"%s","file":"%s"}`, fromChannel, targetChannel, a.FileName)
	}

	// Merge promoted assets into target feed
	if err := h.mergePromotedAssets(targetChannel, matchingAssets); err != nil {
		log.Printf(`{"event":"promote_merge_error","channel":"%s","error":"%s"}`, targetChannel, err)
		http.Error(w, `{"error":"failed to merge release feed"}`, http.StatusInternalServerError)
		return
	}

	// Trigger auto-cleanup on target channel
	removed, err := h.Store.CleanupOldVersions(targetChannel, storage.DefaultMaxKeep)
	if err != nil {
		log.Printf(`{"event":"promote_cleanup_error","channel":"%s","error":"%s"}`, targetChannel, err)
		// Non-fatal: promote succeeded, cleanup is best-effort
	} else if len(removed) > 0 {
		log.Printf(`{"event":"promote_cleanup","channel":"%s","removed_versions":%d}`, targetChannel, len(removed))
	}

	resp := PromoteResponse{
		Promoted:    version,
		From:        fromChannel,
		To:          targetChannel,
		FilesCopied: filesCopied,
	}

	log.Printf(`{"event":"promote_success","version":"%s","from":"%s","to":"%s","files_copied":%d}`,
		version, fromChannel, targetChannel, filesCopied)

	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(resp)
}

// mergePromotedAssets merges the promoted assets into the target channel's feed.
func (h *PromoteHandler) mergePromotedAssets(targetChannel string, assets []model.ReleaseAsset) error {
	targetFeed, err := h.Store.ReadReleaseFeed(targetChannel)
	if err != nil {
		return fmt.Errorf("read target feed: %w", err)
	}

	// Build dedup set
	existingNames := map[string]bool{}
	for _, a := range targetFeed.Assets {
		existingNames[a.FileName] = true
	}

	for _, a := range assets {
		if existingNames[a.FileName] {
			continue
		}
		targetFeed.Assets = append(targetFeed.Assets, a)
		existingNames[a.FileName] = true
	}

	return h.Store.WriteReleaseFeed(targetChannel, targetFeed)
}

// extractChannelFromPromotePath parses /api/channels/{channel}/promote.
func extractChannelFromPromotePath(path string) (string, error) {
	prefix := "/api/channels/"
	suffix := "/promote"
	if len(path) < len(prefix)+len(suffix)+1 {
		return "", fmt.Errorf("invalid promote path: %s", path)
	}
	if path[:len(prefix)] != prefix {
		return "", fmt.Errorf("invalid promote path: %s", path)
	}
	if path[len(path)-len(suffix):] != suffix {
		return "", fmt.Errorf("invalid promote path: %s", path)
	}
	channel := path[len(prefix) : len(path)-len(suffix)]
	if channel == "" {
		return "", fmt.Errorf("missing channel in path: %s", path)
	}
	return channel, nil
}

// copyFile copies a single file from src to dst.
func copyFile(src, dst string) error {
	srcFile, err := os.Open(src)
	if err != nil {
		return err
	}
	defer srcFile.Close()

	dstFile, err := os.Create(dst)
	if err != nil {
		return err
	}
	defer dstFile.Close()

	if _, err := io.Copy(dstFile, srcFile); err != nil {
		return err
	}
	return dstFile.Sync()
}
