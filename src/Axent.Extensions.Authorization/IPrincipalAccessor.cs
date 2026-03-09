using System.Security.Claims;

namespace Axent.Extensions.Authorization;

internal interface IPrincipalAccessor
{
    ClaimsPrincipal Principal { get; }
}
