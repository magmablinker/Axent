# Changelog

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
