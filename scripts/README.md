# DocuFiller Build and Publish Scripts

This directory contains automated build and publish scripts for DocuFiller releases.

## Scripts Overview

### Configuration File
- **config/publish-config.bat** - Configuration for update server URL and authentication token

### Build Scripts
- **build.bat** - Compiles WPF application and creates zip package
- **publish.bat** - Uploads package to update server
- **build-and-publish.bat** - One-click build and publish

## Prerequisites

1. .NET 8 SDK installed
2. Go update server running (see docufiller-update-server)
3. curl command available (included in Windows 10+)

## Configuration

Before using the scripts, configure the update server settings in `config/publish-config.bat`:

```batch
set UPDATE_SERVER_URL=http://localhost:8080
set UPDATE_SERVER_TOKEN=your-secure-token-here
set DEFAULT_CHANNEL=stable
```

**Important**: Change the default token to a secure value in production!

## Usage

### Method 1: One-Click Build and Publish (Recommended)

```batch
# Publish to stable channel (default)
build-and-publish.bat

# Publish to beta channel
build-and-publish.bat beta
```

### Method 2: Step-by-Step

```batch
# Step 1: Build the application
build.bat

# Step 2: Publish to server
# Syntax: publish.bat [channel] [version]
publish.bat stable 1.0.0
```

## Channels

- **stable** - Production releases (default)
- **beta** - Testing/pre-release versions

## Version Management

The build script automatically reads the version from `DocuFiller.csproj`:

```xml
<Version>1.0.0</Version>
```

To release a new version:
1. Update the version in `DocuFiller.csproj`
2. Run `build-and-publish.bat`

## Build Output

Build artifacts are stored in the `build/` directory:
- `build/docufiller-{version}.zip` - Release package
- `build/temp/` - Temporary build files (cleaned up automatically)

## Examples

### Release a new stable version
```batch
# Update version in DocuFiller.csproj to 1.2.0
# Then run:
build-and-publish.bat stable
```

### Publish a beta test version
```batch
build-and-publish.bat beta
```

### Build without publishing
```batch
build.bat
```

### Publish existing build
```batch
# If you already have a build, publish it directly
publish.bat stable 1.0.0
```

## Troubleshooting

### Build fails
- Ensure .NET 8 SDK is installed: `dotnet --version`
- Check that DocuFiller.csproj has a valid `<Version>` tag
- Verify the project builds: `dotnet build DocuFiller.csproj`

### Publish fails
- Verify the Go update server is running at the configured URL
- Check the authentication token matches the server configuration
- Test server health: `curl http://localhost:8080/api/health`
- Verify the build file exists in `build/` directory

### Version not found
- Ensure DocuFiller.csproj contains `<Version>x.x.x</Version>`
- Or specify version explicitly: `publish.bat stable 1.0.0`

## Server API

The publish script calls the Go update server API:

**Endpoint**: `POST /api/version/upload`

**Parameters**:
- `channel` - Release channel (stable/beta)
- `version` - Version number
- `file` - Zip package file
- `mandatory` - Whether update is mandatory (default: false)
- `notes` - Release notes (optional)

## Security Notes

- Never commit production tokens to version control
- Use environment variables for sensitive data in CI/CD
- Keep the update server token secure
- Use HTTPS in production environments

## Integration with CI/CD

These scripts can be integrated into CI/CD pipelines:

```batch
# Example GitHub Actions / Azure DevOps
call scripts/configure-ci-env.bat
call scripts/build.bat
call scripts/publish.bat %CHANNEL% %VERSION%
```

## Related Documentation

- [Update Server Documentation](../docufiller-update-server/README.md)
- [Implementation Plan](../docs/plans/2025-01-15-auto-update-implementation.md)
