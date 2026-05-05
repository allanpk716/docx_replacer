---
estimated_steps: 12
estimated_files: 4
skills_used: []
---

# T04: Wire Go embed + SPA handler and verify full integration

Wire the Vue 3 frontend into the Go binary via `embed` and create the SPA serving handler. Routing relies on Go 1.22 ServeMux pattern specificity: literal `/assets/` overrides wildcard `/{appId}/`, which overrides catch-all `/`.

## Routing Architecture
- `/api/*` → API handlers
- `/assets/` (literal) → embedded Vite assets
- `/{appId}/` (wildcard) → StaticHandler (Velopack, existing)
- `/` (catch-all) → SPA handler (index.html)

## Steps

1. Create `embed.go`: //go:embed web/dist
2. Create `handler/spa.go`: SPAHandler with fs.FS, serve file or fallback to index.html, correct Content-Type
3. Update `main.go`: register /assets/ handler, SPA catch-all, login route, pass JWT secret
4. Create `handler/spa_test.go`: test SPA fallback, asset serving, path traversal protection
5. Full build: npm run build → go build → single binary

## Inputs

- `update-hub/web/dist/index.html`
- `update-hub/main.go`

## Expected Output

- `update-hub/embed.go`
- `update-hub/handler/spa.go`
- `update-hub/handler/spa_test.go`
- `update-hub/main.go`

## Verification

cd update-hub && go test ./handler/... -run SPA -v -count=1 && go vet ./... && go build -o update-hub.exe . && ls update-hub.exe && rm -f update-hub.exe

## Observability Impact

Signals added: spa_setup at startup confirming embed loaded. Future agent inspects via startup log. Failure state: go build fails with clear error if web/dist missing.
