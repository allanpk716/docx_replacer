---
estimated_steps: 28
estimated_files: 6
skills_used: []
---

# T01: Add JWT session auth — login endpoint and middleware update

Add JWT session-based authentication for the Web UI. Create a POST /api/auth/login endpoint that validates a password and issues a JWT session cookie. Update the existing BearerAuth middleware to also accept JWT session cookies on write operations. The login endpoint itself must be exempt from auth.

## Steps

1. Create `middleware/jwt.go` with JWT utility functions:
   - `GenerateToken(secret []byte, expiry time.Duration) (string, error)` — generates HMAC-SHA256 JWT with `exp` and `iat` claims
   - `ValidateToken(tokenString string, secret []byte) (*jwt.MapClaims, error)` — validates signature and expiry
   - JWT secret: generate random UUID at server startup (via `crypto/rand` or `uuid`), stored in a struct
   - Use `golang-jwt/jwt/v5` library for JWT operations

2. Create `handler/auth_login.go` with LoginHandler:
   - POST /api/auth/login accepts JSON body `{"password": "..."}`
   - Compare password against configured admin password using `crypto/subtle.ConstantTimeCompare`
   - On success: generate JWT, set HttpOnly cookie named `session` with the JWT value
   - Cookie attributes: HttpOnly=true, SameSite=Lax, Path=/, Max-Age=86400 (24h), Secure=false (internal HTTP)
   - Return 200 JSON `{"ok": true}` on success, 401 `{"error": "invalid password"}` on failure
   - If no password is configured (empty string), login endpoint returns 401 with `{"error": "login disabled"}` immediately
   - Log login_success and login_failed events

3. Update `middleware/auth.go` to accept JWT session cookies:
   - Add `JWTSecret []byte` parameter to the BearerAuth middleware factory
   - After Bearer header check fails, look for `session` cookie
   - If cookie exists, validate JWT; if valid, allow the request
   - Add special case: skip ALL auth for `POST /api/auth/login` path
   - Keep existing behavior: empty token config disables Bearer auth; GET requests to /api/ are public

4. Update `main.go`:
   - Add `-password` flag (admin password for Web UI login)
   - Generate JWT secret at startup: `uuid.New().String()` or 32 random bytes
   - Create LoginHandler, wire to `POST /api/auth/login`
   - Pass JWT secret to BearerAuth middleware
   - Log jwt_secret as "(generated)" in startup log

5. Add `golang-jwt/jwt/v5` and `github.com/google/uuid` to go.mod

## Inputs

- `update-hub/middleware/auth.go`
- `update-hub/main.go`
- `update-hub/go.mod`

## Expected Output

- `update-hub/handler/auth_login.go`
- `update-hub/middleware/jwt.go`
- `update-hub/middleware/auth.go`
- `update-hub/go.mod`
- `update-hub/handler/auth_login_test.go`

## Verification

cd update-hub && go test ./handler/... ./middleware/... -run "Login|JWT|Cookie" -v -count=1 && go vet ./... && go build ./...

## Observability Impact

Signals added: login_success, login_failed, jwt_invalid, jwt_expired structured JSON events. Future agent inspects via server log output. Failure state: login_failed includes reason (wrong_password vs no_password_configured).
