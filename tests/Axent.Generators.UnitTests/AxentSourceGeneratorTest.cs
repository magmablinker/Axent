using Axent.Abstractions.Requests;
using Axent.Core.Attributes;
using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Axent.Generators.UnitTests;

public sealed class AxentSourceGeneratorTests
{
    private static (
        Compilation Output,
        IReadOnlyList<Diagnostic> Diagnostics,
        IReadOnlyList<SyntaxTree> GeneratedTrees)
        RunGenerator(string source)
    {
        _ = typeof(ICommand<>);

        var parseOptions = CSharpParseOptions.Default
            .WithLanguageVersion(LanguageVersion.Latest);

        var trustedAssemblies = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!
            .ToString()!
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path));

        var explicitReferences = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(ValueTask<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(CancellationToken).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(ICommand<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Core.DependencyInjection.AxentOptions).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(AxentModuleAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(IServiceCollection).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(ServiceCollectionServiceExtensions).Assembly.Location),
        };

        var references = trustedAssemblies
            .Concat(explicitReferences)
            .GroupBy(reference => reference.Display)
            .Select(group => group.First())
            .ToArray();

        var inputCompilation = CSharpCompilation.Create(
            "TestAssembly",
            [CSharpSyntaxTree.ParseText(source, parseOptions)],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithNullableContextOptions(NullableContextOptions.Enable));

        var generator = new AxentSourceGenerator().AsSourceGenerator();

        var driver = CSharpGeneratorDriver
            .Create(
                generators: [generator],
                parseOptions: parseOptions)
            .RunGeneratorsAndUpdateCompilation(
                inputCompilation,
                out var outputCompilation,
                out var diagnostics);

        var runResult = driver.GetRunResult();

        var allDiagnostics = inputCompilation.GetDiagnostics()
            .Concat(runResult.Diagnostics)
            .Concat(runResult.Results.SelectMany(result => result.Diagnostics))
            .Concat(diagnostics)
            .ToList();

        var generatedTrees = outputCompilation.SyntaxTrees
            .Where(tree => !inputCompilation.SyntaxTrees.Contains(tree))
            .ToList();

        return (outputCompilation, allDiagnostics, generatedTrees);
    }

    private static string GetGeneratedFile(
        IReadOnlyList<SyntaxTree> trees,
        string fileName)
    {
        var tree = trees.FirstOrDefault(tree =>
            tree.FilePath.EndsWith(fileName, StringComparison.Ordinal));

        Assert.True(
            tree is not null,
            $"""
             Expected generated file '{fileName}' was not found.

             Generated files:
             {string.Join(Environment.NewLine, trees.Select(tree => tree.FilePath))}
             """);

        return tree.ToString();
    }

    private static void AssertNoErrors(IEnumerable<Diagnostic> diagnostics)
    {
        var errors = diagnostics
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToList();

        Assert.True(
            errors.Count == 0,
            $"""
             Expected no errors, but found:

             {string.Join(Environment.NewLine, errors.Select(error => error.ToString()))}
             """);
    }

    [Fact]
    public void Generator_should_warn_when_authorized_cacheable_query_has_implicit_global_scope()
    {
        // Arrange
        const string source = """
            using System;
            using Axent.Abstractions.Attributes;
            using Axent.Abstractions.Requests;
            using Microsoft.AspNetCore.Authorization;

            namespace Microsoft.AspNetCore.Authorization
            {
                public interface IAuthorizeData { }

                [AttributeUsage(AttributeTargets.Class, Inherited = true)]
                public sealed class AuthorizeAttribute : Attribute, IAuthorizeData { }
            }

            namespace TestNamespace
            {
                [Axent]
                [Authorize]
                public sealed record AccountQuery : ICacheableQuery<string>
                {
                    public string CacheKey => "account";
                    public bool BypassCache => false;
                }
            }
            """;

        // Act
        var (_, diagnostics, _) = RunGenerator(source);

        // Assert
        var diagnostic = Assert.Single(
            diagnostics
                .Where(item => item.Id == "AXENT003")
                .DistinctBy(item => (
                    item.Id,
                    item.Location.SourceSpan,
                    item.GetMessage(CultureInfo.InvariantCulture))));
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Contains(
            "AccountQuery",
            diagnostic.GetMessage(CultureInfo.InvariantCulture),
            StringComparison.Ordinal);
        Assert.NotEqual(Location.None, diagnostic.Location);
    }

    [Theory]
    [InlineData("public CacheScope CacheScope => CacheScope.Global;", "")]
    [InlineData("", "[AllowAnonymous]")]
    public void Generator_should_not_warn_when_global_sharing_is_explicit_or_anonymous(
        string cacheScopeMember,
        string additionalAttribute)
    {
        // Arrange
        var source = $$"""
            using System;
            using Axent.Abstractions.Attributes;
            using Axent.Abstractions.Caching;
            using Axent.Abstractions.Requests;
            using Microsoft.AspNetCore.Authorization;

            namespace Microsoft.AspNetCore.Authorization
            {
                public interface IAuthorizeData { }
                public interface IAllowAnonymous { }

                [AttributeUsage(AttributeTargets.Class, Inherited = true)]
                public sealed class AuthorizeAttribute : Attribute, IAuthorizeData { }

                [AttributeUsage(AttributeTargets.Class, Inherited = true)]
                public sealed class AllowAnonymousAttribute : Attribute, IAllowAnonymous { }
            }

            namespace TestNamespace
            {
                [Axent]
                [Authorize]
                {{additionalAttribute}}
                public sealed record AccountQuery : ICacheableQuery<string>
                {
                    public string CacheKey => "account";
                    public bool BypassCache => false;
                    {{cacheScopeMember}}
                }
            }
            """;

        // Act
        var (_, diagnostics, _) = RunGenerator(source);

        // Assert
        Assert.DoesNotContain(diagnostics, item => item.Id == "AXENT003");
    }

    [Fact(Skip = "Generator test harness does not currently discover incremental generated trees.")]
    public void Generator_Should_Generate_RequestSender_And_Registrar_For_Valid_Request()
    {
        // Arrange
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using Axent.Abstractions.Attributes;
            using Axent.Abstractions.Requests;
            using Axent.Abstractions.Models;
            using Axent.Abstractions.Services;

            namespace TestNamespace;

            [AxentAttribute]
            public sealed record TestCommand : ICommand<TestResponse>;

            public sealed class TestResponse { }

            public sealed class TestCommandHandler : IRequestHandler<TestCommand, TestResponse>
            {
                public ValueTask<Response<TestResponse>> HandleAsync(
                    TestCommand request,
                    CancellationToken cancellationToken = default)
                    => ValueTask.FromResult(Response.Success(new TestResponse()));
            }
            """;

        // Act
        var (_, diagnostics, generatedTrees) = RunGenerator(source);

        // Assert
        AssertNoErrors(diagnostics);

        Assert.Contains(generatedTrees, tree => tree.FilePath.EndsWith(".Sender.g.cs", StringComparison.Ordinal));
        Assert.Contains(generatedTrees, tree => tree.FilePath.EndsWith("RegistrarTestAssembly.g.cs", StringComparison.Ordinal));

        var senderSource = GetGeneratedFile(generatedTrees, ".Sender.g.cs");
        Assert.Contains("IRequestSender<global::TestNamespace.TestCommand, global::TestNamespace.TestResponse>", senderSource);
        Assert.Contains("_handler.HandleAsync(request, cancellationToken)", senderSource);

        var registrarSource = GetGeneratedFile(generatedTrees, "RegistrarTestAssembly.g.cs");
        Assert.Contains("services.AddScoped<IRequestSender<global::TestNamespace.TestCommand, global::TestNamespace.TestResponse>", registrarSource);
        Assert.DoesNotContain("services.AddScoped<ISender", registrarSource);
    }

    [Fact(Skip = "Generator test harness does not currently discover incremental generated trees.")]
    public void Generator_Should_Generate_Output_That_Compiles_For_Typed_Sender()
    {
        // Arrange
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using Axent.Abstractions.Attributes;
            using Axent.Abstractions.Requests;
            using Axent.Abstractions.Models;
            using Axent.Abstractions.Services;

            namespace TestNamespace;

            [AxentAttribute]
            public sealed record TestQuery : IQuery<TestResponse>;

            public sealed class TestResponse { }

            public sealed class TestQueryHandler : IRequestHandler<TestQuery, TestResponse>
            {
                public ValueTask<Response<TestResponse>> HandleAsync(
                    TestQuery request,
                    CancellationToken cancellationToken = default)
                    => ValueTask.FromResult(Response.Success(new TestResponse()));
            }
            """;

        // Act
        var (outputCompilation, diagnostics, _) = RunGenerator(source);

        // Assert
        AssertNoErrors(diagnostics);
        AssertNoErrors(outputCompilation.GetDiagnostics(TestContext.Current.CancellationToken));
    }

    [Fact(Skip = "Skip")]
    public void Generator_Should_Produce_No_Diagnostics_For_Valid_Command()
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
                public ValueTask<Response<TestResponse>> HandleAsync(
                    RequestContext<TestCommand> context,
                    CancellationToken cancellationToken = default)
                    => ValueTask.FromResult(Response.Success(new TestResponse()));
            }
            """;

        // Act
        var (_, diagnostics, _) = RunGenerator(source);

        // Assert
        AssertNoErrors(diagnostics);
    }

    [Fact(Skip = "Skip")]
    public void Generator_Should_Produce_No_Diagnostics_For_Valid_Query()
    {
        // Arrange
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using Axent.Abstractions.Requests;
            using Axent.Abstractions.Models;
            using Axent.Abstractions.Services;

            namespace TestNamespace;

            internal sealed class TestQuery : IQuery<TestResponse> { }

            internal sealed class TestResponse { }

            internal sealed class TestQueryHandler : IRequestHandler<TestQuery, TestResponse>
            {
                public ValueTask<Response<TestResponse>> HandleAsync(
                    RequestContext<TestQuery> context,
                    CancellationToken cancellationToken = default)
                    => ValueTask.FromResult(Response.Success(new TestResponse()));
            }
            """;

        // Act
        var (_, diagnostics, _) = RunGenerator(source);

        // Assert
        AssertNoErrors(diagnostics);
    }

    [Fact(Skip = "Skip")]
    public void Generator_Should_Generate_All_Expected_Files()
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
                public ValueTask<Response<TestResponse>> HandleAsync(
                    RequestContext<TestCommand> context,
                    CancellationToken cancellationToken = default)
                    => ValueTask.FromResult(Response.Success(new TestResponse()));
            }
            """;

        // Act
        var (_, diagnostics, generatedTrees) = RunGenerator(source);

        // Assert
        AssertNoErrors(diagnostics);

        Assert.Contains(generatedTrees, tree => tree.FilePath.EndsWith("Pipelines.g.cs", StringComparison.Ordinal));
        Assert.Contains(generatedTrees, tree => tree.FilePath.EndsWith("HandlerPipe.g.cs", StringComparison.Ordinal));
        Assert.Contains(generatedTrees, tree => tree.FilePath.EndsWith("RequestModule.g.cs", StringComparison.Ordinal));
        Assert.Contains(generatedTrees, tree => tree.FilePath.EndsWith("Registrar.g.cs", StringComparison.Ordinal));
        Assert.DoesNotContain(generatedTrees, tree => tree.FilePath.EndsWith("Sender.g.cs", StringComparison.Ordinal));
    }

    [Fact(Skip = "Skip")]
    public void Generator_Should_Not_Generate_Files_When_No_Requests_Found()
    {
        // Arrange
        const string source = """
            namespace TestNamespace;

            internal sealed class NotARequest { }
            """;

        // Act
        var (_, diagnostics, generatedTrees) = RunGenerator(source);

        // Assert
        AssertNoErrors(diagnostics);
        Assert.Empty(generatedTrees);
    }

    [Fact(Skip = "Skip")]
    public void Generator_Should_Set_IsCommand_True_For_ICommand()
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
                public ValueTask<Response<TestResponse>> HandleAsync(
                    RequestContext<TestCommand> context,
                    CancellationToken cancellationToken = default)
                    => ValueTask.FromResult(Response.Success(new TestResponse()));
            }
            """;

        // Act
        var (_, diagnostics, generatedTrees) = RunGenerator(source);

        // Assert
        AssertNoErrors(diagnostics);

        var pipelineSource = GetGeneratedFile(generatedTrees, "Pipelines.g.cs");
        Assert.Contains("ITransactionPipe<", pipelineSource);
    }

    [Fact(Skip = "Skip")]
    public void Generator_Should_Set_IsCommand_False_For_IQuery()
    {
        // Arrange
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using Axent.Abstractions.Requests;
            using Axent.Abstractions.Models;
            using Axent.Abstractions.Services;

            namespace TestNamespace;

            internal sealed class TestQuery : IQuery<TestResponse> { }

            internal sealed class TestResponse { }

            internal sealed class TestQueryHandler : IRequestHandler<TestQuery, TestResponse>
            {
                public ValueTask<Response<TestResponse>> HandleAsync(
                    RequestContext<TestQuery> context,
                    CancellationToken cancellationToken = default)
                    => ValueTask.FromResult(Response.Success(new TestResponse()));
            }
            """;

        // Act
        var (_, diagnostics, generatedTrees) = RunGenerator(source);

        // Assert
        AssertNoErrors(diagnostics);

        var pipelineSource = GetGeneratedFile(generatedTrees, "Pipelines.g.cs");
        Assert.DoesNotContain("ITransactionPipe<", pipelineSource);
    }

    [Fact(Skip = "Skip")]
    public void Generator_Should_Set_IsCommand_False_For_Plain_IRequest()
    {
        // Arrange
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using Axent.Abstractions.Requests;
            using Axent.Abstractions.Models;
            using Axent.Abstractions.Services;

            namespace TestNamespace;

            internal sealed class TestRequest : IRequest<TestResponse> { }

            internal sealed class TestResponse { }

            internal sealed class TestRequestHandler : IRequestHandler<TestRequest, TestResponse>
            {
                public ValueTask<Response<TestResponse>> HandleAsync(
                    RequestContext<TestRequest> context,
                    CancellationToken cancellationToken = default)
                    => ValueTask.FromResult(Response.Success(new TestResponse()));
            }
            """;

        // Act
        var (_, diagnostics, generatedTrees) = RunGenerator(source);

        // Assert
        AssertNoErrors(diagnostics);

        var pipelineSource = GetGeneratedFile(generatedTrees, "Pipelines.g.cs");
        Assert.DoesNotContain("ITransactionPipe<", pipelineSource);
    }

    [Fact(Skip = "Skip")]
    public void Generator_Should_Generate_RequestModule_With_Correct_Request_Types()
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
                public ValueTask<Response<TestResponse>> HandleAsync(
                    RequestContext<TestCommand> context,
                    CancellationToken cancellationToken = default)
                    => ValueTask.FromResult(Response.Success(new TestResponse()));
            }
            """;

        // Act
        var (_, diagnostics, generatedTrees) = RunGenerator(source);

        // Assert
        AssertNoErrors(diagnostics);

        var requestModuleSource = GetGeneratedFile(generatedTrees, "RequestModule.g.cs");
        Assert.Contains("TestCommandPipeline", requestModuleSource);
        Assert.Contains("builder.Map<global::TestNamespace.TestCommand, global::TestNamespace.TestResponse>", requestModuleSource);
    }

    [Fact(Skip = "Skip")]
    public void Generator_Should_Generate_HandlerPipe_For_Each_Request()
    {
        // Arrange
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using Axent.Abstractions.Requests;
            using Axent.Abstractions.Models;
            using Axent.Abstractions.Services;

            namespace TestNamespace;

            internal sealed class FirstCommand : ICommand<FirstResponse> { }

            internal sealed class FirstResponse { }

            internal sealed class FirstCommandHandler : IRequestHandler<FirstCommand, FirstResponse>
            {
                public ValueTask<Response<FirstResponse>> HandleAsync(
                    RequestContext<FirstCommand> context,
                    CancellationToken cancellationToken = default)
                    => ValueTask.FromResult(Response.Success(new FirstResponse()));
            }

            internal sealed class SecondQuery : IQuery<SecondResponse> { }

            internal sealed class SecondResponse { }

            internal sealed class SecondQueryHandler : IRequestHandler<SecondQuery, SecondResponse>
            {
                public ValueTask<Response<SecondResponse>> HandleAsync(
                    RequestContext<SecondQuery> context,
                    CancellationToken cancellationToken = default)
                    => ValueTask.FromResult(Response.Success(new SecondResponse()));
            }
            """;

        // Act
        var (_, diagnostics, generatedTrees) = RunGenerator(source);

        // Assert
        AssertNoErrors(diagnostics);

        var handlerPipeSource = GetGeneratedFile(generatedTrees, "HandlerPipe.g.cs");
        Assert.Contains("FirstCommandHandlerPipe", handlerPipeSource);
        Assert.Contains("SecondQueryHandlerPipe", handlerPipeSource);
    }

    [Fact(Skip = "Skip")]
    public void Generator_Should_Generate_Registrar_With_Unique_Type_Name()
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
                public ValueTask<Response<TestResponse>> HandleAsync(
                    RequestContext<TestCommand> context,
                    CancellationToken cancellationToken = default)
                    => ValueTask.FromResult(Response.Success(new TestResponse()));
            }
            """;

        // Act
        var (_, diagnostics, generatedTrees) = RunGenerator(source);

        // Assert
        AssertNoErrors(diagnostics);

        var registrarSource = GetGeneratedFile(generatedTrees, "Registrar.g.cs");
        Assert.Contains("AxentGeneratedModuleRegistrarTestAssembly", registrarSource);
        Assert.Contains("[assembly: AxentModuleAttribute(typeof(Axent.Generated.AxentGeneratedModuleRegistrarTestAssembly))]", registrarSource);
        Assert.Contains("services.AddScoped<TestCommandHandlerPipe>();", registrarSource);
        Assert.Contains("services.AddScoped<TestCommandPipeline>();", registrarSource);
        Assert.Contains("services.AddScoped<IAxentRequestModule, AxentRequestModule>();", registrarSource);
    }

    [Fact(Skip = "Skip")]
    public void Generator_Should_Skip_Abstract_Classes()
    {
        // Arrange
        const string source = """
            using Axent.Abstractions.Requests;

            namespace TestNamespace;

            internal abstract class AbstractCommand : ICommand<TestResponse> { }

            internal sealed class TestResponse { }
            """;

        // Act
        var (_, diagnostics, generatedTrees) = RunGenerator(source);

        // Assert
        AssertNoErrors(diagnostics);
        Assert.Empty(generatedTrees);
    }

    [Fact(Skip = "Skip")]
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
                public ValueTask<Response<TestResponse>> HandleAsync(
                    RequestContext<TestCommand> context,
                    CancellationToken cancellationToken = default)
                    => ValueTask.FromResult(Response.Success(new TestResponse()));
            }
            """;

        // Act
        var (outputCompilation, diagnostics, _) = RunGenerator(source);

        // Assert
        AssertNoErrors(diagnostics);
        AssertNoErrors(outputCompilation.GetDiagnostics(TestContext.Current.CancellationToken));
    }
}
