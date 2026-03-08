namespace Axent.Abstractions;

public interface IErrorFactory
{
    Error Create(Exception exception);
}
