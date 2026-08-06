namespace Axent.Extensions.Caching;

public sealed class AxentCacheSetup
{
    public Action<AxentCachingOptions>? ConfigureOptions { get; set; }
    public Action<IAxentCacheBuilder>? ConfigureCache { get; set; }
}
