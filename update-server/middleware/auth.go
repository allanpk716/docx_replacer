package middleware

import (
	"log"
	"net/http"
	"strings"
)

// BearerAuth returns middleware that validates Bearer tokens on requests.
// GET requests to static file paths (/{channel}/*) skip authentication.
// The token is compared against the configured value; empty token disables auth.
func BearerAuth(configuredToken string) func(http.Handler) http.Handler {
	return func(next http.Handler) http.Handler {
		return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
			// If no token is configured, allow all requests
			if configuredToken == "" {
				next.ServeHTTP(w, r)
				return
			}

			// Skip auth for GET requests to non-API paths (static file serving)
			// and for GET /api/channels/{channel}/releases (version listing is public)
			if r.Method == http.MethodGet {
				if !strings.HasPrefix(r.URL.Path, "/api/") {
					next.ServeHTTP(w, r)
					return
				}
				// Public: GET /api/channels/{channel}/releases
				if strings.HasSuffix(r.URL.Path, "/releases") && !strings.HasSuffix(r.URL.Path, "/promote") {
					next.ServeHTTP(w, r)
					return
				}
			}

			// All /api/* paths require auth
			authHeader := r.Header.Get("Authorization")
			if authHeader == "" {
				log.Printf(`{"event":"auth_missing","method":"%s","path":"%s"}`, r.Method, r.URL.Path)
				http.Error(w, `{"error":"missing Authorization header"}`, http.StatusUnauthorized)
				return
			}

			parts := strings.SplitN(authHeader, " ", 2)
			if len(parts) != 2 || !strings.EqualFold(parts[0], "Bearer") {
				log.Printf(`{"event":"auth_invalid_scheme","method":"%s","path":"%s"}`, r.Method, r.URL.Path)
				http.Error(w, `{"error":"invalid Authorization header format"}`, http.StatusUnauthorized)
				return
			}

			if parts[1] != configuredToken {
				log.Printf(`{"event":"auth_invalid_token","method":"%s","path":"%s"}`, r.Method, r.URL.Path)
				http.Error(w, `{"error":"invalid token"}`, http.StatusUnauthorized)
				return
			}

			next.ServeHTTP(w, r)
		})
	}
}
