namespace Cirreum.Cache.Distributed.Tests;

/// <summary>
/// Small serializable value used to prove a System.Text.Json round-trip through the cache.
/// </summary>
public sealed record SampleRecord(int Id, string Name, bool Flag);
