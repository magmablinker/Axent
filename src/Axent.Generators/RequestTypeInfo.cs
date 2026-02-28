// ReSharper disable NotAccessedPositionalProperty.Global
namespace Axent.Generators;

internal sealed record RequestTypeInfo(
    string RequestFullName,
    string ResponseFullName,
    string SymbolName);
