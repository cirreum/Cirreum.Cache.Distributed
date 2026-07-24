# Changelog

All notable changes to **Cirreum.Cache.Distributed** are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

For detailed migration steps on major version bumps, see the per-version migration
guides linked at the bottom of each entry.

---

## [Unreleased]

### Updated

- Updated NuGet packages.

## [1.0.6] - 2026-07-20

### Updated

- Updated NuGet packages.

## [1.0.5] - 2026-07-19

### Updated

- Updated NuGet packages.

## [1.0.2] - 2026-07-04

### Updated

- Updated NuGet packages.

## [1.0.1] - 2026-07-04

### Updated

- Updated NuGet packages.

## [1.0.0] - 2026-07-03

### Added

- Initial release of **Cirreum.Cache.Distributed**, the `IDistributedCache`-backed
  implementation of `ICacheService`. Supersedes the legacy `Cirreum.QueryCache.Distributed`
  package (now archived); the lineage moved to a new package id alongside the Cirreum 1.0
  foundation reset and the code-first caching model.
- `AddDistributedCacheService()` registers `DistributedCacheService` as the active
  `ICacheService` via the foundation's code-first `AddCacheService(factory)` helper — the
  registration call itself selects the provider (no `CacheProvider` enum, no
  `Cirreum:Cache:Provider` appsettings switch). Fails fast at registration time if no
  `IDistributedCache` has been registered (e.g. `AddStackExchangeRedisCache`,
  `AddSqlServerCache`, `AddDistributedMemoryCache`).
- Targets `Cirreum.Domain` 1.x and consumes the code-first cache abstractions
  (`ICacheService` / `CacheExpirationPolicy`).
- Entries with no configured expiration are bounded by a default 5-minute TTL rather than
  persisting indefinitely (`IDistributedCache` has no built-in default).
- A payload that fails to deserialize (corrupt bytes or schema drift) is evicted and
  recomputed instead of failing every read until it expires.
- Cached `Result` / `Result<T>` values round-trip correctly via the `Cirreum.Result`
  System.Text.Json converter (see ADR-0024).
- Tag-based eviction is unsupported by `IDistributedCache`: `RemoveByTagAsync` /
  `RemoveByTagsAsync` throw `NotSupportedException`.
