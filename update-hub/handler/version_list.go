package handler

import (
	"encoding/json"
	"log"
	"net/http"

	"update-hub/database"
)

// VersionListHandler handles GET /api/apps/{appId}/channels/{channel}/versions —
// returns versions with notes from SQLite metadata.
type VersionListHandler struct {
	DB *database.DB
}

// NewVersionListHandler creates a VersionListHandler backed by the given database.
func NewVersionListHandler(db *database.DB) *VersionListHandler {
	return &VersionListHandler{DB: db}
}

// ServeHTTP returns a JSON array of versions for the given app/channel.
func (h *VersionListHandler) ServeHTTP(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodGet {
		writeJSONError(w, http.StatusMethodNotAllowed, "method not allowed")
		return
	}

	appId := r.PathValue("appId")
	channel := r.PathValue("channel")

	if appId == "" || channel == "" {
		log.Printf(`{"event":"version_list_bad_path","path":"%s"}`, r.URL.Path)
		writeJSONError(w, http.StatusBadRequest, "missing appId or channel")
		return
	}

	if h.DB == nil {
		// No database configured — return empty list
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode([]database.VersionEntry{})
		return
	}

	versions, err := h.DB.GetVersions(r.Context(), appId, channel)
	if err != nil {
		log.Printf(`{"event":"metadata_query_error","op":"get_versions","app":"%s","channel":"%s","error":"%s"}`, appId, channel, err)
		writeJSONError(w, http.StatusInternalServerError, "failed to query versions")
		return
	}

	// Ensure non-null empty array
	if versions == nil {
		versions = []database.VersionEntry{}
	}

	log.Printf(`{"event":"metadata_query","op":"get_versions","app":"%s","channel":"%s","count":%d}`, appId, channel, len(versions))
	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(versions)
}
