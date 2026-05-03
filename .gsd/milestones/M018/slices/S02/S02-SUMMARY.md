---
id: S02
parent: M018
milestone: M018
provides:
  - ["scripts/e2e-portable-update-test.bat — local HTTP portable update E2E", "scripts/e2e-portable-go-update-test.sh — Go server portable update E2E", "Updated docs/plans/e2e-update-test-guide.md with portable testing documentation"]
requires:
  - slice: S01
    provides: CLI update --yes unblocked for portable, IUpdateService.IsPortable property, normal update status flow for portable version
affects:
  []
key_files:
  - ["scripts/e2e-portable-update-test.bat", "scripts/e2e-portable-go-update-test.sh", "docs/plans/e2e-update-test-guide.md"]
key_decisions:
  - (none)
patterns_established:
  - ["Portable E2E scripts use background process + timeout to handle ApplyUpdatesAndRestart process exit", "update-config.json at %USERPROFILE%/.docx_replacer/ is the portable app's update source config", "Multi-strategy version verification: JSONL log parsing + exe file version check", "Distinct ports per E2E script: 8080/8081/19080/19081 avoid conflicts"]
observability_surfaces:
  - none
drill_down_paths:
  - [".gsd/milestones/M018/slices/S02/tasks/T01-SUMMARY.md", ".gsd/milestones/M018/slices/S02/tasks/T02-SUMMARY.md", ".gsd/milestones/M018/slices/S02/tasks/T03-SUMMARY.md"]
duration: ""
verification_result: passed
completed_at: 2026-05-03T09:32:38.126Z
blocker_discovered: false
---

# S02: E2E 便携版更新测试

**Created two automated E2E test scripts (BAT for local HTTP, SH for Go server) and updated the test guide, enabling one-click portable update verification across both deployment environments.**

## What Happened

This slice delivered the E2E automated testing infrastructure for portable DocuFiller self-updates. Three tasks were completed:

**T01** created `scripts/e2e-portable-update-test.bat` — a Windows CMD script that automates the full portable update chain against the local Python HTTP server (e2e-serve.py). It builds v1.0.0 and v1.1.0 from source, packs with vpk, extracts the portable zip, configures the update URL, runs `update --yes` in a background process (handling ApplyUpdatesAndRestart process exit), and verifies the version upgrade through multi-strategy output parsing. Uses port 8081 to avoid conflicts with existing E2E scripts on port 8080.

**T02** created `scripts/e2e-portable-go-update-test.sh` — a git-bash script that automates the same flow against the Go update server. It compiles the Go server, starts it on port 19081, uploads v1.1.0 artifacts via the multipart API, verifies the stable channel feed, then runs the portable update and checks for success. Uses trap EXIT for reliable cleanup.

**T03** updated `docs/plans/e2e-update-test-guide.md` with four new sections covering both portable scripts, prerequisites (portable extraction, update-config.json setup), and 6 new troubleshooting entries for portable-specific issues.

All scripts have PASS/FAIL output, no Chinese characters (per BAT convention), handle cleanup on both success and failure paths, and support dry-run/syntax-check modes. No production code was modified — dotnet build passes with 0 errors.

## Verification

Slice verification passed across all checks:
1. `scripts/e2e-portable-update-test.bat` exists (17KB), supports `--dry-run` (exit 0), contains 40 PASS/FAIL/OVERALL markers
2. `scripts/e2e-portable-go-update-test.sh` exists (13KB), `bash -n` syntax check passes, contains 9 PASS/FAIL markers
3. `docs/plans/e2e-update-test-guide.md` contains 20 references to "Portable" across new sections
4. `dotnet build --no-restore -v q` — 0 errors, 95 warnings (all pre-existing)
5. All task verification evidence collected and passed (see individual task summaries)

## Requirements Advanced

- R026 — E2E test scripts now cover portable update scenarios (local HTTP + Go server) in addition to existing installer tests

## Requirements Validated

None.

## New Requirements Surfaced

None.

## Requirements Invalidated or Re-scoped

None.

## Operational Readiness

None.

## Deviations

None.

## Known Limitations

None.

## Follow-ups

None.

## Files Created/Modified

None.
