package handler

import (
	"net/http"
	"strings"

	"docufiller-update-server/storage"
)

// APIHandler routes requests under /api/channels/ to the appropriate sub-handler
// based on path suffix and HTTP method.
type APIHandler struct {
	upload  *UploadHandler
	promote *PromoteHandler
	list    *ListHandler
}

// NewAPIHandler creates a new API multiplexer.
func NewAPIHandler(store *storage.Store) *APIHandler {
	return &APIHandler{
		upload:  NewUploadHandler(store),
		promote: NewPromoteHandler(store),
		list:    NewListHandler(store),
	}
}

// ServeHTTP dispatches to the appropriate sub-handler based on path and method.
func (h *APIHandler) ServeHTTP(w http.ResponseWriter, r *http.Request) {
	path := r.URL.Path

	switch {
	case strings.HasSuffix(path, "/promote"):
		h.promote.ServeHTTP(w, r)
	case strings.HasSuffix(path, "/releases"):
		if r.Method == http.MethodGet {
			h.list.ServeHTTP(w, r)
		} else {
			h.upload.ServeHTTP(w, r)
		}
	default:
		http.Error(w, `{"error":"not found"}`, http.StatusNotFound)
	}
}
