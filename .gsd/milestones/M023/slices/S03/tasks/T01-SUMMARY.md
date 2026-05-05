---
id: T01
parent: S03
milestone: M023
key_files:
  - update-hub/middleware/jwt.go
  - update-hub/handler/auth_login.go
  - update-hub/middleware/auth.go
  - update-hub/handler/auth_login_test.go
  - update-hub/main.go
  - update-hub/go.mod
key_decisions:
  - Bearer token takes precedence over JWT cookie when both are present — preserves backward compatibility with CLI/API clients
  - Login endpoint returns 401 'login disabled' when no password is configured rather than 404 — clearer for frontend error handling
  - JWT secret generated as UUID string at startup (not persisted) — sessions survive server restart only if server isn't restarted
duration: 
verification_result: passed
completed_at: 2026-05-05T07:09:49.187Z
blocker_discovered: false
---

# T01: Add JWT session auth with POST /api/auth/login endpoint and middleware cookie support

**Add JWT session auth with POST /api/auth/login endpoint and middleware cookie support**

## What Happened

Implemented JWT session-based authentication for the Web UI. Created `middleware/jwt.go` with `GenerateToken` and `ValidateToken` functions using HMAC-SHA256 (golang-jwt/jwt/v5). Created `handler/auth_login.go` with `LoginHandler` that validates passwords via `crypto/subtle.ConstantTimeCompare`, generates JWT tokens, and sets HttpOnly session cookies (24h expiry, SameSite=Lax). Updated `middleware/auth.go` to accept both Bearer tokens and JWT session cookies — Bearer takes precedence, cookie is fallback. Added explicit exemption for `POST /api/auth/login` from auth. Updated `main.go` to add `-password` flag, generate JWT secret via `uuid.New()` at startup, wire login handler, and pass JWT secret to BearerAuth. Updated all existing tests (`auth_test.go`, `integration_test.go`) for the new two-parameter `BearerAuth` signature. Added comprehensive tests in `handler/auth_login_test.go` covering login success/failure cases, JWT cookie validation, expired tokens, invalid signatures, bearer-vs-cookie precedence, and login endpoint auth exemption. All 24 tests pass, vet and build clean.

## Verification

Ran `go test ./handler/... ./middleware/... -run "Login|JWT|Cookie|Auth" -v -count=1` — all 24 tests pass including 5 login handler tests, 8 JWT/cookie integration tests, and 11 existing auth tests. Ran `go vet ./...` and `go build ./...` — both clean. Full test suite `go test ./... -count=1` passes across all 5 packages.

## Verification Evidence

| # | Command | Exit Code | Verdict | Duration |
|---|---------|-----------|---------|----------|
| 1 | `go test ./handler/... ./middleware/... -run "Login|JWT|Cookie|Auth" -v -count=1` | 0 | ✅ pass | 1800ms |
| 2 | `go vet ./...` | 0 | ✅ pass | 5000ms |
| 3 | `go build ./...` | 0 | ✅ pass | 3000ms |
| 4 | `go test ./... -count=1` | 0 | ✅ pass | 5000ms |

## Deviations

Error message for missing auth changed from "missing Authorization header" to "authentication required" since JWT cookies are now an alternative auth method. Existing test assertion updated accordingly.

## Known Issues

None.

## Files Created/Modified

- `update-hub/middleware/jwt.go`
- `update-hub/handler/auth_login.go`
- `update-hub/middleware/auth.go`
- `update-hub/handler/auth_login_test.go`
- `update-hub/main.go`
- `update-hub/go.mod`
