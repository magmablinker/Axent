using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Axent.Extensions.Authorization;

internal sealed class HttpContextPrincipalAccessor : IPrincipalAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextPrincipalAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public ClaimsPrincipal Principal =>
        _httpContextAccessor.HttpContext?.User
        ?? new ClaimsPrincipal(new ClaimsIdentity());
}
