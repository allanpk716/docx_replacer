package middleware

import (
	"crypto/subtle"
	"encoding/json"
	"log"
	"net/http"
	"strings"
)

// BearerAuth returns middleware that validates Bearer tokens and/or JWT session cookies on requests.
// GET requests to non-API paths (Velopack static serving) skip authentication.
// GET /api/* requests are public (version listing).
// POST /api/auth/login is always exempt from authentication.
// If both configuredToken and jwtSecret are empty, auth is disabled entirely.
// jwtSecret is optional — pass nil to disable JWT cookie auth.
func BearerAuth(configuredToken string, jwtSecret []byte) func(http.Handler) http.Handler {
	return func(next http.Handler) http.Handler {
		return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
			// If no token and no JWT secret is configured, allow all requests (auth disabled)
			if configuredToken == "" && len(jwtSecret) == 0 {
				next.ServeHTTP(w, r)
				return
			}

			// Skip auth for GET requests to non-API paths (static file serving)
			if r.Method == http.MethodGet && !strings.HasPrefix(r.URL.Path, "/api/") {
				next.ServeHTTP(w, r)
				return
			}

			// Skip auth for all GET /api/* requests (public read-only queries:
			// /api/apps, /api/apps/{id}/channels/{ch}/releases, /api/apps/{id}/channels/{ch}/versions)
			if r.Method == http.MethodGet && strings.HasPrefix(r.URL.Path, "/api/") {
				next.ServeHTTP(w, r)
				return
			}

			// Skip auth for login endpoint (must be accessible without credentials)
			if r.URL.Path == "/api/auth/login" {
				next.ServeHTTP(w, r)
				return
			}

			// Try Bearer token auth first
			if configuredToken != "" {
				authHeader := r.Header.Get("Authorization")
				if authHeader != "" {
					parts := strings.SplitN(authHeader, " ", 2)
					if len(parts) == 2 && strings.EqualFold(parts[0], "Bearer") {
						if subtle.ConstantTimeCompare([]byte(parts[1]), []byte(configuredToken)) == 1 {
							next.ServeHTTP(w, r)
							return
						}
						log.Printf(`{"event":"auth_invalid_token","method":"%s","path":"%s"}`, r.Method, r.URL.Path)
						writeAuthError(w, "invalid token")
						return
					}
					log.Printf(`{"event":"auth_invalid_scheme","method":"%s","path":"%s"}`, r.Method, r.URL.Path)
					writeAuthError(w, "invalid Authorization header format")
					return
				}
			}

			// Try JWT session cookie auth
			if len(jwtSecret) > 0 {
				cookie, err := r.Cookie("session")
				if err == nil && cookie.Value != "" {
					claims, err := ValidateToken(cookie.Value, jwtSecret)
					if err == nil && claims != nil {
						next.ServeHTTP(w, r)
						return
					}
					// Distinguish expired vs invalid for observability
					if err != nil {
						log.Printf(`{"event":"jwt_invalid","method":"%s","path":"%s","error":"%s"}`, r.Method, r.URL.Path, err)
					}
				}
			}

			// No valid auth found
			log.Printf(`{"event":"auth_missing","method":"%s","path":"%s"}`, r.Method, r.URL.Path)
			writeAuthError(w, "authentication required")
		})
	}
}

// writeAuthError writes a 401 JSON error response.
func writeAuthError(w http.ResponseWriter, message string) {
	w.Header().Set("Content-Type", "application/json")
	w.WriteHeader(http.StatusUnauthorized)
	json.NewEncoder(w).Encode(map[string]string{"error": message})
}
