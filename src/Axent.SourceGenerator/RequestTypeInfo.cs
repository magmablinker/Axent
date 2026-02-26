namespace Axent.SourceGenerator;

internal sealed record RequestTypeInfo(
    string RequestFullName,
    string ResponseFullName,
    string RequestNamespace
);