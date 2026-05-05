package handler

import (
	"net/http"

	"update-hub/middleware"
)

// AuthCheckHandler handles GET /api/auth/check for verifying JWT session status.
type AuthCheckHandler struct {
	jwtSecret []byte
}

// NewAuthCheckHandler creates an AuthCheckHandler with the JWT secret.
func NewAuthCheckHandler(jwtSecret []byte) *AuthCheckHandler {
	return &AuthCheckHandler{jwtSecret: jwtSecret}
}

func (h *AuthCheckHandler) ServeHTTP(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodGet {
		http.Error(w, "method not allowed", http.StatusMethodNotAllowed)
		return
	}

	// If no JWT secret configured, auth is disabled — always authenticated
	if len(h.jwtSecret) == 0 {
		writeJSON(w, http.StatusOK, map[string]bool{"ok": true})
		return
	}

	// Check session cookie
	cookie, err := r.Cookie("session")
	if err != nil || cookie.Value == "" {
		writeJSON(w, http.StatusUnauthorized, map[string]string{"error": "no session"})
		return
	}

	claims, err := middleware.ValidateToken(cookie.Value, h.jwtSecret)
	if err != nil || claims == nil {
		writeJSON(w, http.StatusUnauthorized, map[string]string{"error": "invalid session"})
		return
	}

	writeJSON(w, http.StatusOK, map[string]bool{"ok": true})
}
