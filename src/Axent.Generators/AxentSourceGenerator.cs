using System.Collections.Immutable;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Scriban;
using Scriban.Runtime;

namespace Axent.Generators;

[Generator]
public sealed class AxentSourceGenerator : IIncrementalGenerator
{
    private const string AxentModuleInitializerFile = "AxentModuleInitializer.g.cs";
    private const string SenderFile = "Sender.g.cs";
    private const string PipelinesFile = "Pipelines.g.cs";
    private const string HandlerPipeFile = "HandlerPipe.g.cs";

    private const string RequestMetadataName = "Axent.Abstractions.IRequest`1";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var requestTypes =
            context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (node, _) => IsCandidate(node),
                    transform: static (ctx, ct) => GetRequestInfo(ctx, ct))
                .Where(static info => info is not null)
                .WithComparer(RequestTypeInfo.Comparer)
                .Collect();

        context.RegisterSourceOutput(
            requestTypes,
            static (spc, types) => Execute(spc, types));
    }

    private static bool IsCandidate(SyntaxNode node)
        => node is (ClassDeclarationSyntax or RecordDeclarationSyntax) and TypeDeclarationSyntax { BaseList.Types.Count: > 0 };

    private static RequestTypeInfo? GetRequestInfo(
        GeneratorSyntaxContext ctx,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (ctx.SemanticModel.GetDeclaredSymbol(ctx.Node, ct) is not INamedTypeSymbol symbol
            || symbol.IsAbstract
            || symbol.IsStatic)
        {
            return null;
        }

        var requestInterface =
            ctx.SemanticModel.Compilation.GetTypeByMetadataName(RequestMetadataName);

        if (requestInterface is null)
        {
            return null;
        }

        foreach (var iface in symbol.AllInterfaces)
        {
            if (!SymbolEqualityComparer.Default.Equals(
                    iface.OriginalDefinition,
                    requestInterface))
            {
                continue;
            }

            var responseType = iface.TypeArguments[0];

            return new RequestTypeInfo(
                RequestFullName:
                    symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                ResponseFullName:
                    responseType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                SymbolName: symbol.Name);
        }

        return null;
    }

    private static void Execute(
        SourceProductionContext ctx,
        ImmutableArray<RequestTypeInfo?> types)
    {
        var requests =
            types
                .Where(static t => t is not null)
                .Select(static t => t!)
                .OrderBy(static t => t.RequestFullName)
                .ToImmutableArray();

        if (requests.Length == 0)
        {
            return;
        }

        ctx.AddSource(SenderFile,
            SourceText.From(BuildSenderSource(requests), Encoding.UTF8));

        ctx.AddSource(PipelinesFile,
            SourceText.From(BuildPipelinesSource(requests), Encoding.UTF8));

        ctx.AddSource(HandlerPipeFile,
            SourceText.From(BuildHandlerPipeSource(requests), Encoding.UTF8));

        ctx.AddSource(
            AxentModuleInitializerFile,
            SourceText.From(BuildModuleInitializerSource(requests), Encoding.UTF8));
    }

    private static string BuildSenderSource(ImmutableArray<RequestTypeInfo> types)
    {
        var template = GetTemplate("Sender");
        return RenderTemplate(types, template);
    }

    private static string BuildPipelinesSource(ImmutableArray<RequestTypeInfo> types)
    {
        var template = GetTemplate("Pipeline");
        return RenderTemplate(types, template);
    }

    private static string BuildHandlerPipeSource(ImmutableArray<RequestTypeInfo> types)
    {
        var template = GetTemplate("HandlerPipe");
        return RenderTemplate(types, template);
    }

    private static string BuildModuleInitializerSource(ImmutableArray<RequestTypeInfo> types)
    {
        var template = GetTemplate("ModuleInitializer");
        return RenderTemplate(types, template);
    }

    private static Template GetTemplate(string name)
    {
        using var stream = Assembly
            .GetExecutingAssembly()
            .GetManifestResourceStream(
                $"Axent.Generators.Templates.{name}.sbntxt"
            );

        if (stream is null)
        {
            throw new InvalidOperationException($"Template '{name}' could not be found");
        }

        using var reader = new StreamReader(stream);
        return Template.Parse(reader.ReadToEnd());
    }

    private static string RenderTemplate(ImmutableArray<RequestTypeInfo> types, Template template)
    {
        var context = new TemplateContext();
        var scriptObject = new ScriptObject();
        scriptObject.Import(new { Types = types });
        context.PushGlobal(scriptObject);

        return template.Render(context);
    }
}

internal sealed record RequestTypeInfo(
    string RequestFullName,
    string ResponseFullName,
    string SymbolName)
{
    public static readonly IEqualityComparer<RequestTypeInfo?> Comparer =
        EqualityComparer<RequestTypeInfo?>.Default;
}
