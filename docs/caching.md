# 💾 Caching

Axent supports response caching through the pipeline.

Caching is designed to stay simple and provider-agnostic:

- Axent only depends on an `ICache` abstraction
- the built-in implementation uses `IMemoryCache`
- consumers can replace it with Redis or any other backplane by implementing `ICache`
- only requests that explicitly opt in are cached

Caching is typically best suited for queries and other read-only requests.

## 📦 Installation
Install the caching extension package:

```shell
dotnet add package Axent.Extensions.Caching
```

## ⚙️ Registration
Register caching and configure it.

```csharp
builder.Services.AddAxent()
    .AddCaching();
```
> The default `ICache` implementation uses `IMemoryCache`

## ✅ Create a cacheable query
A request opts into caching by implementing ICacheableQuery<TResponse>.

When a cached request is sent:
1. Axent checks the cache for the request key
2. if a cached value exists, the handler is skipped
3. if no cached value exists, the handler runs normally
4. successful responses are stored in the cache
5. later requests with the same cache key reuse the cached response

```csharp
public sealed record GetOrderQuery(Guid OrderId) : ICacheableQuery<OrderDto>
{
    public string CacheKey => $"order:{OrderId}";

    public CacheEntryOptions CacheOptions => new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
    };

    public bool BypassCache => false;
}
```

## 🛠️ Create the handler
The handler itself does not need any special caching logic.

## 🧾 Cache options
CacheEntryOptions lets you control how long an item should stay in the cache.

```c#
public sealed record GetDashboardQuery(Guid UserId) : ICacheableQuery<DashboardDto>
{
    public string CacheKey => $"dashboard:{UserId}";

    public CacheEntryOptions CacheOptions => new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
        SlidingExpiration = TimeSpan.FromMinutes(2)
    };
}
```

Supported options

* `AbsoluteExpirationRelativeToNow`
  - Removes the entry after a fixed amount of time

* `SlidingExpiration`
  - Extends the lifetime while the entry continues to be accessed

## ⏭️ Bypass the cache
A request can decide to bypass the cache completely.

```csharp
public sealed record SearchProductsQuery(string Term, bool ForceRefresh)
    : ICacheableQuery<SearchProductsResponse>
{
    public string CacheKey => $"products:search:{Term.Trim().ToLowerInvariant()}";

    public CacheEntryOptions CacheOptions => new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2)
    };

    public bool BypassCache => ForceRefresh;
}
```
This is useful when you want to force a fresh read without changing the general caching behavior.

## 🏗️ Implement a custom cache provider
If you want to use Redis, a database, or any other storage, implement ICache.

```csharp
public interface ICache
{
    ValueTask<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    ValueTask SetAsync<T>(string key, T value, CacheEntryOptions? options = null, CancellationToken cancellationToken = default);
    ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default);
}
```

Example skeleton

```csharp
public sealed class CustomCache : ICache
{
    public ValueTask<CacheResult<T>> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public ValueTask SetAsync<T>(string key, T value, CacheEntryOptions? options = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
```
Register your implementation:

```csharp
builder.Services.AddSingleton<ICache, CustomCache>();
```

## 🧼 Removing cache entries
`ICache` also exposes `RemoveAsync`, which allows cache invalidation from your own application code.

```csharp
await cache.RemoveAsync($"order:{orderId}", cancellationToken);
```

A common pattern is to remove cached entries after a successful command that changes data.

## 📌 Notes
* caching is opt-in
* only requests implementing ICacheableQuery<TResponse> are cached
* handlers do not need to know whether a request is cached
* ICache is interchangeable, so consumers can plug in their own providers
* in-memory caching is best for a single application instance
* distributed providers such as Redis are better when multiple instances need to share cached data
