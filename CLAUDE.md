# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is the **Cirreum.Cache.Distributed** library - a NuGet package that provides distributed caching capabilities for the Cirreum Conductor framework using `IDistributedCache`.

**Key Purpose**: Bridges the gap between ASP.NET Core's `IDistributedCache` interface and Cirreum's `ICacheService`, enabling distributed caching for CQRS query results.

## Build and Development Commands

```bash
# Build the project
dotnet build Cirreum.Cache.Distributed.slnx

# Build in Release mode
dotnet build Cirreum.Cache.Distributed.slnx --configuration Release

# Restore dependencies
dotnet restore Cirreum.Cache.Distributed.slnx

# Create NuGet package
dotnet pack Cirreum.Cache.Distributed.slnx --configuration Release

# Clean build artifacts
dotnet clean Cirreum.Cache.Distributed.slnx
```

## Architecture and Key Components

### Core Implementation
- **DistributedCacheService**: Main service class in `src/Cirreum.Cache.Distributed/DistributedCacheService.cs`
  - Implements `ICacheService` from Cirreum.Caching
  - Uses `System.Text.Json` for serialization
  - Primary constructor pattern with sealed class
  - **Important**: Tag-based cache eviction is NOT supported (throws `NotSupportedException`)

### Dependency Injection
- Extension method `AddDistributedCacheService()` in `Extensions/ServiceCollectionExtensions.cs`
- **Code-first provider selection**: the registration call itself selects the provider — it
  delegates to the foundation's `AddCacheService(factory)` helper (from `Cirreum.Domain`).
  There is no `CacheProvider` enum or `Cirreum:Cache:Provider` appsettings switch.
- Requires `IDistributedCache` to be registered separately — **fails fast** at registration
  time (throws `InvalidOperationException`) if absent

### Build Configuration
- Target framework: .NET 10.0
- Nullable reference types enabled
- Implicit usings enabled
- Documentation XML generation enabled
- Version handled via CI/CD (GitHub Actions)

## Important Limitations

1. **No Tag-Based Eviction**: Methods `RemoveByTagAsync` and `RemoveByTagsAsync` throw `NotSupportedException`
2. **No Test Project**: Tests should be added when implementing new features
3. **External Cache Required**: This library doesn't provide `IDistributedCache` implementation - must be configured separately (e.g., Redis, SQL Server)

## CI/CD Pipeline

GitHub Actions workflow in `.github/workflows/publish.yml`:
- Triggers on release tags (`v*`) or manual dispatch
- Builds with .NET 10.0
- Publishes to NuGet.org using OIDC authentication
- Version extracted from git tag or generates dev version

## Development Guidelines

1. **Code-First**: Provider selection is the `AddDistributedCacheService()` call, not config
2. **Follow Patterns**: Use existing `Cirreum.Domain` / `Cirreum.Contracts` patterns and conventions
3. **Document Changes**: Update XML documentation for public APIs
4. **Error Handling**: Maintain graceful handling of cache misses
5. **Async-First**: All operations should support `CancellationToken`

## Common Tasks

### Adding New Cache Features
1. Check if `IDistributedCache` supports the feature
2. Implement in `DistributedCacheService`
3. Ensure proper JSON serialization handling
4. Add XML documentation
5. Consider backward compatibility

### Updating Dependencies
- Edit `Cirreum.Cache.Distributed.csproj`
- Run `dotnet restore` to verify resolution
- Test against the latest `Cirreum.Domain` version

### Publishing Updates
1. Create a new release on GitHub with tag `v{VERSION}`
2. GitHub Actions will automatically build and publish to NuGet
3. Manual dispatch available for dev builds