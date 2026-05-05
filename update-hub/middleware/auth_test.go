package middleware

import (
	"encoding/json"
	"net/http"
	"net/http/httptest"
	"testing"
)

// testHandler is a simple handler that returns 200 OK with "ok" body.
var testHandler = http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
	w.WriteHeader(http.StatusOK)
	w.Write([]byte("ok"))
})

// ── Auth tests ──

func TestAuth_GETStaticPath_SkipsAuth(t *testing.T) {
	handler := BearerAuth("secret-token", nil)(testHandler)

	req := httptest.NewRequest(http.MethodGet, "/docufiller/stable/DocuFiller-1.0.0-full.nupkg", nil)
	rr := httptest.NewRecorder()
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusOK {
		t.Errorf("GET static path should skip auth, got %d", rr.Code)
	}
}

func TestAuth_GETStaticRootPath_SkipsAuth(t *testing.T) {
	handler := BearerAuth("secret-token", nil)(testHandler)

	req := httptest.NewRequest(http.MethodGet, "/docufiller/stable/releases.win.json", nil)
	rr := httptest.NewRecorder()
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusOK {
		t.Errorf("GET static feed path should skip auth, got %d", rr.Code)
	}
}

func TestAuth_GETReleasesEndpoint_SkipsAuth(t *testing.T) {
	handler := BearerAuth("secret-token", nil)(testHandler)

	// GET /api/apps/{appId}/channels/{channel}/releases — public version list
	req := httptest.NewRequest(http.MethodGet, "/api/apps/docufiller/channels/stable/releases", nil)
	rr := httptest.NewRecorder()
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusOK {
		t.Errorf("GET /api/.../releases should skip auth, got %d", rr.Code)
	}
}

func TestAuth_GETNonReleasesAPI_SkipsAuth(t *testing.T) {
	handler := BearerAuth("secret-token", nil)(testHandler)

	req := httptest.NewRequest(http.MethodGet, "/api/apps/docufiller/channels", nil)
	rr := httptest.NewRecorder()
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusOK {
		t.Errorf("GET /api/... should be public (read-only), got %d", rr.Code)
	}
}

func TestAuth_POSTWithoutToken_Returns401(t *testing.T) {
	handler := BearerAuth("secret-token", nil)(testHandler)

	req := httptest.NewRequest(http.MethodPost, "/api/apps/docufiller/channels/stable/releases", nil)
	rr := httptest.NewRecorder()
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusUnauthorized {
		t.Fatalf("POST without token should return 401, got %d", rr.Code)
	}

	var errResp map[string]string
	if err := json.NewDecoder(rr.Body).Decode(&errResp); err != nil {
		t.Fatalf("decode error response: %v", err)
	}
	if errResp["error"] != "authentication required" {
		t.Errorf("unexpected error message: %s", errResp["error"])
	}
}

func TestAuth_POSTWithWrongToken_Returns401(t *testing.T) {
	handler := BearerAuth("secret-token", nil)(testHandler)

	req := httptest.NewRequest(http.MethodPost, "/api/apps/docufiller/channels/stable/releases", nil)
	req.Header.Set("Authorization", "Bearer wrong-token")
	rr := httptest.NewRecorder()
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusUnauthorized {
		t.Fatalf("POST with wrong token should return 401, got %d", rr.Code)
	}

	var errResp map[string]string
	json.NewDecoder(rr.Body).Decode(&errResp)
	if errResp["error"] != "invalid token" {
		t.Errorf("unexpected error message: %s", errResp["error"])
	}
}

func TestAuth_POSTWithCorrectToken_PassesThrough(t *testing.T) {
	handler := BearerAuth("secret-token", nil)(testHandler)

	req := httptest.NewRequest(http.MethodPost, "/api/apps/docufiller/channels/stable/releases", nil)
	req.Header.Set("Authorization", "Bearer secret-token")
	rr := httptest.NewRecorder()
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusOK {
		t.Errorf("POST with correct token should pass through, got %d", rr.Code)
	}
	if rr.Body.String() != "ok" {
		t.Errorf("unexpected body: %s", rr.Body.String())
	}
}

func TestAuth_EmptyToken_DisablesAuth(t *testing.T) {
	handler := BearerAuth("", nil)(testHandler)

	// POST to API path without token — should pass through when auth is disabled
	req := httptest.NewRequest(http.MethodPost, "/api/apps/docufiller/channels/stable/releases", nil)
	rr := httptest.NewRecorder()
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusOK {
		t.Errorf("empty token config should disable auth, got %d", rr.Code)
	}
}

func TestAuth_DELETE_RequiresAuth(t *testing.T) {
	handler := BearerAuth("secret-token", nil)(testHandler)

	req := httptest.NewRequest(http.MethodDelete, "/api/apps/docufiller/channels/stable/versions/1.0.0", nil)
	rr := httptest.NewRecorder()
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusUnauthorized {
		t.Errorf("DELETE should require auth, got %d", rr.Code)
	}
}

func TestAuth_DELETE_WithCorrectToken_PassesThrough(t *testing.T) {
	handler := BearerAuth("secret-token", nil)(testHandler)

	req := httptest.NewRequest(http.MethodDelete, "/api/apps/docufiller/channels/stable/versions/1.0.0", nil)
	req.Header.Set("Authorization", "Bearer secret-token")
	rr := httptest.NewRecorder()
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusOK {
		t.Errorf("DELETE with correct token should pass through, got %d", rr.Code)
	}
}

func TestAuth_InvalidAuthScheme_Returns401(t *testing.T) {
	handler := BearerAuth("secret-token", nil)(testHandler)

	req := httptest.NewRequest(http.MethodPost, "/api/apps/docufiller/channels/stable/releases", nil)
	req.Header.Set("Authorization", "Basic dXNlcjpwYXNz")
	rr := httptest.NewRecorder()
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusUnauthorized {
		t.Fatalf("invalid auth scheme should return 401, got %d", rr.Code)
	}

	var errResp map[string]string
	json.NewDecoder(rr.Body).Decode(&errResp)
	if errResp["error"] != "invalid Authorization header format" {
		t.Errorf("unexpected error message: %s", errResp["error"])
	}
}

func TestAuth_CaseInsensitiveBearer(t *testing.T) {
	handler := BearerAuth("secret-token", nil)(testHandler)

	req := httptest.NewRequest(http.MethodPost, "/api/apps/docufiller/channels/stable/releases", nil)
	req.Header.Set("Authorization", "bearer secret-token")
	rr := httptest.NewRecorder()
	handler.ServeHTTP(rr, req)

	if rr.Code != http.StatusOK {
		t.Errorf("case-insensitive 'bearer' should pass through, got %d", rr.Code)
	}
}
