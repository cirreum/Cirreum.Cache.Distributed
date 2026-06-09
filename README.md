# Cirreum.Cache.Distributed

[![NuGet Version](https://img.shields.io/nuget/v/Cirreum.Cache.Distributed.svg?style=flat-square&labelColor=1F1F1F&color=003D8F)](https://www.nuget.org/packages/Cirreum.Cache.Distributed/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Cirreum.Cache.Distributed.svg?style=flat-square&labelColor=1F1F1F&color=003D8F)](https://www.nuget.org/packages/Cirreum.Cache.Distributed/)
[![GitHub Release](https://img.shields.io/github/v/release/cirreum/Cirreum.Cache.Distributed?style=flat-square&labelColor=1F1F1F&color=FF3B2E)](https://github.com/cirreum/Cirreum.Cache.Distributed/releases)
[![License](https://img.shields.io/badge/license-MIT-F2F2F2?style=flat-square&labelColor=1F1F1F)](https://github.com/cirreum/Cirreum.Cache.Distributed/blob/main/LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-003D8F?style=flat-square&labelColor=1F1F1F)](https://dotnet.microsoft.com/)

**Distributed caching adapter for Cirreum's cache service**

> Supersedes the legacy `Cirreum.QueryCache.Distributed` package. The lineage moved to a new
> package id alongside the Cirreum 1.0 foundation reset and the code-first caching model.

## Overview

**Cirreum.Cache.Distributed** provides a distributed caching implementation for the Cirreum Conductor framework by adapting ASP.NET Core's `IDistributedCache` interface to Cirreum's `ICacheService`. This enables caching of query results across multiple application instances using any distributed cache provider (Redis, SQL Server, NCache, etc.).

## Installation

```bash
dotnet add package Cirreum.Cache.Distributed
```

## Usage

```csharp
// In your Startup.cs or Program.cs
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379";
});

// Add Cirreum distributed query caching
services.AddDistributedCacheService();
```

The service automatically registers as a singleton implementation of `ICacheService` and uses the configured `IDistributedCache` for storage.

### Configuration Options

Serialization uses `System.Text.Json`. To customize it, register a `JsonSerializerOptions`
in DI before adding the cache service — the adapter picks it up automatically, and falls back
to defaults when none is registered:

```csharp
services.AddSingleton(new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
});

services.AddDistributedCacheService();
```

## Features

- **Distributed Cache Integration**: Works with any `IDistributedCache` implementation
- **JSON Serialization**: Configurable JSON serialization for cached objects
- **Failure Expiration**: Supports different cache durations for failed results
- **Async Operations**: Fully async with `CancellationToken` support
- **Conductor Integration**: Seamlessly integrates with Cirreum Conductor's caching pipeline

## Limitations

- **No Tag-Based Eviction**: The `RemoveByTagAsync` and `RemoveByTagsAsync` methods are not supported due to `IDistributedCache` limitations. These methods will throw `NotSupportedException`.
- **Requires External Cache**: This package doesn't provide a distributed cache implementation. You must configure one separately (e.g., Redis, SQL Server, NCache).

## Requirements

- .NET 10.0 or later
- Cirreum.Domain 1.x or later
- A configured `IDistributedCache` implementation

## Contribution Guidelines

1. **Be conservative with new abstractions**  
   The API surface must remain stable and meaningful.

2. **Limit dependency expansion**  
   Only add foundational, version-stable dependencies.

3. **Favor additive, non-breaking changes**  
   Breaking changes ripple through the entire ecosystem.

4. **Include thorough unit tests**  
   All primitives and patterns should be independently testable.

5. **Document architectural decisions**  
   Context and reasoning should be clear for future maintainers.

6. **Follow .NET conventions**  
   Use established patterns from Microsoft.Extensions.* libraries.

## Versioning

Cirreum.Cache.Distributed follows [Semantic Versioning](https://semver.org/):

- **Major** - Breaking API changes
- **Minor** - New features, backward compatible
- **Patch** - Bug fixes, backward compatible

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

**Cirreum Foundation Framework**  
*Layered simplicity for modern .NET*