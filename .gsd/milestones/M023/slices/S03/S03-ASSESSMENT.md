---
sliceId: S03
uatType: artifact-driven
verdict: PASS
date: 2026-05-05T15:30:35Z
---

# UAT Result — S03

## Checks

| Check | Mode | Result | Notes |
|-------|------|--------|-------|
| Smoke Test: Go binary builds | artifact | PASS | `go build -o update-hub.exe .` exit 0, binary 14.3 MB with embedded SPA |
| TC1: SPA serves index.html at root | artifact | PASS | TestSPAHandler_ServesIndexHTML passed (0.19s) — GET / returns 200 with index.html content |
| TC2: SPA serves Vite assets with correct MIME types | artifact | PASS | TestSPAHandler_ServesAssets passed — /assets/app.js → application/javascript, /assets/app.css → text/css |
| TC3: SPA fallback for client-side routes | artifact | PASS | TestSPAHandler_FallbackToIndex passed — /login, /apps, /apps/myapp, /settings/profile all return index.html 200 |
| TC4: Path traversal protection | artifact | PASS | TestSPAHandler_PathTraversal passed — 3 sub-tests (double_dot, embedded_double_dot, mixed_traversal) all return 400 |
| TC5: JWT login endpoint issues session cookie | artifact | PASS | TestLogin_CorrectPassword_ReturnsTokenCookie passed — 200 + Set-Cookie HttpOnly JWT. Edge cases verified: wrong_password→401, no_password→401 "login disabled", invalid_body→400, GET→405 |
| TC6: Auth middleware accepts JWT cookie | artifact | PASS | 7 auth tests passed: valid cookie passes through, expired/invalid signature →401, no auth →401, Bearer precedence over cookie, empty Bearer falls back to cookie, login endpoint skips auth |
| TC7: Vue 3 frontend compiles | artifact | PASS | vue-tsc -b exit 0 (zero TS errors). vite build outputs dist/index.html + 4 CSS + 4 JS assets (54 modules transformed, built in 1.04s) |
| TC8: Full Go test suite passes | artifact | PASS | All 5 packages pass: database (0.32s), handler (1.57s), middleware (1.10s), storage (0.25s). Exit code 0 |

## Edge Cases Verified (via test output)

- Login with no password configured returns 401 with "login_failed" reason "no_password" (not 404)
- Expired JWT returns 401 with "token is expired" message
- Invalid JWT signature returns 401 with "signature is invalid" message
- Bearer token and JWT cookie both present — Bearer takes precedence (TestAuth_BearerTokenPreferredOverCookie)
- Empty Bearer header falls back to cookie (TestAuth_EmptyBearer_FallsBackToCookie)

## Not Proven (as documented in UAT)

- Visual rendering of Vue 3 frontend in real browser — deferred to S04 deployment
- End-to-end login → manage apps → upload workflow on running server — deferred to S04
- Responsive layout and CSS styling — deferred to S04
- S04 deployment on Windows Server 2019

## Overall Verdict

PASS — All 8 UAT test cases passed via artifact-driven verification (Go tests, TypeScript check, Vite build, binary compilation). All edge cases verified through test output inspection.

## Notes

- GOCACHE=/tmp/go-cache required for Go commands in this agent environment
- npm install requires --ignore-scripts workaround on this build machine (esbuild postinstall fails)
- Binary size: 14.3 MB (includes embedded Vue SPA at ~125 KB gzip)
