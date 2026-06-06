namespace Cirreum.Cache.Distributed.Tests;

using Cirreum;
using Cirreum.Caching;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

public class DistributedCacheServiceTests {

	private const string Key = "cache-key";

	private static DistributedCacheService CreateService(IDistributedCache cache) =>
		new(cache);

	private static byte[] Serialize<T>(T value) =>
		JsonSerializer.SerializeToUtf8Bytes(value);

	private static ValueTask<T> Factory<T>(T value) =>
		new(value);

	// 1. Miss -> factory invoked exactly once; returned value equals factory output.
	[Fact]
	public async Task GetOrCreateAsync_Miss_InvokesFactoryOnce_AndReturnsItsOutput() {
		var cache = Substitute.For<IDistributedCache>();
		cache.GetAsync(Key, Arg.Any<CancellationToken>()).Returns((byte[]?)null);
		var service = CreateService(cache);

		var invocations = 0;

		var result = await service.GetOrCreateAsync(
			Key,
			_ => { invocations++; return Factory("produced"); },
			new CacheExpirationPolicy());

		result.Should().Be("produced");
		invocations.Should().Be(1);
	}

	// 2. Miss -> cache.SetAsync called once with the serialized bytes.
	[Fact]
	public async Task GetOrCreateAsync_Miss_CallsSetAsyncOnceWithSerializedBytes() {
		var cache = Substitute.For<IDistributedCache>();
		cache.GetAsync(Key, Arg.Any<CancellationToken>()).Returns((byte[]?)null);
		var service = CreateService(cache);

		byte[]? captured = null;
		await cache.SetAsync(
			Key,
			Arg.Do<byte[]>(b => captured = b),
			Arg.Any<DistributedCacheEntryOptions>(),
			Arg.Any<CancellationToken>());

		await service.GetOrCreateAsync(Key, _ => Factory("produced"), new CacheExpirationPolicy());

		await cache.Received(1).SetAsync(
			Key,
			Arg.Any<byte[]>(),
			Arg.Any<DistributedCacheEntryOptions>(),
			Arg.Any<CancellationToken>());

		captured.Should().NotBeNull();
		captured.Should().Equal(Serialize("produced"));
	}

	// 3. Miss with Expiration = 10min -> captured options.AbsoluteExpirationRelativeToNow == 10min.
	[Fact]
	public async Task GetOrCreateAsync_Miss_WithExpiration_UsesThatAbsoluteExpiration() {
		var cache = Substitute.For<IDistributedCache>();
		cache.GetAsync(Key, Arg.Any<CancellationToken>()).Returns((byte[]?)null);
		var service = CreateService(cache);

		var captured = await CaptureSetOptions(cache, service,
			settings: new CacheExpirationPolicy(Expiration: TimeSpan.FromMinutes(10)),
			value: "produced");

		captured.AbsoluteExpirationRelativeToNow.Should().Be(TimeSpan.FromMinutes(10));
	}

	// 4. Miss with no expiration -> captured options.AbsoluteExpirationRelativeToNow == 5min (the default).
	[Fact]
	public async Task GetOrCreateAsync_Miss_WithoutExpiration_UsesFiveMinuteDefault() {
		var cache = Substitute.For<IDistributedCache>();
		cache.GetAsync(Key, Arg.Any<CancellationToken>()).Returns((byte[]?)null);
		var service = CreateService(cache);

		var captured = await CaptureSetOptions(cache, service,
			settings: new CacheExpirationPolicy(),
			value: "produced");

		captured.AbsoluteExpirationRelativeToNow.Should().Be(TimeSpan.FromMinutes(5));
	}

	// Extra: non-positive expiration also falls back to the 5-minute default.
	[Fact]
	public async Task GetOrCreateAsync_Miss_WithNonPositiveExpiration_UsesFiveMinuteDefault() {
		var cache = Substitute.For<IDistributedCache>();
		cache.GetAsync(Key, Arg.Any<CancellationToken>()).Returns((byte[]?)null);
		var service = CreateService(cache);

		var captured = await CaptureSetOptions(cache, service,
			settings: new CacheExpirationPolicy(Expiration: TimeSpan.Zero),
			value: "produced");

		captured.AbsoluteExpirationRelativeToNow.Should().Be(TimeSpan.FromMinutes(5));
	}

	// 5. Hit (valid serialized bytes) -> returns that value; factory NEVER invoked; SetAsync NEVER called.
	[Fact]
	public async Task GetOrCreateAsync_Hit_ReturnsCachedValue_WithoutFactoryOrSet() {
		var cache = Substitute.For<IDistributedCache>();
		cache.GetAsync(Key, Arg.Any<CancellationToken>()).Returns(Serialize("cached"));
		var service = CreateService(cache);

		var invocations = 0;

		var result = await service.GetOrCreateAsync(
			Key,
			_ => { invocations++; return Factory("produced"); },
			new CacheExpirationPolicy());

		result.Should().Be("cached");
		invocations.Should().Be(0);
		await cache.DidNotReceive().SetAsync(
			Arg.Any<string>(),
			Arg.Any<byte[]>(),
			Arg.Any<DistributedCacheEntryOptions>(),
			Arg.Any<CancellationToken>());
	}

	// 6. Poison hit (invalid bytes) -> RemoveAsync(key) once, factory invoked, recomputed value returned.
	[Fact]
	public async Task GetOrCreateAsync_PoisonHit_EvictsAndRecomputes() {
		var cache = Substitute.For<IDistributedCache>();
		cache.GetAsync(Key, Arg.Any<CancellationToken>()).Returns(new byte[] { 1, 2, 3 });
		var service = CreateService(cache);

		var invocations = 0;

		var result = await service.GetOrCreateAsync(
			Key,
			_ => { invocations++; return Factory("produced"); },
			new CacheExpirationPolicy());

		result.Should().Be("produced");
		invocations.Should().Be(1);
		await cache.Received(1).RemoveAsync(Key, Arg.Any<CancellationToken>());
	}

	// 7. Failure value + FailureExpiration=1min, Expiration=10min -> options == 1min.
	[Fact]
	public async Task GetOrCreateAsync_FailureValue_WithFailureExpiration_UsesFailureExpiration() {
		var cache = Substitute.For<IDistributedCache>();
		cache.GetAsync(Key, Arg.Any<CancellationToken>()).Returns((byte[]?)null);
		var service = CreateService(cache);

		var captured = await CaptureSetOptions(cache, service,
			settings: new CacheExpirationPolicy(
				Expiration: TimeSpan.FromMinutes(10),
				FailureExpiration: TimeSpan.FromMinutes(1)),
			value: Result<string>.Fail(new InvalidOperationException("boom")));

		captured.AbsoluteExpirationRelativeToNow.Should().Be(TimeSpan.FromMinutes(1));
	}

	// 8. Failure value but FailureExpiration null (Expiration=10min) -> options == 10min (failure path NOT taken).
	[Fact]
	public async Task GetOrCreateAsync_FailureValue_WithoutFailureExpiration_UsesExpiration() {
		var cache = Substitute.For<IDistributedCache>();
		cache.GetAsync(Key, Arg.Any<CancellationToken>()).Returns((byte[]?)null);
		var service = CreateService(cache);

		var captured = await CaptureSetOptions(cache, service,
			settings: new CacheExpirationPolicy(Expiration: TimeSpan.FromMinutes(10)),
			value: Result<string>.Fail(new InvalidOperationException("boom")));

		captured.AbsoluteExpirationRelativeToNow.Should().Be(TimeSpan.FromMinutes(10));
	}

	// 9. Success value + FailureExpiration=1min, Expiration=10min -> options == 10min.
	[Fact]
	public async Task GetOrCreateAsync_SuccessValue_NeverUsesFailureExpiration() {
		var cache = Substitute.For<IDistributedCache>();
		cache.GetAsync(Key, Arg.Any<CancellationToken>()).Returns((byte[]?)null);
		var service = CreateService(cache);

		var captured = await CaptureSetOptions(cache, service,
			settings: new CacheExpirationPolicy(
				Expiration: TimeSpan.FromMinutes(10),
				FailureExpiration: TimeSpan.FromMinutes(1)),
			value: Result<string>.Success("ok"));

		captured.AbsoluteExpirationRelativeToNow.Should().Be(TimeSpan.FromMinutes(10));
	}

	// 10. RemoveAsync(key) -> cache.Received(1).RemoveAsync(key, ...).
	[Fact]
	public async Task RemoveAsync_DelegatesToCache() {
		var cache = Substitute.For<IDistributedCache>();
		var service = CreateService(cache);

		await service.RemoveAsync(Key, CancellationToken.None);

		await cache.Received(1).RemoveAsync(Key, Arg.Any<CancellationToken>());
	}

	// 11. RemoveByTagAsync -> throws NotSupportedException.
	[Fact]
	public async Task RemoveByTagAsync_ThrowsNotSupported() {
		var cache = Substitute.For<IDistributedCache>();
		var service = CreateService(cache);

		var act = async () => await service.RemoveByTagAsync("tag");

		await act.Should().ThrowAsync<NotSupportedException>();
	}

	// 12. RemoveByTagsAsync -> throws NotSupportedException.
	[Fact]
	public async Task RemoveByTagsAsync_ThrowsNotSupported() {
		var cache = Substitute.For<IDistributedCache>();
		var service = CreateService(cache);

		var act = async () => await service.RemoveByTagsAsync(["tag-a", "tag-b"]);

		await act.Should().ThrowAsync<NotSupportedException>();
	}

	// 13. Round-trip: serialize a record on a miss (capture bytes), feed back as a hit on a fresh service,
	//     assert deserialized value equals original.
	[Fact]
	public async Task GetOrCreateAsync_RoundTrip_HitOnPreviouslyStoredBytes_ReturnsEquivalentValue() {
		var original = new SampleRecord(42, "answer", true);

		// First service: miss -> compute -> capture the bytes written via SetAsync.
		var writeCache = Substitute.For<IDistributedCache>();
		writeCache.GetAsync(Key, Arg.Any<CancellationToken>()).Returns((byte[]?)null);
		var writeService = CreateService(writeCache);

		var captured = await CaptureSetBytes(writeCache, writeService, original);
		captured.Should().NotBeNull();

		// Fresh service: feed the captured bytes back as a hit.
		var readCache = Substitute.For<IDistributedCache>();
		readCache.GetAsync(Key, Arg.Any<CancellationToken>()).Returns(captured);
		var readService = CreateService(readCache);

		var roundTripped = await readService.GetOrCreateAsync(
			Key,
			_ => Factory(new SampleRecord(0, "should-not-run", false)),
			new CacheExpirationPolicy());

		roundTripped.Should().Be(original);
	}

	// Extra: Result<T> success value round-trips a hit and stays a success (proves the JsonConverter path).
	[Fact]
	public async Task GetOrCreateAsync_RoundTrip_ResultSuccess_RemainsSuccess() {
		var cache = Substitute.For<IDistributedCache>();
		cache.GetAsync(Key, Arg.Any<CancellationToken>())
			.Returns(Serialize(Result<string>.Success("ok")));
		var service = CreateService(cache);

		var result = await service.GetOrCreateAsync(
			Key,
			_ => Factory(Result<string>.Fail(new InvalidOperationException("should-not-run"))),
			new CacheExpirationPolicy());

		result.IsSuccess.Should().BeTrue();
		result.Value.Should().Be("ok");
	}

	// Extra: an empty (zero-length) cached payload is treated as a poison/invalid hit -> evict + recompute.
	[Fact]
	public async Task GetOrCreateAsync_EmptyCachedBytes_EvictsAndRecomputes() {
		var cache = Substitute.For<IDistributedCache>();
		cache.GetAsync(Key, Arg.Any<CancellationToken>()).Returns([]);
		var service = CreateService(cache);

		var invocations = 0;

		var result = await service.GetOrCreateAsync(
			Key,
			_ => { invocations++; return Factory("produced"); },
			new CacheExpirationPolicy());

		result.Should().Be("produced");
		invocations.Should().Be(1);
		await cache.Received(1).RemoveAsync(Key, Arg.Any<CancellationToken>());
	}

	private static async Task<DistributedCacheEntryOptions> CaptureSetOptions<T>(
		IDistributedCache cache,
		DistributedCacheService service,
		CacheExpirationPolicy settings,
		T value) {

		DistributedCacheEntryOptions? captured = null;
		await cache.SetAsync(
			Key,
			Arg.Any<byte[]>(),
			Arg.Do<DistributedCacheEntryOptions>(o => captured = o),
			Arg.Any<CancellationToken>());

		await service.GetOrCreateAsync(Key, _ => new ValueTask<T>(value), settings);

		captured.Should().NotBeNull();
		return captured!;
	}

	private static async Task<byte[]?> CaptureSetBytes<T>(
		IDistributedCache cache,
		DistributedCacheService service,
		T value) {

		byte[]? captured = null;
		await cache.SetAsync(
			Key,
			Arg.Do<byte[]>(b => captured = b),
			Arg.Any<DistributedCacheEntryOptions>(),
			Arg.Any<CancellationToken>());

		await service.GetOrCreateAsync(Key, _ => new ValueTask<T>(value), new CacheExpirationPolicy());

		return captured;
	}
}
