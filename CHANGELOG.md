# Changelog

## [3.0.0]

### Breaking Changes

- Update Solution to .NET 10
- Improve source generator performance
    - Introduce `AxentRequestAttribute` for marking commands/queries/requests

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
