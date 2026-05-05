package handler

import (
	"context"
	"encoding/json"
	"log"
	"net/http"

	"update-hub/database"
	"update-hub/model"
	"update-hub/storage"
)

// DeleteResponse is returned after a successful delete operation.
type DeleteResponse struct {
	Channel      string `json:"channel"`
	Version      string `json:"version"`
	FilesDeleted int    `json:"files_deleted"`
}

// DeleteHandler handles DELETE /api/apps/{appId}/channels/{channel}/versions/{version}.
// It removes matching .nupkg files and removes entries from all releases.*.json feed files.
type DeleteHandler struct {
	Store *storage.Store
	DB    *database.DB
}

// NewDeleteHandler creates a DeleteHandler backed by the given store and optional database.
func NewDeleteHandler(store *storage.Store, db *database.DB) *DeleteHandler {
	return &DeleteHandler{Store: store, DB: db}
}

// ServeHTTP processes delete requests.
// Expected: DELETE /api/apps/{appId}/channels/{channel}/versions/{version}
func (h *DeleteHandler) ServeHTTP(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodDelete {
		writeJSONError(w, http.StatusMethodNotAllowed, "method not allowed")
		return
	}

	appId := r.PathValue("appId")
	channel := r.PathValue("channel")
	version := r.PathValue("version")

	if appId == "" || channel == "" || version == "" {
		log.Printf(`{"event":"delete_bad_path","path":"%s"}`, r.URL.Path)
		writeJSONError(w, http.StatusBadRequest, "missing appId, channel, or version")
		return
	}

	// Delete matching .nupkg files from disk
	if err := h.Store.DeleteVersion(appId, channel, version); err != nil {
		log.Printf(`{"event":"delete_version_error","appId":"%s","channel":"%s","version":"%s","error":"%s"}`, appId, channel, version, err)
		writeJSONError(w, http.StatusInternalServerError, "failed to delete version files")
		return
	}

	// Update all feed files to remove matching assets
	feedFiles, err := h.Store.ListFeedFiles(appId, channel)
	if err != nil {
		log.Printf(`{"event":"delete_list_feeds_error","appId":"%s","channel":"%s","error":"%s"}`, appId, channel, err)
		writeJSONError(w, http.StatusInternalServerError, "failed to list feed files")
		return
	}

	filesDeleted := 0
	for _, ff := range feedFiles {
		feed, err := h.Store.ReadReleaseFeed(appId, channel, ff)
		if err != nil {
			log.Printf(`{"event":"delete_feed_read_error","appId":"%s","channel":"%s","file":"%s","error":"%s"}`, appId, channel, ff, err)
			continue
		}

		var kept []model.ReleaseAsset
		removed := 0
		for _, a := range feed.Assets {
			if a.Version == version {
				removed++
			} else {
				kept = append(kept, a)
			}
		}

		if removed > 0 {
			feed.Assets = kept
			if err := h.Store.WriteReleaseFeed(appId, channel, ff, feed); err != nil {
				log.Printf(`{"event":"delete_feed_write_error","appId":"%s","channel":"%s","file":"%s","error":"%s"}`, appId, channel, ff, err)
			}
			filesDeleted += removed
		}
	}

	log.Printf(`{"event":"delete_success","appId":"%s","channel":"%s","version":"%s","files_deleted":%d}`,
		appId, channel, version, filesDeleted)

	// Remove metadata from SQLite (best-effort)
	if h.DB != nil {
		if err := h.DB.DeleteVersion(context.Background(), appId, channel, version); err != nil {
			log.Printf(`{"event":"metadata_delete_error","appId":"%s","channel":"%s","version":"%s","error":"%s"}`, appId, channel, version, err)
		}
	}

	resp := DeleteResponse{
		Channel:      channel,
		Version:      version,
		FilesDeleted: filesDeleted,
	}

	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(resp)
}
