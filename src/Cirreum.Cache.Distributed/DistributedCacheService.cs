namespace Cirreum.Cache.Distributed;

using Cirreum.Caching;
using Microsoft.Extensions.Caching.Distributed;
using System.Collections.Generic;
using System.Text.Json;

sealed class DistributedCacheService(
	IDistributedCache cache,
	JsonSerializerOptions? serializerOptions = null
) : ICacheService {

	// IDistributedCache exposes no built-in default TTL. A null (or non-positive) expiration would persist the
	// entry indefinitely — until the backing store evicts it — so when no expiration is configured (neither
	// per-query nor via the global CacheSettings.DefaultExpiration the intercept folds in) we fall back to this
	// bounded default. Mirrors HybridCache's out-of-the-box behaviour of always bounding entry lifetime.
	private static readonly TimeSpan DefaultAbsoluteExpiration = TimeSpan.FromMinutes(5);

	private readonly JsonSerializerOptions _serializerOptions =
		serializerOptions ?? new JsonSerializerOptions();

	public async ValueTask<TResponse> GetOrCreateAsync<TResponse>(
		string cacheKey,
		Func<CancellationToken, ValueTask<TResponse>> factory,
		CacheExpirationPolicy settings,
		string[]? tags = null,
		CancellationToken cancellationToken = default) {

		var cached = await cache.GetAsync(cacheKey, cancellationToken);
		if (cached is not null) {
			if (this.TryDeserialize<TResponse>(cached, out var hit)) {
				return hit;
			}

			// Corrupt bytes or schema drift: a poison entry would otherwise fail every read until it expires.
			// Evict it and fall through to recompute.
			await cache.RemoveAsync(cacheKey, cancellationToken);
		}

		var value = await factory(cancellationToken);

		var useFailureExpiration = value is IResult { IsSuccess: false }
			&& settings.FailureExpiration.HasValue;

		var options = CreateOptions(settings, useFailureExpiration);
		var bytes = JsonSerializer.SerializeToUtf8Bytes(value, _serializerOptions);

		await cache.SetAsync(cacheKey, bytes, options, cancellationToken);

		return value;
	}

	public ValueTask RemoveAsync(string cacheKey, CancellationToken cancellationToken) =>
		new(cache.RemoveAsync(cacheKey, cancellationToken));

	public ValueTask RemoveByTagAsync(string tag, CancellationToken cancellationToken = default) {
		throw new NotSupportedException("IDistributedCache does not support tag-based eviction.");
	}

	public ValueTask RemoveByTagsAsync(IEnumerable<string> tags, CancellationToken cancellationToken = default) {
		throw new NotSupportedException("IDistributedCache does not support tag-based eviction.");
	}

	private bool TryDeserialize<TResponse>(byte[] cached, out TResponse value) {
		try {
			var deserialized = JsonSerializer.Deserialize<TResponse>(cached, _serializerOptions);
			if (deserialized is null) {
				value = default!;
				return false;
			}
			value = deserialized;
			return true;
		} catch (JsonException) {
			value = default!;
			return false;
		}
	}

	private static DistributedCacheEntryOptions CreateOptions(
		CacheExpirationPolicy settings,
		bool useFailureExpiration = false) {

		var configured = useFailureExpiration && settings.FailureExpiration.HasValue
			? settings.FailureExpiration.Value
			: settings.Expiration;

		var expiration = configured is { } ttl && ttl > TimeSpan.Zero
			? ttl
			: DefaultAbsoluteExpiration;

		return new DistributedCacheEntryOptions {
			AbsoluteExpirationRelativeToNow = expiration
		};
	}

}
