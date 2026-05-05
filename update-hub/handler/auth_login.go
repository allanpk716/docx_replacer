package handler

import (
	"crypto/subtle"
	"encoding/json"
	"log"
	"net/http"
	"time"

	"update-hub/middleware"
)

// LoginHandler handles POST /api/auth/login for JWT session authentication.
type LoginHandler struct {
	password  string
	jwtSecret []byte
}

// NewLoginHandler creates a LoginHandler with the configured password and JWT secret.
func NewLoginHandler(password string, jwtSecret []byte) *LoginHandler {
	return &LoginHandler{
		password:  password,
		jwtSecret: jwtSecret,
	}
}

// loginRequest is the JSON body expected by the login endpoint.
type loginRequest struct {
	Password string `json:"password"`
}

func (h *LoginHandler) ServeHTTP(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodPost {
		http.Error(w, "method not allowed", http.StatusMethodNotAllowed)
		return
	}

	// If no password is configured, login is disabled
	if h.password == "" {
		log.Printf(`{"event":"login_failed","reason":"no_password"}`)
		writeJSON(w, http.StatusUnauthorized, map[string]string{"error": "login disabled"})
		return
	}

	var req loginRequest
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		log.Printf(`{"event":"login_failed","reason":"invalid_body"}`)
		writeJSON(w, http.StatusBadRequest, map[string]string{"error": "invalid request body"})
		return
	}

	// Timing-safe password comparison
	if subtle.ConstantTimeCompare([]byte(req.Password), []byte(h.password)) != 1 {
		log.Printf(`{"event":"login_failed","reason":"wrong_password"}`)
		writeJSON(w, http.StatusUnauthorized, map[string]string{"error": "invalid password"})
		return
	}

	// Generate JWT token with 24h expiry
	tokenString, err := middleware.GenerateToken(h.jwtSecret, 24*time.Hour)
	if err != nil {
		log.Printf(`{"event":"login_failed","reason":"token_error","error":"%s"}`, err)
		writeJSON(w, http.StatusInternalServerError, map[string]string{"error": "internal error"})
		return
	}

	// Set HttpOnly session cookie
	http.SetCookie(w, &http.Cookie{
		Name:     "session",
		Value:    tokenString,
		Path:     "/",
		MaxAge:   86400, // 24 hours
		HttpOnly: true,
		SameSite: http.SameSiteLaxMode,
		Secure:   false, // internal HTTP
	})

	log.Printf(`{"event":"login_success"}`)
	writeJSON(w, http.StatusOK, map[string]bool{"ok": true})
}

// writeJSON writes a JSON response with the given status code.
func writeJSON(w http.ResponseWriter, code int, v interface{}) {
	w.Header().Set("Content-Type", "application/json")
	w.WriteHeader(code)
	json.NewEncoder(w).Encode(v)
}
