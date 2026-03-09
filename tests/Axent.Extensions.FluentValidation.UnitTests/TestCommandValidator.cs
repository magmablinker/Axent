using Axent.Tests.Shared;
using FluentValidation;

namespace Axent.Extensions.FluentValidation.UnitTests;

public sealed class TestCommandValidator : AbstractValidator<TestCommand>
{
    public TestCommandValidator()
    {
        RuleFor(c => c.Message)
            .NotEmpty()
            .MaximumLength(20);
    }
}
