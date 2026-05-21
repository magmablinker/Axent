#pragma warning disable RS2008

using System.Collections.Concurrent;
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
    private const string AxentAttributeMetadataName =
        "Axent.Abstractions.Attributes.AxentAttribute";

    private const string TemplateResourcePrefix =
        "Axent.Generators.Templates.";

    private static readonly DiagnosticDescriptor _templateMissingDiagnostic =
        new(
            "AXENT001",
            "Template missing",
            "Template '{0}' could not be found",
            "AxentSourceGenerator",
            DiagnosticSeverity.Error,
            true);

    private static readonly DiagnosticDescriptor _templateErrorDiagnostic =
        new(
            "AXENT002",
            "Template error",
            "Template '{0}' has errors: '{1}'",
            "AxentSourceGenerator",
            DiagnosticSeverity.Error,
            true);

    private static readonly ConcurrentDictionary<string, TemplateLoadResult> _templateCache = new();

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var requests =
            context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    fullyQualifiedMetadataName: AxentAttributeMetadataName,
                    predicate: static (node, _) => node is TypeDeclarationSyntax,
                    transform: static (ctx, ct) => GetRequestInfo(ctx, ct))
                .Where(static request => request is not null)
                .Select(static (request, _) => request!);

        context.RegisterSourceOutput(
            requests,
            static (ctx, request) => EmitRequest(ctx, request));

        var registrations = requests
            .Select(static (request, _) => new RequestRegistrationInfo(
                request.RequestFullName,
                request.GeneratedTypeName))
            .Collect()
            .Select(static (items, _) => new UniqueRegistrations(items)); 

        var assemblyName =
            context.CompilationProvider
                .Select(static (compilation, _) => compilation.AssemblyName ?? "Assembly");

        var registrarInput = registrations.Combine(assemblyName);

        context.RegisterSourceOutput(
            registrarInput,
            static (ctx, input) => EmitRegistrar(ctx, input.Left.Items, input.Right));
    }

    private static RequestTypeInfo? GetRequestInfo(
        GeneratorAttributeSyntaxContext ctx,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (ctx.TargetSymbol is not INamedTypeSymbol symbol)
        {
            return null;
        }

        if (symbol.IsAbstract || symbol.IsStatic)
        {
            return null;
        }

        if (symbol.TypeKind is not TypeKind.Class and not TypeKind.Struct)
        {
            return null;
        }

        var requestInterface = symbol.AllInterfaces
            .FirstOrDefault(static candidate => candidate.IsRequestFamilyInterface());

        if (requestInterface is null || requestInterface.TypeArguments.Length != 1)
        {
            return null;
        }

        var requestFullName =
            symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        var responseFullName =
            requestInterface.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        var isCommand =
            symbol.AllInterfaces.Any(static candidate => candidate.IsCommandInterface());

        var isCacheable =
            symbol.AllInterfaces.Any(static candidate => candidate.IsCacheableQueryInterface());

        var generatedTypeName =
            CreateGeneratedTypeName(symbol.Name, requestFullName);

        return new RequestTypeInfo(
            RequestFullName: requestFullName,
            ResponseFullName: responseFullName,
            SymbolName: symbol.Name,
            GeneratedTypeName: generatedTypeName,
            IsCommand: isCommand,
            IsCacheable: isCacheable);
    }

    private static void EmitRequest(
        SourceProductionContext ctx,
        RequestTypeInfo request)
    {
        ctx.CancellationToken.ThrowIfCancellationRequested();

        var handlerPipe = RenderTemplate(ctx, "HandlerPipe", new
        {
            Type = request
        });

        if (handlerPipe is not null)
        {
            ctx.AddSource(
                $"{request.GeneratedTypeName}.HandlerPipe.g.cs",
                SourceText.From(handlerPipe, Encoding.UTF8));
        }

        var pipeline = RenderTemplate(ctx, "Pipeline", new
        {
            Type = request
        });

        if (pipeline is not null)
        {
            ctx.AddSource(
                $"{request.GeneratedTypeName}.Pipeline.g.cs",
                SourceText.From(pipeline, Encoding.UTF8));
        }

        var requestModule = RenderTemplate(ctx, "RequestModule", new
        {
            Type = request
        });

        if (requestModule is not null)
        {
            ctx.AddSource(
                $"{request.GeneratedTypeName}.RequestModule.g.cs",
                SourceText.From(requestModule, Encoding.UTF8));
        }
    }

    private static void EmitRegistrar(
        SourceProductionContext ctx,
        ImmutableArray<RequestRegistrationInfo> registrations,
        string assemblyName)
    {
        ctx.CancellationToken.ThrowIfCancellationRequested();

        var uniqueRegistrations = registrations
            .GroupBy(static registration => registration.RequestFullName, StringComparer.Ordinal)
            .Select(static group => group.First())
            .OrderBy(static registration => registration.RequestFullName, StringComparer.Ordinal)
            .ToImmutableArray();

        if (uniqueRegistrations.Length == 0)
        {
            return;
        }

        var registrarTypeName = CreateRegistrarTypeName(assemblyName);

        var registrar = RenderTemplate(ctx, "Registrar", new
        {
            Types = uniqueRegistrations,
            RegistrarTypeName = registrarTypeName
        });

        if (registrar is null)
        {
            return;
        }

        ctx.AddSource(
            $"{registrarTypeName}.g.cs",
            SourceText.From(registrar, Encoding.UTF8));
    }

    private static string? RenderTemplate(
        SourceProductionContext ctx,
        string name,
        object model)
    {
        var result = GetTemplate(name);

        if (result.Missing)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                _templateMissingDiagnostic,
                Location.None,
                name));

            return null;
        }

        if (!string.IsNullOrWhiteSpace(result.Error))
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                _templateErrorDiagnostic,
                Location.None,
                name,
                result.Error));

            return null;
        }

        if (result.Template is null)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                _templateMissingDiagnostic,
                Location.None,
                name));

            return null;
        }

        var templateContext = new TemplateContext();
        var scriptObject = new ScriptObject();

        scriptObject.Import(model);
        templateContext.PushGlobal(scriptObject);

        return result.Template.Render(templateContext);
    }

    private static TemplateLoadResult GetTemplate(string name)
    {
        return _templateCache.GetOrAdd(name, static templateName =>
        {
            var resourceName = $"{TemplateResourcePrefix}{templateName}.sbntxt";

            using var stream = Assembly
                .GetExecutingAssembly()
                .GetManifestResourceStream(resourceName);

            if (stream is null)
            {
                return TemplateLoadResult.TemplateMissing;
            }

            using var reader = new StreamReader(stream);
            var template = Template.Parse(reader.ReadToEnd());

            if (!template.HasErrors)
            {
                return TemplateLoadResult.Success(template);
            }

            var errors = string.Join(
                ", ",
                template.Messages.Select(static message => message.ToString()));

            return TemplateLoadResult.Failure(errors);
        });
    }

    private static string CreateRegistrarTypeName(string assemblyName)
    {
        var builder = new StringBuilder("AxentGeneratedModuleRegistrar");

        foreach (var character in assemblyName.Where(char.IsLetterOrDigit))
        {
            builder.Append(character);
        }

        if (builder.Length == "AxentGeneratedModuleRegistrar".Length)
        {
            builder.Append("Assembly");
        }

        return builder.ToString();
    }

    private static string CreateGeneratedTypeName(
        string symbolName,
        string requestFullName)
    {
        return $"Axent{CreateIdentifierFragment(symbolName)}_{StableHash(requestFullName)}";
    }

    private static string CreateIdentifierFragment(string value)
    {
        var builder = new StringBuilder();

        foreach (var character in value.Where(char.IsLetterOrDigit))
        {
            builder.Append(character);
        }

        return builder.Length == 0
            ? "Request"
            : builder.ToString();
    }

    private static string StableHash(string value)
    {
        unchecked
        {
            const ulong offset = 14695981039346656037UL;
            const ulong prime = 1099511628211UL;

            var hash = offset;

            foreach (var character in value)
            {
                hash ^= character;
                hash *= prime;
            }

            return hash.ToString("X16");
        }
    }

    private sealed record TemplateLoadResult(
        Template? Template,
        bool Missing,
        string? Error)
    {
        public static TemplateLoadResult TemplateMissing { get; } =
            new(null, true, null);

        public static TemplateLoadResult Success(Template template) =>
            new(template, false, null);

        public static TemplateLoadResult Failure(string error) =>
            new(null, false, error);
    }
}
