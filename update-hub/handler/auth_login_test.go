package handler

import (
	"encoding/json"
	"net/http"
	"net/http/httptest"
	"strings"
	"testing"
	"time"

	"update-hub/middleware"
)

const testJWTSecret = "test-jwt-secret-key-12345"

func TestLogin_CorrectPassword_ReturnsTokenCookie(t *testing.T) {
	handler := NewLoginHandler("admin-pass", []byte(testJWTSecret))

	body := `{"password":"admin-pass"}`
	req := httptest.NewRequest(http.MethodPost, "/api/auth/login", strings.NewReader(body))
	req.Header.Set("Content-Type", "application/json")
	rr := httptest.NewRecorder()
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d", rr.Code)
	}

	var resp map[string]bool
	if err := json.NewDecoder(rr.Body).Decode(&resp); err != nil {
		t.Fatalf("decode response: %v", err)
	}
	if !resp["ok"] {
		t.Error("expected ok=true")
	}

	// Check session cookie
	cookies := rr.Result().Cookies()
	var sessionCookie *http.Cookie
	for _, c := range cookies {
		if c.Name == "session" {
			sessionCookie = c
			break
		}
	}
	if sessionCookie == nil {
		t.Fatal("expected session cookie to be set")
	}
	if !sessionCookie.HttpOnly {
		t.Error("expected HttpOnly=true")
	}
	if sessionCookie.Path != "/" {
		t.Error("expected Path=/")
	}
	if sessionCookie.MaxAge != 86400 {
		t.Errorf("expected MaxAge=86400, got %d", sessionCookie.MaxAge)
	}

	// Validate the JWT token
	claims, err := middleware.ValidateToken(sessionCookie.Value, []byte(testJWTSecret))
	if err != nil {
		t.Fatalf("validate token: %v", err)
	}
	if claims == nil {
		t.Fatal("expected non-nil claims")
	}
}

func TestLogin_WrongPassword_Returns401(t *testing.T) {
	handler := NewLoginHandler("admin-pass", []byte(testJWTSecret))

	body := `{"password":"wrong-pass"}`
	req := httptest.NewRequest(http.MethodPost, "/api/auth/login", strings.NewReader(body))
	req.Header.Set("Content-Type", "application/json")
	rr := httptest.NewRecorder()
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusUnauthorized {
		t.Fatalf("expected 401, got %d", rr.Code)
	}

	var resp map[string]string
	json.NewDecoder(rr.Body).Decode(&resp)
	if resp["error"] != "invalid password" {
		t.Errorf("unexpected error: %s", resp["error"])
	}

	// No cookie should be set on failure
	cookies := rr.Result().Cookies()
	for _, c := range cookies {
		if c.Name == "session" {
			t.Error("session cookie should not be set on failed login")
		}
	}
}

func TestLogin_NoPasswordConfigured_Returns401(t *testing.T) {
	handler := NewLoginHandler("", []byte(testJWTSecret))

	body := `{"password":"anything"}`
	req := httptest.NewRequest(http.MethodPost, "/api/auth/login", strings.NewReader(body))
	req.Header.Set("Content-Type", "application/json")
	rr := httptest.NewRecorder()
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusUnauthorized {
		t.Fatalf("expected 401, got %d", rr.Code)
	}

	var resp map[string]string
	json.NewDecoder(rr.Body).Decode(&resp)
	if resp["error"] != "login disabled" {
		t.Errorf("unexpected error: %s", resp["error"])
	}
}

func TestLogin_InvalidJSON_Returns400(t *testing.T) {
	handler := NewLoginHandler("admin-pass", []byte(testJWTSecret))

	req := httptest.NewRequest(http.MethodPost, "/api/auth/login", strings.NewReader("not json"))
	req.Header.Set("Content-Type", "application/json")
	rr := httptest.NewRecorder()
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusBadRequest {
		t.Fatalf("expected 400, got %d", rr.Code)
	}
}

func TestLogin_GetMethod_Returns405(t *testing.T) {
	handler := NewLoginHandler("admin-pass", []byte(testJWTSecret))

	req := httptest.NewRequest(http.MethodGet, "/api/auth/login", nil)
	rr := httptest.NewRecorder()
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusMethodNotAllowed {
		t.Fatalf("expected 405, got %d", rr.Code)
	}
}

// ── JWT middleware integration tests ──

func TestAuth_LoginEndpoint_SkipsAuth(t *testing.T) {
	// The login endpoint itself must be accessible without any credentials
	authMiddleware := BearerAuthTest("secret-token", []byte(testJWTSecret))
	loginHandler := NewLoginHandler("admin-pass", []byte(testJWTSecret))

	req := httptest.NewRequest(http.MethodPost, "/api/auth/login", strings.NewReader(`{"password":"admin-pass"}`))
	req.Header.Set("Content-Type", "application/json")
	rr := httptest.NewRecorder()

	// Wrap login handler through the auth middleware
	authMiddleware(loginHandler).ServeHTTP(rr, req)

	if rr.Code != http.StatusOK {
		t.Fatalf("login endpoint should skip auth, got %d", rr.Code)
	}
}

func TestAuth_JWTCookie_ValidToken_PassesThrough(t *testing.T) {
	// Generate a valid JWT
	token, err := middleware.GenerateToken([]byte(testJWTSecret), 24*time.Hour)
	if err != nil {
		t.Fatalf("generate token: %v", err)
	}

	authMiddleware := BearerAuthTest("secret-token", []byte(testJWTSecret))

	req := httptest.NewRequest(http.MethodPost, "/api/apps/docufiller/channels/stable/releases", nil)
	req.AddCookie(&http.Cookie{Name: "session", Value: token})
	rr := httptest.NewRecorder()

	authMiddleware(okHandler).ServeHTTP(rr, req)

	if rr.Code != http.StatusOK {
		t.Fatalf("valid JWT cookie should pass through, got %d", rr.Code)
	}
}

func TestAuth_JWTCookie_ExpiredToken_Returns401(t *testing.T) {
	// Generate an expired JWT
	token, err := middleware.GenerateToken([]byte(testJWTSecret), -1*time.Hour)
	if err != nil {
		t.Fatalf("generate token: %v", err)
	}

	authMiddleware := BearerAuthTest("secret-token", []byte(testJWTSecret))

	req := httptest.NewRequest(http.MethodPost, "/api/apps/docufiller/channels/stable/releases", nil)
	req.AddCookie(&http.Cookie{Name: "session", Value: token})
	rr := httptest.NewRecorder()

	authMiddleware(okHandler).ServeHTTP(rr, req)

	if rr.Code != http.StatusUnauthorized {
		t.Fatalf("expired JWT should return 401, got %d", rr.Code)
	}
}

func TestAuth_JWTCookie_InvalidSignature_Returns401(t *testing.T) {
	// Generate token with wrong secret
	token, err := middleware.GenerateToken([]byte("wrong-secret"), 24*time.Hour)
	if err != nil {
		t.Fatalf("generate token: %v", err)
	}

	authMiddleware := BearerAuthTest("secret-token", []byte(testJWTSecret))

	req := httptest.NewRequest(http.MethodPost, "/api/apps/docufiller/channels/stable/releases", nil)
	req.AddCookie(&http.Cookie{Name: "session", Value: token})
	rr := httptest.NewRecorder()

	authMiddleware(okHandler).ServeHTTP(rr, req)

	if rr.Code != http.StatusUnauthorized {
		t.Fatalf("invalid JWT signature should return 401, got %d", rr.Code)
	}
}

func TestAuth_NoBearerToken_NoCookie_Returns401(t *testing.T) {
	authMiddleware := BearerAuthTest("secret-token", []byte(testJWTSecret))

	req := httptest.NewRequest(http.MethodPost, "/api/apps/docufiller/channels/stable/releases", nil)
	rr := httptest.NewRecorder()

	authMiddleware(okHandler).ServeHTTP(rr, req)

	if rr.Code != http.StatusUnauthorized {
		t.Fatalf("no auth should return 401, got %d", rr.Code)
	}
}

func TestAuth_BearerTokenPreferredOverCookie(t *testing.T) {
	// When both Bearer token and cookie are present, Bearer takes precedence
	authMiddleware := BearerAuthTest("secret-token", []byte(testJWTSecret))

	req := httptest.NewRequest(http.MethodPost, "/api/apps/docufiller/channels/stable/releases", nil)
	req.Header.Set("Authorization", "Bearer secret-token")
	// Also set an invalid cookie — Bearer should be used first and succeed
	req.AddCookie(&http.Cookie{Name: "session", Value: "invalid-jwt"})
	rr := httptest.NewRecorder()

	authMiddleware(okHandler).ServeHTTP(rr, req)

	if rr.Code != http.StatusOK {
		t.Fatalf("Bearer token should take precedence, got %d", rr.Code)
	}
}

func TestAuth_EmptyBearer_FallsBackToCookie(t *testing.T) {
	// When no Bearer token is configured, should fall back to JWT cookie
	authMiddleware := BearerAuthTest("", []byte(testJWTSecret))

	token, err := middleware.GenerateToken([]byte(testJWTSecret), 24*time.Hour)
	if err != nil {
		t.Fatalf("generate token: %v", err)
	}

	req := httptest.NewRequest(http.MethodPost, "/api/apps/docufiller/channels/stable/releases", nil)
	req.AddCookie(&http.Cookie{Name: "session", Value: token})
	rr := httptest.NewRecorder()

	authMiddleware(okHandler).ServeHTTP(rr, req)

	if rr.Code != http.StatusOK {
		t.Fatalf("JWT cookie should work when no Bearer token configured, got %d", rr.Code)
	}
}

// BearerAuthTest wraps the real BearerAuth for testing.
func BearerAuthTest(token string, jwtSecret []byte) func(http.Handler) http.Handler {
	return middleware.BearerAuth(token, jwtSecret)
}

var okHandler = http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
	w.WriteHeader(http.StatusOK)
	w.Write([]byte("ok"))
})
