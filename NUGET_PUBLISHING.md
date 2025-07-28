# NuGet Publishing Setup

This project is configured for automatic NuGet package publishing using GitHub Actions.

## How It Works

The GitHub Actions workflow (`.github/workflows/nuget-publish.yml`) automatically:

1. **On Pull Requests**: Builds and tests the code to ensure quality
2. **On Push to Main**: Builds, tests, and publishes a new NuGet package

## Setup Requirements

### 1. NuGet API Key

To enable publishing, you need to add your NuGet API key as a GitHub secret:

1. Go to [NuGet.org](https://nuget.org) and sign in
2. Go to your account settings → API Keys
3. Create a new API key with "Push new packages and package versions" permission
4. In your GitHub repository, go to Settings → Secrets and variables → Actions
5. Add a new repository secret named `NUGET_API_KEY` with your API key as the value

### 2. Package Metadata

The package metadata is configured in `Serilog.Enrichers.CallStack.csproj`:

- **PackageId**: `Serilog.Enrichers.CallStack`
- **Authors**: Michael Akinyemi
- **Description**: A Serilog enricher that adds call stack information to log events
- **Tags**: serilog, enricher, callstack, logging, diagnostics, debugging, tracing
- **License**: Included from LICENSE file
- **README**: Included from README.md file

## Versioning Strategy

The workflow uses commit-based versioning:
- Format: `1.0.{COMMIT_COUNT}`
- Example: `1.0.42` (where 42 is the total number of commits)

This ensures every push to main creates a unique version number.

## Workflow Features

### Build and Test Job
- Restores dependencies
- Builds in Release configuration
- Runs all tests with code coverage
- Uploads test results as artifacts

### Publish Job (Main branch only)
- Builds and packages the library
- Generates version number from commit count
- Creates both main package (.nupkg) and symbols package (.snupkg)
- Publishes to NuGet.org with skip-duplicate flag
- Uploads packages as GitHub artifacts

## Manual Package Creation

To create a package locally for testing:

```bash
# Build in release mode
dotnet build --configuration Release

# Create package
dotnet pack --configuration Release --output ./packages

# The generated files will be:
# - Serilog.Enrichers.CallStack.{version}.nupkg (main package)
# - Serilog.Enrichers.CallStack.{version}.snupkg (symbols package)
```

## Testing the Workflow

1. Make changes to the code
2. Create a pull request - this will trigger build and test
3. Merge to main - this will trigger build, test, and publish
4. Check the Actions tab in GitHub to monitor progress
5. Verify package appears on [NuGet.org](https://www.nuget.org/packages/Serilog.Enrichers.CallStack/)

## Important Notes

- The workflow only publishes from the `main` branch
- Symbol packages (.snupkg) are included for debugging support
- Source Link is configured for source code navigation
- The `--skip-duplicate` flag prevents errors if the same version is published twice
- All builds include comprehensive metadata for a professional NuGet package