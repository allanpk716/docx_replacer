package handler

import (
	"context"
	"encoding/json"
	"fmt"
	"io"
	"log"
	"net/http"
	"os"
	"path/filepath"
	"strings"

	"update-hub/database"
	"update-hub/model"
	"update-hub/storage"
)

// PromoteResponse is returned after a successful promote operation.
type PromoteResponse struct {
	Promoted    string `json:"promoted"`
	From        string `json:"from"`
	To          string `json:"to"`
	FilesCopied int    `json:"files_copied"`
}

// PromoteHandler handles POST /api/apps/{appId}/channels/{target}/promote.
// It copies .nupkg files and merges feed entries between channels of the same app.
type PromoteHandler struct {
	Store *storage.Store
	DB    *database.DB
}

// NewPromoteHandler creates a PromoteHandler backed by the given store and optional database.
func NewPromoteHandler(store *storage.Store, db *database.DB) *PromoteHandler {
	return &PromoteHandler{Store: store, DB: db}
}

// ServeHTTP processes promote requests.
// Expected: POST /api/apps/{appId}/channels/{target}/promote?from={source}&version={version}
func (h *PromoteHandler) ServeHTTP(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodPost {
		writeJSONError(w, http.StatusMethodNotAllowed, "method not allowed")
		return
	}

	appId := r.PathValue("appId")
	targetChannel := r.PathValue("target")

	if appId == "" || targetChannel == "" {
		log.Printf(`{"event":"promote_bad_path","path":"%s"}`, r.URL.Path)
		writeJSONError(w, http.StatusBadRequest, "missing appId or target channel")
		return
	}

	// Parse query params
	fromChannel := r.URL.Query().Get("from")
	version := r.URL.Query().Get("version")

	if fromChannel == "" {
		writeJSONError(w, http.StatusBadRequest, "missing 'from' query parameter")
		return
	}
	if version == "" {
		writeJSONError(w, http.StatusBadRequest, "missing 'version' query parameter")
		return
	}

	if fromChannel == targetChannel {
		writeJSONError(w, http.StatusBadRequest, "source and target channels must differ")
		return
	}

	// Read source feed files
	srcFeedFiles, err := h.Store.ListFeedFiles(appId, fromChannel)
	if err != nil {
		log.Printf(`{"event":"promote_source_error","appId":"%s","from":"%s","error":"%s"}`, appId, fromChannel, err)
		writeJSONError(w, http.StatusInternalServerError, "failed to read source channel")
		return
	}

	if len(srcFeedFiles) == 0 {
		writeJSONError(w, http.StatusNotFound, fmt.Sprintf("no feeds found in source channel %s", fromChannel))
		return
	}

	// Find matching assets across all source feeds
	var matchingAssets []model.ReleaseAsset
	for _, ff := range srcFeedFiles {
		feed, err := h.Store.ReadReleaseFeed(appId, fromChannel, ff)
		if err != nil {
			continue
		}
		for _, a := range feed.Assets {
			if a.Version == version {
				matchingAssets = append(matchingAssets, a)
			}
		}
	}

	if len(matchingAssets) == 0 {
		log.Printf(`{"event":"promote_version_not_found","appId":"%s","from":"%s","version":"%s"}`, appId, fromChannel, version)
		writeJSONError(w, http.StatusNotFound, fmt.Sprintf("version %s not found in %s", version, fromChannel))
		return
	}

	// Ensure target directory exists
	if err := h.Store.EnsureDir(appId, targetChannel); err != nil {
		log.Printf(`{"event":"promote_mkdir_error","appId":"%s","channel":"%s","error":"%s"}`, appId, targetChannel, err)
		writeJSONError(w, http.StatusInternalServerError, "failed to create target directory")
		return
	}

	// Copy .nupkg files from source to target
	filesCopied := 0
	for _, a := range matchingAssets {
		if a.FileName == "" || !strings.HasSuffix(a.FileName, ".nupkg") {
			continue
		}
		srcPath := filepath.Join(h.Store.ChannelDir(appId, fromChannel), a.FileName)
		dstPath := filepath.Join(h.Store.ChannelDir(appId, targetChannel), a.FileName)

		if err := copyFile(srcPath, dstPath); err != nil {
			if os.IsNotExist(err) {
				log.Printf(`{"event":"promote_skip_missing","appId":"%s","file":"%s"}`, appId, a.FileName)
				continue
			}
			log.Printf(`{"event":"promote_copy_error","appId":"%s","file":"%s","error":"%s"}`, appId, a.FileName, err)
			writeJSONError(w, http.StatusInternalServerError, fmt.Sprintf("copy failed: %s", err))
			return
		}
		filesCopied++
		log.Printf(`{"event":"promote_copy","appId":"%s","from":"%s","to":"%s","file":"%s"}`, appId, fromChannel, targetChannel, a.FileName)
	}

	// Merge promoted assets into ALL target channel feed files.
	// If target has no feeds yet, create feeds using source feed filenames.
	targetFeedFiles, _ := h.Store.ListFeedFiles(appId, targetChannel)
	if len(targetFeedFiles) == 0 {
		targetFeedFiles = srcFeedFiles
	}

	for _, ff := range targetFeedFiles {
		if err := h.mergePromotedAssets(appId, targetChannel, ff, matchingAssets); err != nil {
			log.Printf(`{"event":"promote_merge_error","appId":"%s","channel":"%s","file":"%s","error":"%s"}`, appId, targetChannel, ff, err)
		}
	}

	// Auto-cleanup on target channel
	for _, ff := range targetFeedFiles {
		if removed, err := h.Store.CleanupOldVersions(appId, targetChannel, storage.DefaultMaxKeep, ff); err != nil {
			log.Printf(`{"event":"promote_cleanup_error","appId":"%s","channel":"%s","error":"%s"}`, appId, targetChannel, err)
		} else if len(removed) > 0 {
			log.Printf(`{"event":"promote_cleanup","appId":"%s","channel":"%s","removed":%d}`, appId, targetChannel, len(removed))
		}
	}

	// Sync metadata to SQLite (best-effort)
	if h.DB != nil {
		// Look up source notes if available
		notes := ""
		srcVersions, err := h.DB.GetVersions(context.Background(), appId, fromChannel)
		if err == nil {
			for _, sv := range srcVersions {
				if sv.Version == version {
					notes = sv.Notes
					break
				}
			}
		}
		if err := h.DB.UpsertVersion(context.Background(), appId, targetChannel, version, notes); err != nil {
			log.Printf(`{"event":"metadata_upsert_error","op":"promote_sync","appId":"%s","channel":"%s","version":"%s","error":"%s"}`, appId, targetChannel, version, err)
		}
	}

	resp := PromoteResponse{
		Promoted:    version,
		From:        fromChannel,
		To:          targetChannel,
		FilesCopied: filesCopied,
	}

	log.Printf(`{"event":"promote_success","appId":"%s","version":"%s","from":"%s","to":"%s","files_copied":%d}`,
		appId, version, fromChannel, targetChannel, filesCopied)

	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(resp)
}

// mergePromotedAssets merges assets into a specific target feed file, deduplicating by filename.
func (h *PromoteHandler) mergePromotedAssets(appId, channel, feedFilename string, assets []model.ReleaseAsset) error {
	targetFeed, err := h.Store.ReadReleaseFeed(appId, channel, feedFilename)
	if err != nil {
		return fmt.Errorf("read target feed: %w", err)
	}

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

	return h.Store.WriteReleaseFeed(appId, channel, feedFilename, targetFeed)
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
