package main

import (
	"flag"
	"fmt"
	"io/fs"
	"log"
	"net/http"
	"path/filepath"
	"time"

	"update-hub/database"
	"update-hub/handler"
	"update-hub/middleware"
	"update-hub/migration"
	"update-hub/storage"

	"github.com/google/uuid"
)

func main() {
	port := flag.Int("port", 30001, "HTTP listen port")
	dataDir := flag.String("data-dir", "./data", "Directory for release artifacts")
	token := flag.String("token", "", "Bearer token for upload/promote/delete APIs (empty = auth disabled)")
	password := flag.String("password", "", "Admin password for Web UI login (empty = login disabled)")
	migrateAppId := flag.String("migrate-app-id", "docufiller", "App ID for old-format directory migration (empty = skip migration)")
	flag.Parse()

	// Generate JWT secret at startup for session cookie signing
	jwtSecret := []byte(uuid.New().String())

	store := storage.NewStore(*dataDir)

	// Initialize SQLite metadata database
	db, err := database.Init(filepath.Join(*dataDir, "update-hub.db"))
	if err != nil {
		log.Fatalf(`{"event":"db_init_error","error":"%s"}`, err)
	}
	defer db.Close()

	// Run old-format directory migration if migrate-app-id is non-empty
	if *migrateAppId != "" {
		log.Printf(`{"event":"migration_start","appId":"%s","dataDir":"%s"}`, *migrateAppId, *dataDir)
		if err := migration.Migrate(*dataDir, *migrateAppId); err != nil {
			log.Fatalf(`{"event":"migration_error","appId":"%s","error":"%s"}`, *migrateAppId, err)
		}
	} else {
		log.Printf(`{"event":"migration_skip","reason":"migrate_app_id_empty"}`)
	}

	// Sync file-system data into SQLite metadata (ensures consistency after migration or for pre-existing data)
	if err := migration.SyncMetadata(*dataDir, db); err != nil {
		log.Fatalf(`{"event":"sync_metadata_error","error":"%s"}`, err)
	}

	// Create handlers
	uploadHandler := handler.NewUploadHandler(store, db)
	listHandler := handler.NewListHandler(store)
	promoteHandler := handler.NewPromoteHandler(store, db)
	deleteHandler := handler.NewDeleteHandler(store, db)
	staticHandler := handler.NewStaticHandler(store)
	appListHandler := handler.NewAppListHandler(db)
	versionListHandler := handler.NewVersionListHandler(db)

	// Go 1.22 ServeMux with method+path patterns
	mux := http.NewServeMux()

	// API routes (write operations protected by auth middleware)
	mux.HandleFunc("POST /api/apps/{appId}/channels/{channel}/releases", uploadHandler.ServeHTTP)
	mux.HandleFunc("GET /api/apps/{appId}/channels/{channel}/releases", listHandler.ServeHTTP)
	mux.HandleFunc("POST /api/apps/{appId}/channels/{target}/promote", promoteHandler.ServeHTTP)
	mux.HandleFunc("DELETE /api/apps/{appId}/channels/{channel}/versions/{version}", deleteHandler.ServeHTTP)

	// Metadata query endpoints for Web UI
	mux.HandleFunc("GET /api/apps", appListHandler.ServeHTTP)
	mux.HandleFunc("GET /api/apps/{appId}/channels/{channel}/versions", versionListHandler.ServeHTTP)

	// Auth endpoints for Web UI
	loginHandler := handler.NewLoginHandler(*password, jwtSecret)
	mux.HandleFunc("POST /api/auth/login", loginHandler.ServeHTTP)

	authCheckHandler := handler.NewAuthCheckHandler(jwtSecret)
	mux.HandleFunc("GET /api/auth/check", authCheckHandler.ServeHTTP)

	// Static file serving for Velopack SimpleWebSource compatibility
	// Matches /{appId}/{channel}/{filename} — catch-all for non-API paths
	mux.Handle("/{appId}/", staticHandler)

	// SPA: serve embedded Vue frontend
	webDistFS, err := fs.Sub(webDist, "web/dist")
	if err != nil {
		log.Fatalf(`{"event":"spa_embed_error","error":"%s"}`, err)
	}
	spaHandler, err := handler.NewSPAHandler(webDistFS)
	if err != nil {
		log.Fatalf(`{"event":"spa_setup_error","error":"%s"}`, err)
	}
	// /assets/ (literal) serves Vite build assets — literal beats /{appId}/ wildcard
	mux.Handle("/assets/", spaHandler)
	// / (catch-all) serves SPA index.html with client-side routing fallback
	mux.Handle("/", spaHandler)

	// Apply auth middleware (skips GET to non-API paths, skips GET /api/.../releases, skips login)
	authed := middleware.BearerAuth(*token, jwtSecret)(mux)

	addr := fmt.Sprintf(":%d", *port)
	server := &http.Server{
		Addr:         addr,
		Handler:      loggingMiddleware(authed),
		ReadTimeout:  30 * time.Second,
		WriteTimeout: 60 * time.Second,
		IdleTimeout:  120 * time.Second,
	}

	log.Printf(`{"event":"startup","addr":"%s","data_dir":"%s","token":"%s","jwt_secret":"(generated)"}`, addr, *dataDir, tokenStatus(*token))
	if err := server.ListenAndServe(); err != nil {
		log.Fatalf(`{"event":"server_failed","error":"%s"}`, err)
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
