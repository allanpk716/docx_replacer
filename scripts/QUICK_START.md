# Quick Start Guide - Build and Publish Scripts

## Files Created

```
scripts/
├── config/
│   └── publish-config.bat          # Server URL and token configuration
├── build.bat                        # Compile and package WPF app
├── publish.bat                      # Upload to update server
├── build-and-publish.bat            # One-click build + publish
├── README.md                        # Comprehensive documentation
└── TASK8_COMPLETION_REPORT.md       # Implementation details
```

## Quick Usage

### 1. Configure Server
Edit `scripts/config/publish-config.bat`:
```batch
set UPDATE_SERVER_URL=http://localhost:8080
set UPDATE_SERVER_TOKEN=your-secure-token
set DEFAULT_CHANNEL=stable
```

### 2. Build Application
```batch
cd scripts
build.bat
```
Output: `build/docufiller-1.0.0.zip` (174MB)

### 3. Publish to Server
```batch
publish.bat stable 1.0.0
```

### 4. One-Click Build and Publish
```batch
build-and-publish.bat stable
```

## Test Results

✅ **Build Script**: Tested and verified
- Version parsing: Working (reads 1.0.0 from DocuFiller.csproj)
- Compilation: Success (dotnet publish)
- Packaging: Success (174MB zip)
- Cleanup: Success (temp files removed)

⚠️ **Publish Script**: Created, requires Task 7 fix
- Script syntax: Valid
- API call structure: Correct
- Full test: Pending Go server bug fix

## Technical Details

- **Version Parsing**: Extracts from `<Version>` tag using batch commands
- **Platform**: Windows win-x64 self-contained
- **Upload Method**: HTTP multipart/form-data via curl
- **Channels**: stable, beta
- **File Format**: Zip archive (tar with gzip)

## Git Commit

```
commit 1eb9b07
feat(scripts): add build and publish scripts for auto-update system (Task 8)
```

## Next Steps

1. Fix Task 7 logger compilation error in Go server
2. Test publish.bat end-to-end
3. Proceed to Task 9 (WPF update service interfaces)

---

**Status**: ✅ Task 8 Complete
**Date**: 2025-01-15
