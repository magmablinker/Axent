# Changelog

## [4.0.1]

### Features Added

- Add user, tenant, and culture cache scopes with configurable unresolved-scope behavior and optional eviction tags.
- Add dependency-injected cache key and cache scope providers.
- Add claims-backed ASP.NET Core user and tenant cache scope providers.
- Add cache-tag eviction restricted to one user, tenant, or culture discriminator.
- Warn when an authorized cacheable query implicitly uses global scope.
- Add redis implementation for ICache

### Bugs Fixed

- Ensure authorization runs before caching so cache hits cannot bypass authorization.

### Breaking Changes

- Refactor cache setup

## [4.0.0]

### Breaking Changes

- Replace `RequestContext<TRequest>` handler input with direct `TRequest` input.
- Replace `IPipelineChain<TRequest, TResponse>` with generated continuation delegates for custom pipes.
- Replace generated pipeline/request-module dispatch with generated `IRequestSender<TRequest, TResponse>` implementations.

### Features Added

- Add `IRequestSender<TRequest, TResponse>` as the recommended high-performance sender API.
- Keep `ISender` as a dynamic compatibility adapter that resolves generated typed senders across multiple assemblies.
- Make `ResponseBase` and `Response<TResponse>` readonly structs to reduce framework allocations.

### Bugs Fixed

- Fix multi-assembly `ISender` dispatch where one generated assembly sender could shadow requests generated in another assembly.

## [3.0.1]

#### Bugs Fixed

- Fix issue which caused whole request module to be instantiated on every request

## [3.0.0]

### Breaking Changes

- Update Solution to .NET 10
- Improve source generator performance
  - Introduce `AxentRequestAttribute` for marking commands/queries/requests

### Bugs Fixed

- Update Scriban to 7.2.0 to avoid vulnerability of previous version

## [2.0.2]

### Features Added

- Add `PaymentRequired` to `ErrorDefaults`

## [2.0.1]

### Features Added

- Add tags to `CacheEntryOptions`

## [2.0.0]

### Breaking Changes

- Rework source generation so multiple assemblies work properly

### Bugs Fixed

- Only complete transaction if request has been completed successfully
- Bump scriban version to 7.1.0

## [1.2.2]

### Breaking Changes

- Adjust namespaces of `Axent.Abstractions` package
- Change visibility of `AxentBuilder` and replace it with `IAxentBuilder`

### Features Added

- Add `Axent.Extensions.Authorization` for request authorization
- Add `Axent.Extensions.Caching` for response caching

## [1.2.1]

### Features Added

- Add `dotnet new axent-api` template for easier setup
- Add `Axent.Extensions.FluentValidation` for validation

## [1.2.0]

### Breaking Changes

- `Task<Response<TResponse>>` return type changed to `ValueTask<Response<TResponse>>` across all interface methods
- Removed reflection-based implementation

### Features Added

- Improved source-generated dispatcher performance
- Added built-in pipes for
  - observability
  - request logging
  - transactions

## [1.1.0]

### Features Added

- Replaced reflection-based `ISender` with a source-generated implementation for improved performance

## [1.0.1]

### Features Added

- Simplified pipeline registration
- Improved `Result` class instantiation — `Result.Success(new ResponseDto())` replaces `Result<ResponseDto>.Success(new ResponseDto())`

## [1.0.0]

### Features Added

- Initial release
