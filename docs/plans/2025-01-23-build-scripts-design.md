# Build Scripts Design - Unified Build System

> **Date:** 2025-01-23
> **Author:** Claude Code
> **Status:** Design Approved

## Problem Statement

### Current Issues

1. **Missing update-client.exe in runtime**: The build validates External files during compilation, but when packaging for distribution, the `update-client.exe` may not be properly included, breaking the self-update feature at runtime.

2. **Script confusion**: Multiple build scripts (`build.bat`, `publish.bat`, `build-and-publish.bat`) with overlapping logic cause confusion about which script to use.

3. **No automation**: Version numbers and release notes need to be manually specified, even though Git tags and commit messages contain this information.

### Requirements

1. **Single entry point**: Users should only need to run one script
2. **Auto-detection**: The script should automatically detect whether to build for local testing or publish to server
3. **Smart versioning**: Automatically derive version from Git tags or csproj file
4. **Auto release notes**: Generate release notes from Git commit messages
5. **No Chinese characters**: All BAT scripts must use English only

## Solution Design

### Architecture

```
scripts/
├── build.bat                # Single entry point
├── build-internal.bat       # Internal logic (not called directly)
├── config/
│   └── publish-config.bat   # Publish configuration
└── build/                   # Build output directory
    └── docufiller-{version}.zip
```

### User Interface

```batch
# Auto-detect mode (recommended)
build.bat

# Force standalone build
build.bat --standalone

# Force publish to server
build.bat --publish

# Show help
build.bat --help
```

### Mode Detection Logic

```
Run build.bat
    │
    ├─ No parameter → Auto-detect
    │   ├─ Git Tag exists → Ask: "Publish to update server?"
    │   │   ├─ Yes → publish mode
    │   │   └─ No → standalone mode
    │   └─ No Git Tag → standalone mode (local testing)
    │
    ├─ --standalone → Standalone mode
    ├─ --publish → Publish mode
    └─ --help → Show usage
```

## Component Specifications

### 1. build.bat (Entry Point)

**Purpose:** Parse arguments and delegate to build-internal.bat

**Key Features:**
- Argument parsing: `--standalone`, `--publish`, `--help`
- Auto-detect mode when no arguments provided
- Detect Git Tag and prompt user for confirmation
- Display help information

**Auto-Detection Algorithm:**
1. Run `git describe --tags --exact-match`
2. If tag exists:
   - Display tag name
   - Ask user: "Publish to update server? (Y/N)"
3. If no tag:
   - Automatically use standalone mode

### 2. build-internal.bat (Core Logic)

**Purpose:** Handle all build and publish operations

**Responsibilities:**
1. Get version (from Git tag or csproj)
2. Detect channel (stable/beta/alpha/dev)
3. Clean old build output
4. Compile project (`dotnet publish`)
5. Copy External files to output
6. Create ZIP package
7. (Optional) Publish to server

**Version Detection:**
```
If Git Tag exists (e.g., v1.0.0, v1.0.0-beta):
    VERSION = Git Tag
    CHANNEL = beta/alpha if tag contains keyword, else stable
Else:
    VERSION = csproj version + "-dev" suffix
    CHANNEL = dev
```

**External Files to Copy:**
- `External/update-client.exe` → `build/temp/update-client.exe`
- `External/update-client.config.yaml` → `build/temp/update-client.config.yaml`

**Package Output:**
- `build/docufiller-{version}.zip`

### 3. publish-client.exe Integration

**Location:** `External/publish-client.exe`

**Command:**
```batch
publish-client.exe upload
  --server {UPDATE_SERVER_URL}
  --token {UPDATE_SERVER_TOKEN}
  --program-id {PROGRAM_ID}
  --channel {CHANNEL}
  --version {VERSION}
  --file {PACKAGE_PATH}
  --notes {RELEASE_NOTES}
  [--mandatory]
```

**Release Notes Generation:**
```
If Git Tag exists:
    Get commits since tag: git log {tag}..HEAD --oneline
Else:
    Get recent 5 commits: git log -5 --oneline

If no commits:
    Use default: "Release version {VERSION}"
```

**Interactive Prompts:**
1. "Mark as mandatory update? (Y/N)" → adds `--mandatory` flag if yes

### 4. Configuration File

**File:** `scripts/config/publish-config.bat`

**Content:**
```batch
REM Update Server Configuration
set UPDATE_SERVER_URL=http://172.18.200.47:58100
set UPDATE_SERVER_TOKEN=your-token-here
set PROGRAM_ID=docufiller
```

**Security:** This file should NOT be committed to git (add to .gitignore)

## Data Flow

### Standalone Build Flow

```
build.bat (no args)
    → No Git Tag detected
    → MODE = standalone
    → build-internal.bat standalone
    → Get version from csproj (add -dev)
    → Clean build/
    → dotnet publish
    → Copy External files
    → Create ZIP
    → Output: build/docufiller-{version}-dev.zip
```

### Publish Flow

```
build.bat (no args)
    → Git Tag detected (e.g., v1.0.0)
    → Ask user: "Publish to update server? (Y/N)"
    → User selects Yes
    → MODE = publish
    → build-internal.bat publish
    → VERSION = v1.0.0, CHANNEL = stable
    → Build and package (same as standalone)
    → Load publish-config.bat
    → Get release notes from commits
    → Ask: "Mark as mandatory update? (Y/N)"
    → publish-client.exe upload
    → Display result
```

## Implementation Tasks

### Task 1: Create build-internal.bat
- Implement version detection logic
- Implement build and package functions
- Implement publish function
- Implement release notes generation

### Task 2: Create build.bat
- Implement argument parsing
- Implement auto-detection logic
- Implement help display

### Task 3: Update publish-config.bat
- Set correct server URL
- Set correct program ID
- Add to .gitignore

### Task 4: Remove obsolete scripts
- Delete old `build.bat` (rename to build.bat.old first)
- Delete `publish.bat`
- Delete `build-and-publish.bat`

### Task 5: Update .gitignore
- Ignore `scripts/config/publish-config.bat` (contains token)

## Testing Checklist

- [ ] `build.bat --help` shows usage
- [ ] `build.bat --standalone` creates package with -dev suffix
- [ ] `build.bat` with no tag creates standalone package
- [ ] `build.bat` with tag prompts for publish
- [ ] `build.bat --publish` with tag publishes successfully
- [ ] Package contains `update-client.exe` and `update-client.config.yaml`
- [ ] Release notes are generated from commits
- [ ] Mandatory flag prompt works correctly

## Migration Notes

### Before
- `build.bat` - Standalone build only
- `publish.bat` - Publish via curl API
- `build-and-publish.bat` - Combined operation

### After
- `build.bat` - Single entry point with auto-detection

### Breaking Changes
- Old `build.bat` will be replaced (backup as build.bat.old)
- `publish.bat` and `build-and-publish.bat` will be removed

## Security Considerations

1. **Token storage**: `publish-config.bat` contains API token, must be in .gitignore
2. **Token validation**: Script should validate token format before upload
3. **Server URL**: Should use HTTPS in production (configurable via publish-config.bat)

## Future Enhancements

1. **Delta updates**: Only upload changed files (not implemented yet)
2. **Automatic changelog**: Generate CHANGELOG.md from commits
3. **Version bumping**: Interactive tool to bump version and create tag
4. **CI/CD integration**: Support for GitHub Actions / Azure DevOps

## References

- Update Client Usage: `External/publish-client.usage.txt`
- Update Server Documentation: [update-server/docs]
- Project CLAUDE.md: `CLAUDE.md`
