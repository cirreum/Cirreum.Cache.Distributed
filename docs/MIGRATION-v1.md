# Cirreum.Cache.Distributed v1.0.0 — Migration Guide

> **From:** `Cirreum.QueryCache.Distributed` (now archived) &nbsp;•&nbsp; **To:** `Cirreum.Cache.Distributed 1.0.0`

## Why v1

`Cirreum.Cache.Distributed` is the successor to the published `Cirreum.QueryCache.Distributed`
package. The package id changed alongside the Cirreum 1.0 foundation reset and the **code-first
caching model**: a cache provider is now selected by the registration call rather than a
`CacheProvider` enum / `Cirreum:Cache:Provider` appsettings switch. The `IDistributedCache`-backed
`ICacheService` implementation itself is unchanged in behavior.

---

## Breaking Changes — Find/Replace Table

| `Cirreum.QueryCache.Distributed` | `Cirreum.Cache.Distributed 1.0.0` | Notes |
|---|---|---|
| `<PackageReference Include="Cirreum.QueryCache.Distributed" .../>` | `<PackageReference Include="Cirreum.Cache.Distributed" Version="1.0.0" />` | New package id |
| `AddDistributedQueryCaching()` | `AddDistributedCacheService()` | Registration verb; now installs the provider via the foundation's `AddCacheService(factory)` seam |
| `DistributedCacheableQueryService` | `DistributedCacheService` | The `ICacheService` implementation |
| `namespace Cirreum.QueryCache.Distributed` | `namespace Cirreum.Cache.Distributed` | + `.Extensions` for the registration method |

### From the code-first caching model (`Cirreum.Contracts`/`Cirreum.Domain 1.1.1`)

| Before | After | Notes |
|---|---|---|
| `Cirreum:Cache:Provider` appsettings + `CacheProvider` enum | (removed) | The `Add…CacheService()` call *is* the provider choice |
| `CacheExpirationSettings` | `CacheExpirationPolicy` | Runtime per-operation expiration spec |
| `namespace Cirreum.Caching` (for `CacheSettings` / `CacheExpirationOverride`) | `namespace Cirreum.Caching.Configuration` | App-author configuration types |

---

## Migration Walkthrough

1. Swap the package reference to `Cirreum.Cache.Distributed 1.0.0`.
2. Replace `AddDistributedQueryCaching()` with `AddDistributedCacheService()` (after
   `AddCirreumCaching()` / `AddDomainServices()`; it registers the active `ICacheService` in any
   order). Ensure an `IDistributedCache` is registered (e.g. `AddStackExchangeRedisCache`) — the
   call fails fast at registration if none is.
3. Delete any `Cirreum:Cache:Provider` appsettings entry.
4. Apply the type/namespace renames from the tables above.

---

## What Didn't Change

- The `IDistributedCache`-backed caching behavior, the default 5-minute TTL for entries with no
  configured expiration, poison-payload evict-and-recompute, and correct `Result` / `Result<T>`
  round-tripping (via the `Cirreum.Result` STJ converter, ADR-0024).
- Tag-based eviction remains unsupported by `IDistributedCache` (`RemoveByTag(s)Async` throw
  `NotSupportedException`).

---

## Downstream Package Impact

Consumers of the old `Cirreum.QueryCache.Distributed` package should move to
`Cirreum.Cache.Distributed`. The old package is deprecated on NuGet with a successor pointer.
