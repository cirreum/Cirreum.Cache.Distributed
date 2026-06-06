namespace Cirreum.Cache.Distributed.Tests;

using Cirreum.Cache.Distributed.Extensions;
using Cirreum.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;

public class ServiceCollectionExtensionsTests {

	// 14. No IDistributedCache registered -> throws InvalidOperationException.
	[Fact]
	public void AddDistributedCacheService_WithoutDistributedCache_Throws() {
		var services = new ServiceCollection();

		var act = () => services.AddDistributedCacheService();

		act.Should().Throw<InvalidOperationException>();
	}

	// 15. After registering an IDistributedCache -> a descriptor for ICacheService is present.
	[Fact]
	public void AddDistributedCacheService_AfterDistributedCacheRegistered_RegistersCacheServiceDescriptor() {
		var services = new ServiceCollection();
		services.AddSingleton(Substitute.For<IDistributedCache>());

		services.AddDistributedCacheService();

		services.Any(d => d.ServiceType == typeof(ICacheService)).Should().BeTrue();
	}

	// 16. Null service collection -> throws ArgumentNullException.
	[Fact]
	public void AddDistributedCacheService_NullServices_Throws() {
		var act = () => ((IServiceCollection)null!).AddDistributedCacheService();

		act.Should().Throw<ArgumentNullException>();
	}

	// Extra: returns the same collection instance for chaining.
	[Fact]
	public void AddDistributedCacheService_ReturnsSameCollectionForChaining() {
		var services = new ServiceCollection();
		services.AddSingleton(Substitute.For<IDistributedCache>());

		var returned = services.AddDistributedCacheService();

		returned.Should().BeSameAs(services);
	}
}
