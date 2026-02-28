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

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var requestTypes =
            context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (node, _) =>
                        node is (ClassDeclarationSyntax or RecordDeclarationSyntax)
                            and TypeDeclarationSyntax { BaseList.Types.Count: > 0 },
                    transform: static (ctx, ct) => GetRequestInfo(ctx, ct))
                .Where(static info => info is not null)
                .Collect();

        context.RegisterSourceOutput(
            requestTypes,
            static (spc, types) => Execute(spc, types));
    }

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

        // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
        foreach (var @interface in symbol.AllInterfaces)
        {
            if (!@interface.IsRequestInterface()) continue;

            var responseType = @interface.TypeArguments[0];

            return new (
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
        var requests = types.OfType<RequestTypeInfo>()
                .OrderBy(t => t.RequestFullName)
                .ToImmutableArray();

        if (requests.Length == 0) return;

        ctx.AddSource(SenderFile,
            SourceText.From(BuildSenderSource(requests, ctx), Encoding.UTF8));

        ctx.AddSource(PipelinesFile,
            SourceText.From(BuildPipelinesSource(requests, ctx), Encoding.UTF8));

        ctx.AddSource(HandlerPipeFile,
            SourceText.From(BuildHandlerPipeSource(requests, ctx), Encoding.UTF8));

        ctx.AddSource(
            AxentModuleInitializerFile,
            SourceText.From(BuildModuleInitializerSource(requests, ctx), Encoding.UTF8));
    }

    private static string BuildSenderSource(ImmutableArray<RequestTypeInfo> types, SourceProductionContext ctx)
        => RenderTemplate(types, GetTemplate("Sender", ctx));

    private static string BuildPipelinesSource(ImmutableArray<RequestTypeInfo> types, SourceProductionContext ctx)
        => RenderTemplate(types, GetTemplate("Pipeline", ctx));

    private static string BuildHandlerPipeSource(ImmutableArray<RequestTypeInfo> types, SourceProductionContext ctx)
        => RenderTemplate(types, GetTemplate("HandlerPipe", ctx));

    private static string BuildModuleInitializerSource(ImmutableArray<RequestTypeInfo> types, SourceProductionContext ctx)
        => RenderTemplate(types, GetTemplate("ModuleInitializer", ctx));

    private static Template? GetTemplate(string name, SourceProductionContext ctx)
    {
        using var stream = Assembly
            .GetExecutingAssembly()
            .GetManifestResourceStream($"Axent.Generators.Templates.{name}.sbntxt");

        if (stream is null)
        {
            var templateMissing = new DiagnosticDescriptor(
                "AXENT001",
                "Template missing",
                "Template '{0}' could not be found",
                "AxentSourceGenerator",
                DiagnosticSeverity.Error,
                true
            );
            ctx.ReportDiagnostic(Diagnostic.Create(templateMissing, Location.None, name));
            return null;
        }

        using var reader = new StreamReader(stream);
        var template = Template.Parse(reader.ReadToEnd());
        if (!template.HasErrors)
        {
            return template;
        }

        var templateError = new DiagnosticDescriptor(
            "AXENT002",
            "Template missing",
            "Template '{0}' has errors: '{1}'",
            "AxentSourceGenerator",
            DiagnosticSeverity.Error,
            true
        );
        ctx.ReportDiagnostic(Diagnostic.Create(templateError, Location.None, name, string.Join(", ", template.Messages.ToList())));

        return null;
    }

    private static string RenderTemplate(ImmutableArray<RequestTypeInfo> types, Template? template)
    {
        if (template is null) return string.Empty;

        var context = new TemplateContext();
        var scriptObject = new ScriptObject();
        scriptObject.Import(new { Types = types });
        context.PushGlobal(scriptObject);
        return template.Render(context);
    }
}
