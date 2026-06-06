namespace Cirreum.Cache.Distributed.Extensions;

using Cirreum;
using Cirreum.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Text.Json;

/// <summary>
/// Extension methods for registering the distributed cache service.
/// </summary>
public static class ServiceCollectionExtensions {

	/// <summary>
	/// Registers <see cref="DistributedCacheService"/> as the active <see cref="ICacheService"/> (replacing
	/// the no-op default), bridging Cirreum caching to a registered <see cref="IDistributedCache"/>.
	/// </summary>
	/// <remarks>
	/// Requires an <see cref="IDistributedCache"/> registered separately (e.g. <c>AddStackExchangeRedisCache</c>,
	/// <c>AddSqlServerCache</c>, <c>AddDistributedMemoryCache</c>) — throws at registration time if absent.
	/// Note: <see cref="IDistributedCache"/> has no tag-based eviction, so
	/// <see cref="ICacheService.RemoveByTagAsync"/> / <see cref="ICacheService.RemoveByTagsAsync"/> throw
	/// <see cref="System.NotSupportedException"/>.
	/// </remarks>
	/// <param name="services">The service collection.</param>
	/// <returns>The service collection for chaining.</returns>
	public static IServiceCollection AddDistributedCacheService(this IServiceCollection services) {
		ArgumentNullException.ThrowIfNull(services);

		if (!services.Any(static d => d.ServiceType == typeof(IDistributedCache))) {
			throw new InvalidOperationException(
				"AddDistributedCacheService requires an IDistributedCache to be registered first " +
				"(e.g. AddStackExchangeRedisCache / AddSqlServerCache / AddDistributedMemoryCache).");
		}

		return services.AddCacheService(static sp => new DistributedCacheService(
			sp.GetRequiredService<IDistributedCache>(),
			sp.GetService<JsonSerializerOptions>()));
	}
}
