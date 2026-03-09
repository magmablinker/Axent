# 🔒 Authorization
Axent supports request-level authorization by using the standard ASP.NET Core authorization system.
This means you can protect requests with the familiar [Authorize] and [AllowAnonymous] attributes and reuse your existing policies, requirements, and handlers.

Authorization is executed as part of the Axent pipeline before the request handler runs.

## ✅ What it supports
* [Authorize] on requests
* [Authorize(Policy = "...")] for policy-based authorization
* [Authorize(Roles = "...")] for role-based authorization
* [AllowAnonymous] to explicitly skip authorization
* Custom IAuthorizationHandler implementations
* Resource-based authorization by using the request itself as the authorization resource

## 📦 Installation
Install the authorization extension package:

```shell
dotnet add package Axent.Extensions.Authorization
```

## ⚙️ Registration
Register Axent authorization and configure your policies:

```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AtLeast21", policy =>
        policy.RequireClaim("Age", "21", "22", "23"));
});

builder.Services.AddAxent(options => builder.Configuration.Bind("AppSettings:Axent", options))
    .AddHandlersFromAssemblyContaining<ProtectedQueryHandler>()
    .AddAuthorization();
```

## 🛡️ Protect a request
Add the [Authorize] attribute to a request to require authorization before the handler is executed.

```csharp
using Microsoft.AspNetCore.Authorization;

[Authorize(Policy = "AtLeast21")]
public sealed record ProtectedQuery : IQuery<Unit>;

internal sealed class ProtectedQueryHandler : IRequestHandler<ProtectedQuery, Unit>
{
    public ValueTask<Response<Unit>> HandleAsync(
        RequestContext<ProtectedQuery> context,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(Response.Success(Unit.Value));
}
```

## 👤 Require any authenticated user
Use [Authorize] without additional parameters when any authenticated user should be allowed.

```csharp
using Microsoft.AspNetCore.Authorization;

[Authorize]
public sealed record GetProfileQuery : IQuery<UserProfileDto>;
```

## 👥 Restrict by role
You can also use role-based authorization.

```csharp
using Microsoft.AspNetCore.Authorization;

[Authorize(Roles = "Admin")]
public sealed record DeleteUserCommand(Guid UserId) : ICommand<Unit>;
```

## 🌍 Allow anonymous access

```csharp
using Microsoft.AspNetCore.Authorization;

[AllowAnonymous]
public sealed record LoginCommand(string Email, string Password) : ICommand<LoginResponse>;
```

## 🧠 Resource-based authorization
Axent passes the current request as the authorization resource.
This allows authorization handlers to inspect request data such as IDs or ownership information.

```csharp
using Microsoft.AspNetCore.Authorization;

public sealed class MustOwnOrderRequirement : IAuthorizationRequirement;

[Authorize(Policy = "MustOwnOrder")]
public sealed record GetOrderQuery(Guid OrderId) : IQuery<OrderDto>;

public sealed class MustOwnOrderHandler
    : AuthorizationHandler<MustOwnOrderRequirement, GetOrderQuery>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        MustOwnOrderRequirement requirement,
        GetOrderQuery resource)
    {
        var userId = context.User.FindFirst("sub")?.Value;

        if (userId is not null && UserOwnsOrder(userId, resource.OrderId))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }

    private static bool UserOwnsOrder(string userId, Guid orderId)
    {
        // Custom ownership check
        return true;
    }
}
```

Register the policy and handler:

```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("MustOwnOrder", policy =>
        policy.Requirements.Add(new MustOwnOrderRequirement()));
});

builder.Services.AddSingleton<IAuthorizationHandler, MustOwnOrderHandler>();
```

## ❌ Failed authorization
If authorization fails, Axent stops pipeline execution and returns an error response without calling the request handler.
Typical results are:
* 401 Unauthorized when the user is not authenticated
* 403 Forbidden when the user is authenticated but does not meet the policy requirements

## ℹ️ Notes
* Authorization only applies to requests decorated with authorization attributes
* [AllowAnonymous] always skips authorization for that request
* Policies, roles, and custom handlers behave the same way as in ASP.NET Core
* This extension is a thin integration layer over the built-in ASP.NET Core authorization system
* See the official ASP.NET Core authorization documentation for more details: [Authorization policies in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/policies?view=aspnetcore-8.0)
