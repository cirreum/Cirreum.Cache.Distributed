# Cirreum.Cache.Distributed 1.0.0 — the IDistributedCache provider

The `IDistributedCache`-backed implementation of Cirreum's `ICacheService`. Successor to the
published `Cirreum.QueryCache.Distributed` package, re-homed to a new id alongside the Cirreum
1.0 foundation reset and the code-first caching model.

Migrating from `Cirreum.QueryCache.Distributed`? See [`MIGRATION-v1.md`](MIGRATION-v1.md).

## Why this release exists

The caching foundation went code-first in `Cirreum.Contracts`/`Cirreum.Domain 1.1.1`: a provider
is chosen by the registration call, not an appsettings switch. This package is the distributed
provider under that model, and it moves to the `Cirreum.Cache.*` id family that replaces
`Cirreum.QueryCache.*`.

## What's new

### `AddDistributedCacheService()`

```csharp
services.AddCirreumCaching();
services.AddStackExchangeRedisCache(o => o.Configuration = "...");   // any IDistributedCache
services.AddDistributedCacheService();                              // selects this provider
```

Registers `DistributedCacheService` as the active `ICacheService` via the foundation's code-first
`AddCacheService(factory)` helper — the registration call itself selects the provider (no
`CacheProvider` enum, no `Cirreum:Cache:Provider` appsettings switch). **Fails fast at
registration** if no `IDistributedCache` has been registered.

## Behavior

- Entries with no configured expiration are bounded by a default **5-minute TTL** rather than
  persisting indefinitely (`IDistributedCache` has no built-in default).
- A payload that fails to deserialize (corrupt bytes or schema drift) is **evicted and recomputed**
  instead of failing every read until it expires.
- Cached `Result` / `Result<T>` values round-trip correctly via the `Cirreum.Result` System.Text.Json
  converter (ADR-0024).
- Tag-based eviction is unsupported by `IDistributedCache`: `RemoveByTagAsync` / `RemoveByTagsAsync`
  throw `NotSupportedException` (use `Cirreum.Cache.Hybrid` for tag invalidation).

## Compatibility

- **Successor to a published package** — a package-id + code-first migration, not a new capability.
  See [`MIGRATION-v1.md`](MIGRATION-v1.md).
- **Depends on `Cirreum.Domain 1.1.1`** (code-first cache abstractions: `ICacheService` /
  `CacheExpirationPolicy`).

## See also

- `Cirreum.Cache.Hybrid` — the `HybridCache`-backed provider (L1+L2, stampede protection, tag invalidation)
