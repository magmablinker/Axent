using Axent.Abstractions.Models;

namespace Axent.Abstractions.Factories;

public interface IErrorFactory
{
    Error Create(Exception exception);
}
