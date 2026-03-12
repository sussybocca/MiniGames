using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System.Reflection;

namespace MiniGames.Services;

public class TrippyCompilerService
{
    private readonly HttpClient _http;

    public TrippyCompilerService(HttpClient http)
    {
        _http = http;
    }

    public async Task<Assembly?> CompileToAssembly(string sourceCode, string assemblyName = "DynamicApp")
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode,
            CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp11));

        var references = new List<MetadataReference>();

        // Core assemblies
        var assemblyNames = new[]
        {
            "mscorlib", "System", "System.Core", "System.Linq",
            "System.Runtime", "System.Private.CoreLib", "System.Collections",
            "System.Console", "netstandard"
        };

        foreach (var name in assemblyNames)
        {
            try
            {
                var assemblyUrl = $"./_framework/{name}.dll";
                var assemblyBytes = await _http.GetByteArrayAsync(assemblyUrl);
                references.Add(MetadataReference.CreateFromImage(assemblyBytes));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load assembly {name}: {ex.Message}");
            }
        }

        // Add main app assembly (contains AppBase, ICanvas, etc.)
        try
        {
            var mainAssemblyBytes = await _http.GetByteArrayAsync("./_framework/MiniGames.dll");
            references.Add(MetadataReference.CreateFromImage(mainAssemblyBytes));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load main assembly: {ex.Message}");
        }

        var compilation = CSharpCompilation.Create(
            assemblyName,
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                optimizationLevel: OptimizationLevel.Debug)
        );

        using var ms = new MemoryStream();
        EmitResult result = compilation.Emit(ms);

        if (!result.Success)
        {
            var errors = result.Diagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .Select(d => d.GetMessage());
            Console.WriteLine("Compilation failed: " + string.Join("\n", errors));
            return null;
        }

        ms.Seek(0, SeekOrigin.Begin);
        return Assembly.Load(ms.ToArray());
    }
}