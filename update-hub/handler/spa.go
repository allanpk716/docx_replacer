package handler

import (
	"fmt"
	"io/fs"
	"log"
	"mime"
	"net/http"
	"path/filepath"
	"strconv"
	"strings"
)

// SPAHandler serves a single-page application from an embedded filesystem.
// For each request, it tries to serve the exact file from the FS.
// If the file is not found, it falls back to index.html for client-side routing.
type SPAHandler struct {
	fsys      fs.FS
	indexHTML []byte
}

// NewSPAHandler creates a SPAHandler from the given filesystem rooted at the dist directory.
func NewSPAHandler(fsys fs.FS) (*SPAHandler, error) {
	indexHTML, err := fs.ReadFile(fsys, "index.html")
	if err != nil {
		return nil, fmt.Errorf("reading index.html from embedded FS: %w", err)
	}
	log.Printf(`{"event":"spa_setup","index_size":%d}`, len(indexHTML))
	return &SPAHandler{
		fsys:      fsys,
		indexHTML: indexHTML,
	}, nil
}

// ServeHTTP serves embedded files with SPA fallback to index.html.
func (h *SPAHandler) ServeHTTP(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodGet {
		http.Error(w, "method not allowed", http.StatusMethodNotAllowed)
		return
	}

	// Normalize path: strip leading slash
	path := strings.TrimPrefix(r.URL.Path, "/")
	if path == "" {
		path = "index.html"
	}

	// Path traversal protection
	if strings.Contains(path, "..") {
		log.Printf(`{"event":"spa_path_traversal","path":"%s"}`, r.URL.Path)
		http.Error(w, "invalid path", http.StatusBadRequest)
		return
	}

	// Try to serve the exact file from the embedded FS
	data, err := fs.ReadFile(h.fsys, path)
	if err == nil {
		contentType := mime.TypeByExtension(filepath.Ext(path))
		if contentType == "" {
			contentType = "application/octet-stream"
		}
		w.Header().Set("Content-Type", contentType)
		w.Header().Set("Content-Length", strconv.Itoa(len(data)))
		w.Write(data)
		return
	}

	// Fallback to index.html for SPA client-side routing
	w.Header().Set("Content-Type", "text/html; charset=utf-8")
	w.Header().Set("Content-Length", strconv.Itoa(len(h.indexHTML)))
	w.Write(h.indexHTML)
}
