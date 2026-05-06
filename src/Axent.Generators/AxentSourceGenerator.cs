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
    private const string PipelinesFile = "Pipelines.g.cs";
    private const string HandlerPipeFile = "HandlerPipe.g.cs";
    private const string RequestModuleFile = "RequestModule.g.cs";
    private const string RegistrarFile = "Registrar.g.cs";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var requestTypes =
            context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (node, _) =>
                        node is ClassDeclarationSyntax or RecordDeclarationSyntax,
                    transform: static (ctx, ct) => GetRequestInfo(ctx, ct))
                .Where(static info => info is not null)
                .Collect();

        var assemblyName = context.CompilationProvider
            .Select(static (compilation, _) => compilation.AssemblyName ?? "Assembly");

        var input = requestTypes.Combine(assemblyName);

        context.RegisterSourceOutput(
            input,
            static (spc, source) => Execute(spc, source.Left, source.Right));
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

        var requestInterface = symbol.AllInterfaces
            .FirstOrDefault(static i => i.IsRequestFamilyInterface());

        if (requestInterface is null || requestInterface.TypeArguments.Length != 1)
        {
            return null;
        }

        var responseType = requestInterface.TypeArguments[0];

        var isCommand = symbol.AllInterfaces.Any(static i => i.IsCommandInterface());

        var isCacheable = symbol.AllInterfaces.Any(static i => i.IsCacheableQueryInterface());

        return new RequestTypeInfo(
            RequestFullName: symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            ResponseFullName: responseType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            SymbolName: symbol.Name,
            IsCommand: isCommand,
            IsCacheable: isCacheable);
    }

    private static void Execute(
        SourceProductionContext ctx,
        ImmutableArray<RequestTypeInfo?> types,
        string assemblyName)
    {
        var requests = types
            .OfType<RequestTypeInfo>()
            .GroupBy(t => t.RequestFullName)
            .Select(g => g.First())
            .OrderBy(t => t.RequestFullName)
            .ToImmutableArray();

        if (requests.Length == 0)
        {
            return;
        }

        var registrarTypeName = CreateRegistrarTypeName(assemblyName);

        ctx.AddSource(PipelinesFile,
            SourceText.From(RenderTemplate(requests, registrarTypeName, GetTemplate("Pipeline", ctx)), Encoding.UTF8));

        ctx.AddSource(HandlerPipeFile,
            SourceText.From(RenderTemplate(requests, registrarTypeName, GetTemplate("HandlerPipe", ctx)), Encoding.UTF8));

        ctx.AddSource(RequestModuleFile,
            SourceText.From(RenderTemplate(requests, registrarTypeName, GetTemplate("RequestModule", ctx)), Encoding.UTF8));

        ctx.AddSource(RegistrarFile,
            SourceText.From(RenderTemplate(requests, registrarTypeName, GetTemplate("Registrar", ctx)), Encoding.UTF8));
    }

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
                true);

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
            "Template error",
            "Template '{0}' has errors: '{1}'",
            "AxentSourceGenerator",
            DiagnosticSeverity.Error,
            true);

        ctx.ReportDiagnostic(Diagnostic.Create(
            templateError,
            Location.None,
            name,
            string.Join(", ", template.Messages.ToList())));

        return null;
    }

    private static string RenderTemplate(
        ImmutableArray<RequestTypeInfo> types,
        string registrarTypeName,
        Template? template)
    {
        if (template is null)
        {
            return string.Empty;
        }

        var context = new TemplateContext();
        var scriptObject = new ScriptObject();

        scriptObject.Import(new
        {
            Types = types,
            RegistrarTypeName = registrarTypeName
        });

        context.PushGlobal(scriptObject);

        return template.Render(context);
    }

    private static string CreateRegistrarTypeName(string assemblyName)
    {
        var builder = new StringBuilder("AxentGeneratedModuleRegistrar");

        foreach (var character in assemblyName)
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
            }
        }

        if (builder.Length == "AxentGeneratedModuleRegistrar".Length)
        {
            builder.Append("Assembly");
        }

        return builder.ToString();
    }
}
