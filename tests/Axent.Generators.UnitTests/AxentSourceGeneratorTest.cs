using Axent.Abstractions.Requests;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Axent.Generators.UnitTests;

public sealed class AxentSourceGeneratorTests
{
    private static (Compilation Output, IReadOnlyList<Diagnostic> Diagnostics, IReadOnlyList<SyntaxTree> GeneratedTrees) RunGenerator(string source)
    {
        _ = typeof(ICommand<>);

        var trustedAssemblies = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!
            .ToString()!
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path));

        var inputCompilation = CSharpCompilation.Create(
            "TestAssembly",
            [CSharpSyntaxTree.ParseText(source)],
            [
                ..trustedAssemblies,
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(CancellationToken).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(ICommand<>).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Core.DependencyInjection.AxentOptions).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Microsoft.Extensions.DependencyInjection.IServiceCollection).Assembly.Location),
            ],
            new(OutputKind.DynamicallyLinkedLibrary));

        var generator = new AxentSourceGenerator();
        var driver = CSharpGeneratorDriver
            .Create(generator)
            .RunGeneratorsAndUpdateCompilation(inputCompilation, out var outputCompilation, out var diagnostics);

        var generatedTrees = driver
            .GetRunResult()
            .GeneratedTrees;

        return (outputCompilation, diagnostics, generatedTrees);
    }

    private static string? GetGeneratedFile(IReadOnlyList<SyntaxTree> trees, string fileName)
        => trees.FirstOrDefault(t => t.FilePath.EndsWith(fileName))?.ToString();

    [Fact]
    public void Generator_Should_Produce_No_Diagnostics_For_Valid_Command()
    {
        // Arrange
        const string source = """
            using Axent.Abstractions.Requests;
            using Axent.Abstractions.Models;
            using Axent.Abstractions.Services;

            namespace TestNamespace;

            internal sealed class TestCommand : ICommand<TestResponse> { }
            internal sealed class TestResponse { }
            internal sealed class TestCommandHandler : IRequestHandler<TestCommand, TestResponse>
            {
                public ValueTask<Response<TestResponse>> HandleAsync(RequestContext<TestCommand> context, CancellationToken cancellationToken = default)
                    => ValueTask.FromResult(Response.Success(new TestResponse()));
            }
            """;

        // Act
        var (_, diagnostics, _) = RunGenerator(source);

        // Assert
        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
    }

    [Fact]
    public void Generator_Should_Produce_No_Diagnostics_For_Valid_Query()
    {
        // Arrange
        const string source = """
            using Axent.Abstractions.Requests;
            using Axent.Abstractions.Models;
            using Axent.Abstractions.Services;

            namespace TestNamespace;

            internal sealed class TestQuery : IQuery<TestResponse> { }
            internal sealed class TestResponse { }
            internal sealed class TestQueryHandler : IRequestHandler<TestQuery, TestResponse>
            {
                public ValueTask<Response<TestResponse>> HandleAsync(RequestContext<TestQuery> context, CancellationToken cancellationToken = default)
                    => ValueTask.FromResult(Response.Success(new TestResponse()));
            }
            """;

        // Act
        var (_, diagnostics, _) = RunGenerator(source);

        // Assert
        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
    }

    [Fact]
    public void Generator_Should_Generate_All_Expected_Files()
    {
        // Arrange
        const string source = """
            using Axent.Abstractions.Requests;
            using Axent.Abstractions.Models;
            using Axent.Abstractions.Services;

            namespace TestNamespace;

            internal sealed class TestCommand : ICommand<TestResponse> { }
            internal sealed class TestResponse { }
            internal sealed class TestCommandHandler : IRequestHandler<TestCommand, TestResponse>
            {
                public ValueTask<Response<TestResponse>> HandleAsync(RequestContext<TestCommand> context, CancellationToken cancellationToken = default)
                    => ValueTask.FromResult(Response.Success(new TestResponse()));
            }
            """;

        // Act
        var (_, _, generatedTrees) = RunGenerator(source);

        // Assert
        Assert.Contains(generatedTrees, t => t.FilePath.EndsWith("Sender.g.cs"));
        Assert.Contains(generatedTrees, t => t.FilePath.EndsWith("Pipelines.g.cs"));
        Assert.Contains(generatedTrees, t => t.FilePath.EndsWith("HandlerPipe.g.cs"));
    }

    [Fact]
    public void Generator_Should_Not_Generate_Files_When_No_Requests_Found()
    {
        // Arrange
        const string source = """
            namespace TestNamespace;

            internal sealed class NotARequest { }
            """;

        // Act
        var (_, _, generatedTrees) = RunGenerator(source);

        // Assert
        Assert.Empty(generatedTrees);
    }

    [Fact]
    public void Generator_Should_Set_IsCommand_True_For_ICommand()
    {
        // Arrange
        const string source = """
            using Axent.Abstractions.Requests;
            using Axent.Abstractions.Models;
            using Axent.Abstractions.Services;

            namespace TestNamespace;

            internal sealed class TestCommand : ICommand<TestResponse> { }
            internal sealed class TestResponse { }
            internal sealed class TestCommandHandler : IRequestHandler<TestCommand, TestResponse>
            {
                public ValueTask<Response<TestResponse>> HandleAsync(RequestContext<TestCommand> context, CancellationToken cancellationToken = default)
                    => ValueTask.FromResult(Response.Success(new TestResponse()));
            }
            """;

        // Act
        var (_, _, generatedTrees) = RunGenerator(source);

        // Assert
        var pipelineSource = GetGeneratedFile(generatedTrees, "Pipelines.g.cs");
        Assert.NotNull(pipelineSource);
        Assert.Contains("ITransactionPipe<", pipelineSource);
    }

    [Fact]
    public void Generator_Should_Set_IsCommand_False_For_IQuery()
    {
        // Arrange
        const string source = """
            using Axent.Abstractions.Requests;
            using Axent.Abstractions.Models;
            using Axent.Abstractions.Services;

            namespace TestNamespace;

            internal sealed class TestQuery : IQuery<TestResponse> { }
            internal sealed class TestResponse { }
            internal sealed class TestQueryHandler : IRequestHandler<TestQuery, TestResponse>
            {
                public ValueTask<Response<TestResponse>> HandleAsync(RequestContext<TestQuery> context, CancellationToken cancellationToken = default)
                    => ValueTask.FromResult(Response.Success(new TestResponse()));
            }
            """;

        // Act
        var (_, _, generatedTrees) = RunGenerator(source);

        // Assert
        var pipelineSource = GetGeneratedFile(generatedTrees, "Pipelines.g.cs");
        Assert.NotNull(pipelineSource);
        Assert.DoesNotContain("ITransactionPipe<", pipelineSource);
    }

    [Fact]
    public void Generator_Should_Set_IsCommand_False_For_Plain_IRequest()
    {
        // Arrange
        const string source = """
            using Axent.Abstractions.Requests;
            using Axent.Abstractions.Models;
            using Axent.Abstractions.Services;

            namespace TestNamespace;

            internal sealed class TestRequest : IRequest<TestResponse> { }
            internal sealed class TestResponse { }
            internal sealed class TestRequestHandler : IRequestHandler<TestRequest, TestResponse>
            {
                public ValueTask<Response<TestResponse>> HandleAsync(RequestContext<TestRequest> context, CancellationToken cancellationToken = default)
                    => ValueTask.FromResult(Response.Success(new TestResponse()));
            }
            """;

        // Act
        var (_, _, generatedTrees) = RunGenerator(source);

        // Assert
        var pipelineSource = GetGeneratedFile(generatedTrees, "Pipelines.g.cs");
        Assert.NotNull(pipelineSource);
        Assert.DoesNotContain("ITransactionPipe<", pipelineSource);
    }

    [Fact]
    public void Generator_Should_Generate_Sender_With_Correct_Request_Types()
    {
        // Arrange
        const string source = """
            using Axent.Abstractions.Requests;
            using Axent.Abstractions.Models;
            using Axent.Abstractions.Services;

            namespace TestNamespace;

            internal sealed class TestCommand : ICommand<TestResponse> { }
            internal sealed class TestResponse { }
            internal sealed class TestCommandHandler : IRequestHandler<TestCommand, TestResponse>
            {
                public ValueTask<Response<TestResponse>> HandleAsync(RequestContext<TestCommand> context, CancellationToken cancellationToken = default)
                    => ValueTask.FromResult(Response.Success(new TestResponse()));
            }
            """;

        // Act
        var (_, _, generatedTrees) = RunGenerator(source);

        // Assert
        var senderSource = GetGeneratedFile(generatedTrees, "Sender.g.cs");
        Assert.NotNull(senderSource);
        Assert.Contains("TestCommandPipeline", senderSource);
        Assert.Contains("SendAsync(global::TestNamespace.TestCommand", senderSource);
    }

    [Fact]
    public void Generator_Should_Generate_HandlerPipe_For_Each_Request()
    {
        // Arrange
        const string source = """
            using Axent.Abstractions.Requests;
            using Axent.Abstractions.Models;
            using Axent.Abstractions.Services;

            namespace TestNamespace;

            internal sealed class FirstCommand : ICommand<FirstResponse> { }
            internal sealed class FirstResponse { }
            internal sealed class FirstCommandHandler : IRequestHandler<FirstCommand, FirstResponse>
            {
                public ValueTask<Response<FirstResponse>> HandleAsync(RequestContext<FirstCommand> context, CancellationToken cancellationToken = default)
                    => ValueTask.FromResult(Response.Success(new FirstResponse()));
            }

            internal sealed class SecondQuery : IQuery<SecondResponse> { }
            internal sealed class SecondResponse { }
            internal sealed class SecondQueryHandler : IRequestHandler<SecondQuery, SecondResponse>
            {
                public ValueTask<Response<SecondResponse>> HandleAsync(RequestContext<SecondQuery> context, CancellationToken cancellationToken = default)
                    => ValueTask.FromResult(Response.Success(new SecondResponse()));
            }
            """;

        // Act
        var (_, _, generatedTrees) = RunGenerator(source);

        // Assert
        var handlerPipeSource = GetGeneratedFile(generatedTrees, "HandlerPipe.g.cs");
        Assert.NotNull(handlerPipeSource);
        Assert.Contains("FirstCommandHandlerPipe", handlerPipeSource);
        Assert.Contains("SecondQueryHandlerPipe", handlerPipeSource);
    }

    [Fact]
    public void Generator_Should_Skip_Abstract_Classes()
    {
        // Arrange
        const string source = """
            using Axent.Abstractions.Requests;
            using Axent.Abstractions.Models;
            using Axent.Abstractions.Services;

            namespace TestNamespace;

            internal abstract class AbstractCommand : ICommand<TestResponse> { }
            internal sealed class TestResponse { }
            """;

        // Act
        var (_, _, generatedTrees) = RunGenerator(source);

        // Assert
        Assert.Empty(generatedTrees);
    }

    [Fact]
    public void Generator_Should_Generate_Output_That_Compiles()
    {
        // Arrange
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using Axent.Abstractions.Requests;
            using Axent.Abstractions.Models;
            using Axent.Abstractions.Services;

            namespace TestNamespace;

            internal sealed class TestCommand : ICommand<TestResponse> { }
            internal sealed class TestResponse { }
            internal sealed class TestCommandHandler : IRequestHandler<TestCommand, TestResponse>
            {
                public ValueTask<Response<TestResponse>> HandleAsync(RequestContext<TestCommand> context, CancellationToken cancellationToken = default)
                    => ValueTask.FromResult(Response.Success(new TestResponse()));
            }
            """;

        // Act
        var (outputCompilation, _, _) = RunGenerator(source);

        // Assert
        var compilationDiagnostics = outputCompilation
            .GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();

        Assert.Empty(compilationDiagnostics);
    }
}
