---
phase: close
phase_name: milestone-close
project: DocuFiller
generated: "2026-05-03T02:29:36Z"
counts:
  decisions: 3
  lessons: 2
  patterns: 2
  surprises: 0
missing_artifacts: []
---

# M016 Learnings

### Decisions

- **WindowChrome for custom title bar**: Chose WindowChrome with WindowStyle=None over full chromeless window to add pin button. Preserves system resize, Aero Snap, and window dragging while allowing custom interactive elements. Avoids the heavy cost of reimplementing system window behaviors.
  Source: M016-CONTEXT.md/Architectural Decisions

- **Emoji icons instead of resource files**: Used emoji (📌/📍) with opacity toggle for pin button states rather than icon resource files. Zero-dependency, no build complexity, sufficient visual distinction for an internal desktop tool.
  Source: M016/slices/S01/S01-SUMMARY.md/Key decisions

- **Code-behind PropertyChanged bridge for Window.Topmost**: Bound IsTopmost via ViewModel property + code-behind PropertyChanged subscription rather than attempting direct XAML binding. Window.Topmost is not a dependency property and cannot be XAML-bound.
  Source: M016/slices/S01/S01-SUMMARY.md/Key decisions

### Lessons

- **Window.Topmost is not a dependency property**: Cannot be directly bound in XAML. The standard workaround is a code-behind PropertyChanged handler that subscribes to the ViewModel property and sets Window.Topmost programmatically.
  Source: M016/slices/S01/S01-SUMMARY.md/Key decisions

- **WindowChrome preserves system window behaviors**: Unlike fully chromeless windows (WindowStyle=None + AllowsTransparency=True), WindowChrome retains resize handles, Aero Snap, and title-bar dragging. The only caveat is that custom title bar elements need IsHitTestVisibleInChrome=True to be interactive.
  Source: M016-CONTEXT.md/Architectural Decisions

### Patterns

- **WindowChrome custom title bar pattern**: WindowStyle=None + WindowChrome GlassFrameThickness=-1 with custom DockPanel containing app title and buttons. Interactive elements must set `WindowChrome.IsHitTestVisibleInChrome="True"`. Resize and Aero Snap are preserved without custom code.
  Source: M016/slices/S01/S01-SUMMARY.md/Patterns established

- **Code-behind bridging for non-bindable Window properties**: When a Window property is not a dependency property (e.g., Topmost, WindowState), expose a bindable property on the ViewModel and sync it via code-behind `PropertyChanged` subscription in the Window's Loaded/Initialized event.
  Source: M016/slices/S01/S01-SUMMARY.md/Patterns established

### Surprises

None — implementation proceeded as planned with no unexpected challenges.
