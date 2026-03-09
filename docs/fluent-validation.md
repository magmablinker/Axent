
# FluentValidation Support
Axent can automatically validate requests by using [FluentValidation](https://github.com/FluentValidation/FluentValidation) in the pipeline.  
If a validator exists for a request, it is executed before the remaining pipes and the handler.

1. Install the packages
```shell
dotnet add package Axent.Extensions.FluentValidation --version 1.2.1
dotnet add package FluentValidation.DependencyInjectionExtensions --version 12.1.1
```
> `FluentValidation.DependencyInjectionExtensions` is optional. It is only needed if you want to register validators through assembly scanning.

2. Create a validator
```csharp
public sealed class ExampleCommandValidator : AbstractValidator<ExampleCommand>
{
    public ExampleCommandValidator()
    {
        RuleFor(r => r.Message)
            .NotEmpty()
            .MaximumLength(20);
    }
}
```
> Validators must be public when they are discovered via assembly scanning.

3. Register validators and enable FluentValidation
```csharp
builder.Services.AddValidatorsFromAssemblyContaining<ExampleCommandValidator>();

builder.Services.AddAxent()
    .AddHandlersFromAssemblyContaining<ExampleCommandHandler>()
    .AddAutoFluentValidation();
```

Once configured, Axent will automatically run the validator for the incoming request and stop pipeline execution early if validation fails.
