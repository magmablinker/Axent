using FluentValidation;

namespace Axent.Example.Application;

public sealed class ExampleCommandValidator : AbstractValidator<ExampleCommand>
{
    public ExampleCommandValidator()
    {
        RuleFor(r => r.Message)
            .NotEmpty()
            .MaximumLength(20);
    }
}
