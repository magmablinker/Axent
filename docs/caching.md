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
    .AddCache(options =>
    {
        options.UnresolvedScopeBehavior = UnresolvedCacheScopeBehavior.Bypass;
        options.EmitScopeTags = true;
    });
```
> The default `ICache` implementation uses `IMemoryCache`

`UnresolvedScopeBehavior` defaults to `Bypass`, which executes the handler without reading or
writing a potentially unsafe cache entry. Set it to `Fail` to return an internal-server-error
response instead. Axent never falls back from an unresolved scoped key to a global key.

## ✅ Create a cacheable query
A request opts into caching by implementing ICacheableQuery<TResponse>.

When a cached request is sent:
1. Axent atomically gets or creates the value for the request key
2. if a cached value exists, the handler is skipped
3. concurrent misses allow only one handler execution at a time
4. successful responses are stored in the cache
5. failed or null responses are returned without being cached

```csharp
[Axent]
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

## Partition entries by ambient scope

`CacheScope` separates entries that have the same request key but belong to different callers or
runtime contexts. The default is `CacheScope.Global`, which shares one entry across all callers.
Use flags to select one or more dimensions:

* `CacheScope.User` partitions by the current user.
* `CacheScope.Tenant` partitions by the current tenant.
* `CacheScope.Culture` partitions by `CultureInfo.CurrentUICulture`.

```csharp
[Axent]
public sealed record GetDashboardQuery : ICacheableQuery<DashboardDto>
{
    public string CacheKey => "dashboard";
    public bool BypassCache => false;
    public CacheScope CacheScope => CacheScope.User | CacheScope.Tenant | CacheScope.Culture;
}
```

Culture resolution is registered by `AddCache`. For ASP.NET Core user and tenant resolution,
register the claims-backed providers:

```csharp
builder.Services.AddAxent()
    .AddCache()
    .AddHttpCacheScopes(options =>
    {
        options.UserClaimTypes = [ClaimTypes.NameIdentifier, "sub"];
        options.TenantClaimTypes = ["tenant_id", "tid"];
    });
```

`AddHttpCacheScopes` is provided by `Axent.Extensions.AspNetCore`. User scope requires an
authenticated principal. Tenant scope accepts a tenant claim from either an authenticated or an
anonymous principal. When no configured claim contains a non-empty value, the dimension is
unresolved and `UnresolvedScopeBehavior` applies.

To replace resolution for a built-in dimension, implement `ICacheScopeProvider` and register it
after the built-in providers. The last provider registered for a dimension wins.

```csharp
public sealed class TenantContextCacheScopeProvider(ITenantContext tenantContext)
    : ICacheScopeProvider
{
    public CacheScope Scope => CacheScope.Tenant;

    public ValueTask<string?> GetDiscriminatorAsync(
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult<string?>(tenantContext.TenantId);
}

builder.Services.AddAxent()
    .AddCache()
    .AddHttpCacheScopes()
    .AddCacheScopeProvider<TenantContextCacheScopeProvider>();
```

The source generator emits warning `AXENT003` when an authorized cacheable query does not declare
`CacheScope`. Declare an appropriate scope, or explicitly return `CacheScope.Global` when sharing
is intentional. Authorization always runs before caching, so a cache hit cannot bypass the
authorization gate.

## Build request keys with dependencies

`ICacheKeyProvider<TRequest>` can replace a request's `CacheKey` when key construction requires
services. Scope composition still wraps the returned key. Returning `null` bypasses caching for
that request.

```csharp
public sealed class ProductCacheKeyProvider(IProductVersionStore versions)
    : ICacheKeyProvider<GetProductQuery>
{
    public async ValueTask<string?> GetCacheKeyAsync(
        GetProductQuery request,
        CancellationToken cancellationToken = default)
    {
        var version = await versions.GetVersionAsync(request.ProductId, cancellationToken);
        return version is null ? null : $"product:{request.ProductId}:v{version}";
    }
}

builder.Services.AddAxent()
    .AddCache()
    .AddCacheKeyProvider<GetProductQuery, ProductCacheKeyProvider>();
```

## Evict scoped entries

Enable `EmitScopeTags` to add an implicit tag for every resolved scope dimension. These tags can
invalidate all entries for one user or tenant without depending on the internal cache-key format.

```csharp
builder.Services.AddAxent()
    .AddCache(options => options.EmitScopeTags = true);

await cache.RemoveByTagsAsync(
    [CacheScopeTags.User(userId), CacheScopeTags.Tenant(tenantId)],
    cancellationToken);
```

Scope-tag emission is disabled by default because the in-memory provider retains one expiration
token per distinct tag until that tag is removed.

## 🛠️ Create the handler
The handler itself does not need any special caching logic.

## 🧾 Cache options
CacheEntryOptions lets you control how long an item should stay in the cache.

```c#
[Axent]
public sealed record GetDashboardQuery(Guid UserId) : ICacheableQuery<DashboardDto>
{
    public string CacheKey => $"dashboard:{UserId}";

    public CacheEntryOptions CacheOptions => new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
        SlidingExpiration = TimeSpan.FromMinutes(2),
        Tags = ["dashboard"]
    };
}
```

Supported options

* `AbsoluteExpirationRelativeToNow`
  - Removes the entry after a fixed amount of time

* `SlidingExpiration`
  - Extends the lifetime while the entry continues to be accessed

* `Tags`
  - Group cache values by tag to remove multiple cached values in one go.

## ⏭️ Bypass the cache
A request can decide to bypass the cache completely.

```csharp
[Axent]
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
    ValueTask<Response<T>> GetOrCreateAsync<T>(
        string key,
        Func<ValueTask<Response<T>>> factory,
        CacheEntryOptions? options = null,
        CancellationToken cancellationToken = default);
    ValueTask<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    ValueTask SetAsync<T>(string key, T value, CacheEntryOptions? options = null, CancellationToken cancellationToken = default);
    ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default);
    ValueTask RemoveByTagsAsync(IEnumerable<string> tags, CancellationToken cancellation = default);
}
```

Example skeleton

```csharp
public sealed class CustomCache : ICache
{
    public ValueTask<Response<T>> GetOrCreateAsync<T>(
        string key,
        Func<ValueTask<Response<T>>> factory,
        CacheEntryOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public ValueTask<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
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

    public ValueTask RemoveByTagsAsync(IEnumerable<string> tags, CancellationToken cancellation = default)
    {
        throw new NotImplementedException();
    }
}
```

GetOrCreateAsync owns cache stampede protection. An in-memory provider must
coordinate callers in one process. A distributed provider must coordinate the
same key across every application instance, recheck the cache after acquiring
its distributed lock, and cache only successful non-null responses.

Register your implementation:

```csharp
builder.Services.AddSingleton<ICache, CustomCache>();
```

## 🧼 Removing cache entries

### Single entry
`ICache` exposes `RemoveAsync`, which allows cache invalidation fpr single entries.

```csharp
await cache.RemoveAsync($"order:{orderId}", cancellationToken);
```

### Multiple entries
`ICache` exposes `RemoveByTagsAsync`, which allows cache invalidation for multiple entries.

```csharp
await cache.RemoveByTagsAsync(["orders"], cancellationToken);
```

> **Hint**: A common pattern is to remove cached entries after e.g a successful command that changes data.

## 📌 Notes
* caching is opt-in
* only requests implementing ICacheableQuery<TResponse> are cached
* global scope is the default; declare user, tenant, or culture scope for context-specific data
* handlers do not need to know whether a request is cached
* ICache is interchangeable, so consumers can plug in their own providers
* in-memory caching is best for a single application instance
* distributed providers such as Redis are better when multiple instances need to share cached data
