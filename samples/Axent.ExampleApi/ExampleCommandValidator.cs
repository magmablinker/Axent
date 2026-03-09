using FluentValidation;

namespace Axent.ExampleApi;

public sealed class ExampleCommandValidator : AbstractValidator<ExampleCommand>
{
    public ExampleCommandValidator()
    {
        RuleFor(r => r.Message)
            .NotEmpty()
            .MaximumLength(20);
    }
}
