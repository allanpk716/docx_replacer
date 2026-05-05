package handler

import (
	"net/http"
	"net/http/httptest"
	"testing"
	"testing/fstest"
)

func newTestFS() fstest.MapFS {
	return fstest.MapFS{
		"index.html": &fstest.MapFile{
			Data: []byte("<!DOCTYPE html><html><body>SPA</body></html>"),
		},
		"assets/app.js": &fstest.MapFile{
			Data: []byte("console.log('app')"),
		},
		"assets/app.css": &fstest.MapFile{
			Data: []byte("body { margin: 0; }"),
		},
	}
}

func TestSPAHandler_ServesIndexHTML(t *testing.T) {
	spa, err := NewSPAHandler(newTestFS())
	if err != nil {
		t.Fatalf("NewSPAHandler: %v", err)
	}

	req := httptest.NewRequest(http.MethodGet, "/", nil)
	rec := httptest.NewRecorder()
	spa.ServeHTTP(rec, req)

	if rec.Code != http.StatusOK {
		t.Errorf("status = %d, want %d", rec.Code, http.StatusOK)
	}
	if ct := rec.Header().Get("Content-Type"); ct != "text/html; charset=utf-8" {
		t.Errorf("Content-Type = %q, want %q", ct, "text/html; charset=utf-8")
	}
	body := rec.Body.String()
	if body != "<!DOCTYPE html><html><body>SPA</body></html>" {
		t.Errorf("body = %q, want index.html content", body)
	}
}

func TestSPAHandler_ServesAssets(t *testing.T) {
	spa, err := NewSPAHandler(newTestFS())
	if err != nil {
		t.Fatalf("NewSPAHandler: %v", err)
	}

	tests := []struct {
		path        string
		wantCT      string
		wantContent string
	}{
		{"/assets/app.js", "application/javascript", "console.log('app')"},
		{"/assets/app.css", "text/css; charset=utf-8", "body { margin: 0; }"},
	}

	for _, tt := range tests {
		t.Run(tt.path, func(t *testing.T) {
			req := httptest.NewRequest(http.MethodGet, tt.path, nil)
			rec := httptest.NewRecorder()
			spa.ServeHTTP(rec, req)

			if rec.Code != http.StatusOK {
				t.Errorf("status = %d, want %d", rec.Code, http.StatusOK)
			}
			if ct := rec.Header().Get("Content-Type"); ct != tt.wantCT {
				t.Errorf("Content-Type = %q, want %q", ct, tt.wantCT)
			}
			if body := rec.Body.String(); body != tt.wantContent {
				t.Errorf("body = %q, want %q", body, tt.wantContent)
			}
		})
	}
}

func TestSPAHandler_FallbackToIndex(t *testing.T) {
	spa, err := NewSPAHandler(newTestFS())
	if err != nil {
		t.Fatalf("NewSPAHandler: %v", err)
	}

	// SPA client-side routes should fall back to index.html
	routes := []string{"/login", "/apps", "/apps/myapp", "/settings/profile"}
	for _, route := range routes {
		t.Run(route, func(t *testing.T) {
			req := httptest.NewRequest(http.MethodGet, route, nil)
			rec := httptest.NewRecorder()
			spa.ServeHTTP(rec, req)

			if rec.Code != http.StatusOK {
				t.Errorf("status = %d, want %d", rec.Code, http.StatusOK)
			}
			if ct := rec.Header().Get("Content-Type"); ct != "text/html; charset=utf-8" {
				t.Errorf("Content-Type = %q, want %q", ct, "text/html; charset=utf-8")
			}
			body := rec.Body.String()
			if body != "<!DOCTYPE html><html><body>SPA</body></html>" {
				t.Errorf("body for %s should be index.html fallback, got %q", route, body)
			}
		})
	}
}

func TestSPAHandler_PathTraversal(t *testing.T) {
	spa, err := NewSPAHandler(newTestFS())
	if err != nil {
		t.Fatalf("NewSPAHandler: %v", err)
	}

	tests := []struct {
		name string
		path string
	}{
		{"double_dot", "/../etc/passwd"},
		{"embedded_double_dot", "/assets/../../etc/passwd"},
		{"mixed_traversal", "/foo/../../../etc/shadow"},
	}

	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			req := httptest.NewRequest(http.MethodGet, tt.path, nil)
			rec := httptest.NewRecorder()
			spa.ServeHTTP(rec, req)

			if rec.Code != http.StatusBadRequest {
				t.Errorf("status = %d, want %d", rec.Code, http.StatusBadRequest)
			}
		})
	}
}

func TestSPAHandler_MethodNotAllowed(t *testing.T) {
	spa, err := NewSPAHandler(newTestFS())
	if err != nil {
		t.Fatalf("NewSPAHandler: %v", err)
	}

	for _, method := range []string{http.MethodPost, http.MethodPut, http.MethodDelete} {
		t.Run(method, func(t *testing.T) {
			req := httptest.NewRequest(method, "/", nil)
			rec := httptest.NewRecorder()
			spa.ServeHTTP(rec, req)

			if rec.Code != http.StatusMethodNotAllowed {
				t.Errorf("status = %d, want %d", rec.Code, http.StatusMethodNotAllowed)
			}
		})
	}
}
