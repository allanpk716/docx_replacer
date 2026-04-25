package main

import (
	"flag"
	"fmt"
	"log"
	"net/http"
	"time"

	"docufiller-update-server/handler"
	"docufiller-update-server/middleware"
	"docufiller-update-server/storage"
)

func main() {
	port := flag.Int("port", 8080, "HTTP listen port")
	dataDir := flag.String("data-dir", "./data", "Directory for release artifacts")
	token := flag.String("token", "", "Bearer token for upload/promote APIs")
	flag.Parse()

	store := storage.NewStore(*dataDir)

	// Ensure base directory exists
	if err := store.EnsureChannelDir("stable"); err != nil {
		log.Fatalf("failed to create stable channel dir: %v", err)
	}
	if err := store.EnsureChannelDir("beta"); err != nil {
		log.Fatalf("failed to create beta channel dir: %v", err)
	}

	mux := http.NewServeMux()

	// Static file serving for release artifacts (no auth required for GET)
	staticHandler := handler.NewStaticHandler(store)
	mux.Handle("/", staticHandler)

	// API routes: /api/channels/{channel}/releases (upload/list) and /api/channels/{channel}/promote
	apiHandler := handler.NewAPIHandler(store)
	mux.Handle("/api/channels/", apiHandler)

	// Apply auth middleware (skips GET to non-API paths)
	authed := middleware.BearerAuth(*token)(mux)

	addr := fmt.Sprintf(":%d", *port)
	server := &http.Server{
		Addr:         addr,
		Handler:      loggingMiddleware(authed),
		ReadTimeout:  30 * time.Second,
		WriteTimeout: 60 * time.Second,
		IdleTimeout:  120 * time.Second,
	}

	log.Printf("update-server: listening on %s, data-dir=%s, token=%s", addr, *dataDir, tokenStatus(*token))
	if err := server.ListenAndServe(); err != nil {
		log.Fatalf("server failed: %v", err)
	}
}

func tokenStatus(token string) string {
	if token == "" {
		return "(none)"
	}
	return "configured"
}

// loggingMiddleware logs each HTTP request as structured JSON.
func loggingMiddleware(next http.Handler) http.Handler {
	return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		start := time.Now()
		rec := &responseRecorder{ResponseWriter: w, statusCode: http.StatusOK}
		next.ServeHTTP(rec, r)
		duration := time.Since(start)
		log.Printf(`{"method":"%s","path":"%s","status":%d,"duration_ms":%.1f}`,
			r.Method, r.URL.Path, rec.statusCode, float64(duration.Microseconds())/1000.0)
	})
}

// responseRecorder wraps http.ResponseWriter to capture the status code.
type responseRecorder struct {
	http.ResponseWriter
	statusCode int
}

func (r *responseRecorder) WriteHeader(code int) {
	r.statusCode = code
	r.ResponseWriter.WriteHeader(code)
}
