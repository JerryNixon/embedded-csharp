using Basic.Reference.Assemblies;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using System.Dynamic;
using System.Reflection;
using System.Runtime.Loader;

namespace embedded_csharp;

public class CustomCodeRunner
{
    private MethodInfo? _compiledMethod;

    private readonly HashSet<string> _allowedNamespaces;

    public CustomCodeRunner(params string[] allowedNamespaces)
    {
        _allowedNamespaces =
        [
            "System",
            "System.Text",
            "System.Linq",
            "System.Text.Json",
            "System.Text.RegularExpressions",
            "System.Math"
        ];

        _allowedNamespaces.UnionWith(allowedNamespaces);
    }

    private bool ContainsInvalidNamespaces(SyntaxTree syntaxTree, out List<string> namespaces)
    {
        namespaces = [];

        var set = new HashSet<string>(_allowedNamespaces);
        var compilation = CSharpCompilation.Create(
            "test",
            [syntaxTree],
            Net80.References.All);

        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var nodes = syntaxTree
            .GetRoot()
            .DescendantNodes()
            .OfType<IdentifierNameSyntax>();

        foreach (var node in nodes)
        {
            var info = semanticModel.GetSymbolInfo(node);
            var ns = info.Symbol switch
            {
                ITypeSymbol ts => GetNamespace(ts),
                ILocalSymbol local => GetNamespace(local.Type),
                IMethodSymbol method => GetNamespace(method.ContainingType),
                INamespaceSymbol ns2 => ns2,
                _ => null,
            };

            if (ns is { IsGlobalNamespace: false })
            {
                var name = ns.ToString();
                if (!set.Contains(name))
                {
                    namespaces.Add(name);
                }
            }
        }

        return namespaces.Count > 0;

        INamespaceSymbol? GetNamespace(ITypeSymbol? type) => type?.ContainingNamespace;
    }

    public bool Compile(string snippet, out string? errorMessage)
    {
        errorMessage = null;

        if (string.IsNullOrEmpty(snippet))
        {
            errorMessage = "Snippet is empty.";
            return false;
        }

        var usings = string.Join(Environment.NewLine, _allowedNamespaces
            .Select(ns => ns == "System.Math" ? "using static System.Math;" : $"using {ns};"));

        var code = $$"""
            {{usings}}

            public class CustomCode
            {
                public static bool Run(dynamic item, dynamic claims) 
                {
                    return ({{snippet.Replace(";", string.Empty)}});
                }
            }
            """;

        var tree = CSharpSyntaxTree.ParseText(code);

        var diagnostics = tree.GetDiagnostics();
        if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
        {
            var errorMessages = string.Join(Environment.NewLine, diagnostics.Select(d => d.ToString()));
            errorMessage = $"Parsing errors: {errorMessages}";
            return false;
        }

        var root = tree.GetRoot();

        if (ContainsLoops(tree))
        {
            errorMessage = "Loops are not allowed.";
            return false;
        }

        if (UsesReflection(tree))
        {
            errorMessage = "Reflection is not allowed.";
            return false;
        }

        if (UsesDisallowedTypes(tree))
        {
            errorMessage = "Environmental types not allowed.";
            return false;
        }

        if (ContainsInvalidNamespaces(tree, out var ns))
        {
            errorMessage = $"Invalid namespaces: {string.Join(", ", ns)}";
            return false;
        }

        var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
        var compilation = CSharpCompilation.Create(
            assemblyName: Guid.NewGuid().ToString(),
            syntaxTrees: new[] { tree },
            references: Net80.References.All,
            options: options);

        using var stream = new MemoryStream();
        var emitResult = compilation.Emit(stream);

        if (!emitResult.Success)
        {
            var failures = emitResult.Diagnostics.Where(diagnostic =>
                diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error);

            var errorMessages = string.Join(Environment.NewLine, failures.Select(diagnostic => diagnostic.ToString()));
            errorMessage = $"Compilation errors: {errorMessages}";
            return false;
        }

        stream.Position = 0;
        var context = AssemblyLoadContext.Default;
        var assembly = context.LoadFromStream(stream);

        var type = assembly.GetType("CustomCode");
        if (type is null)
        {
            errorMessage = "Failed to load compiled class.";
            return false;
        }

        _compiledMethod = type.GetMethod("Run", BindingFlags.Static | BindingFlags.Public);
        if (_compiledMethod is null)
        {
            errorMessage = "Failed to retrieve Run method.";
            return false;
        }

        return true;

        bool ContainsLoops(SyntaxTree tree)
        {
            var root = tree.GetRoot();
            return root.DescendantNodes().Any(node =>
                node is WhileStatementSyntax ||
                node is ForStatementSyntax ||
                node is ForEachStatementSyntax);
        }

        bool UsesReflection(SyntaxTree tree)
        {
            return tree.GetRoot().DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .Any(invocation =>
                    invocation.Expression is MemberAccessExpressionSyntax memberAccess
                    && memberAccess.Name.Identifier.Text == "GetType")
                || tree.GetRoot().DescendantNodes().OfType<TypeOfExpressionSyntax>().Any();
        }


        bool UsesDisallowedTypes(SyntaxTree tree)
        {
            return tree.GetRoot().DescendantNodes()
               .OfType<IdentifierNameSyntax>()
               .Any(identifier => DisallowedTypes.Contains(identifier.Identifier.Text));
        }
    }

    public bool Execute((string Name, object Value)[]? items, (string Name, object Value)[]? claims)
    {
        if (_compiledMethod is null)
        {
            throw new InvalidOperationException("Not compiled.");
        }

        try
        {
            object?[]? parameters = [new Parameter(items).Argument, new Parameter(claims).Argument];
            var result = _compiledMethod.Invoke(null, parameters);

            return result is bool typed && typed;
        }
        catch (TargetInvocationException tie)
        {
            if (tie.InnerException is Microsoft.CSharp.RuntimeBinder.RuntimeBinderException binderEx)
            {
                if (binderEx.Message.Contains("does not contain a definition for"))
                {
                    var errorMessage = $"The property does not exist.";
                    throw new InvalidOperationException(errorMessage, binderEx);
                }

                if (binderEx.Message.Contains("Expando"))
                {
                    var errorMessage = $"Syntax is invalid.";
                    throw new InvalidOperationException(errorMessage, binderEx);
                }

                throw new InvalidOperationException($"{binderEx.Message}", binderEx);
            }

            if (tie.InnerException is not null)
            {
                throw new InvalidOperationException($"{tie.InnerException.Message}", tie.InnerException);
            }

            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Unexpected error: {ex.Message}", ex);
        }
    }

    HashSet<string> DisallowedTypes =
    [
        // Core System Types
        "Environment",
        "Console",
        "AppDomain",
        "GC",  // Garbage Collector

        // File System Access
        "File",
        "FileInfo",
        "Directory",
        "DirectoryInfo",
        "Path",
        "DriveInfo",

        // Process and Diagnostics
        "Process",
        "ProcessStartInfo",
        "EventLog",
        "EventLogEntry",
        "PerformanceCounter",
    
        // Registry Access
        "Registry",
        "RegistryKey",
    
        // Networking and Web
        "WebClient",
        "HttpClient",
        "WebRequest",
        "HttpRequest",
        "Dns",
        "Socket",
        "NetworkStream",
        "TcpClient",
        "TcpListener",
        "UdpClient",
        "NamedPipeClientStream",
        "NamedPipeServerStream",
    
        // Security and Cryptography
        "CryptoStream",
        "SymmetricAlgorithm",
        "AsymmetricAlgorithm",
        "RSA",
        "DSA",
        "SHA256",
        "SHA512",
        "SHA1",
        "MD5",
        "ProtectedData",
        "Aes",
    
        // Reflection and Dynamic Code Execution
        "Activator",
        "Assembly",
        "AssemblyBuilder",
        "AssemblyName",
        "FieldInfo",
        "MethodInfo",
        "PropertyInfo",
        "ConstructorInfo",
        "TypeInfo",
        "Type",
        "MethodBase",
        "DynamicMethod",
        "ILGenerator",
        "ParameterInfo",

        // Threading and Synchronization
        "Thread",
        "ThreadPool",
        "Task",
        "Timer",
        "CancellationToken",
        "Semaphore",
        "Mutex",
        "AutoResetEvent",
        "ManualResetEvent",
    
        // Unsafe and Unmanaged Code
        "GCHandle",
        "Marshal",
        "SafeHandle",
        "UnmanagedFunctionPointer",
        "DllImport",
        "StructLayout",
        "SecurityCriticalAttribute",
        "SecuritySafeCriticalAttribute",
    
        // Serialization
        "BinaryFormatter",
        "SoapFormatter",
        "Formatter",
        "DataContractSerializer",
        "XmlSerializer",
    
        // Unsafe Memory Management
        "Pointer",
        "Span",
        "Memory",
        "MemoryManager",
        "FixedBuffer",
    
        // PInvoke and Interop
        "DllImportAttribute",
        "UnmanagedType",
        "CallingConvention",
        "ComImportAttribute",
        "ExternAttribute",
    
        // System.Diagnostics (additional types)
        "StackTrace",
        "StackFrame",
        "Debug",
        "Trace",
        "Debugger",
        "DebuggerStepThroughAttribute",
        "ConditionalAttribute"
    ];
}

public class Parameter
{
    public dynamic Argument { get; }

    public Parameter((string Name, object Value)[]? items)
    {
        if (items is null)
        {
            Argument = new ExpandoObject();
            return;
        }

        Argument = new ExpandoObject();
        var argument = (IDictionary<string, object?>)Argument;
        foreach (var (name, value) in items)
        {
            argument.Add(name, ParseToNativeType(value!));
        }
    }

    public static object? ParseToNativeType(object value)
    {
        if (value is null)
        {
            return null;
        }

        return value switch
        {
            bool _ => value,
            byte _ => value,
            sbyte _ => value,
            short _ => value,
            ushort _ => value,
            int _ => value,
            uint _ => value,
            long _ => value,
            ulong _ => value,
            float _ => value,
            double _ => value,
            decimal _ => value,
            char _ => value,
            DateTime _ => value,
            Guid _ => value,
            _ => value.ToString() switch
            {
                _ when bool.TryParse(value.ToString(), out bool boolResult) => boolResult,
                _ when byte.TryParse(value.ToString(), out byte byteResult) => byteResult,
                _ when sbyte.TryParse(value.ToString(), out sbyte sbyteResult) => sbyteResult,
                _ when short.TryParse(value.ToString(), out short shortResult) => shortResult,
                _ when ushort.TryParse(value.ToString(), out ushort ushortResult) => ushortResult,
                _ when int.TryParse(value.ToString(), out int intResult) => intResult,
                _ when uint.TryParse(value.ToString(), out uint uintResult) => uintResult,
                _ when long.TryParse(value.ToString(), out long longResult) => longResult,
                _ when ulong.TryParse(value.ToString(), out ulong ulongResult) => ulongResult,
                _ when float.TryParse(value.ToString(), out float floatResult) => floatResult,
                _ when double.TryParse(value.ToString(), out double doubleResult) => doubleResult,
                _ when decimal.TryParse(value.ToString(), out decimal decimalResult) => decimalResult,
                _ when char.TryParse(value.ToString(), out char charResult) => charResult,
                _ when DateTime.TryParse(value.ToString(), out DateTime dateResult) => dateResult,
                _ when Guid.TryParse(value.ToString(), out Guid guidResult) => guidResult,
                _ => value
            }
        };
    }
}