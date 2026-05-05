package handler

import (
	"encoding/json"
	"log"
	"net/http"

	"update-hub/database"
)

// AppListHandler handles GET /api/apps — returns all registered apps with their channels.
type AppListHandler struct {
	DB *database.DB
}

// NewAppListHandler creates an AppListHandler backed by the given database.
func NewAppListHandler(db *database.DB) *AppListHandler {
	return &AppListHandler{DB: db}
}

// ServeHTTP returns a JSON array of all registered apps with derived channels.
func (h *AppListHandler) ServeHTTP(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodGet {
		writeJSONError(w, http.StatusMethodNotAllowed, "method not allowed")
		return
	}

	if h.DB == nil {
		// No database configured — return empty list
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode([]database.AppInfo{})
		return
	}

	apps, err := h.DB.GetApps(r.Context())
	if err != nil {
		log.Printf(`{"event":"metadata_query_error","op":"get_apps","error":"%s"}`, err)
		writeJSONError(w, http.StatusInternalServerError, "failed to query apps")
		return
	}

	// Ensure non-null empty array
	if apps == nil {
		apps = []database.AppInfo{}
	}

	log.Printf(`{"event":"metadata_query","op":"get_apps","count":%d}`, len(apps))
	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(apps)
}
