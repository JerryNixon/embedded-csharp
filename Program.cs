using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

public class Program
{
    private static async Task Main(string[] args)
    {
        Console.WriteLine("What is the value of the field? Example: 123");
        var value = (object)123;

        Console.WriteLine("What is the validation script? Example: item > 10");
		var script = "(int)item > 10"; // Cast item to int

        Console.WriteLine("Running...");

        // Run script and get the result
        var (success, result, error) = await ExecuteScript(script, value);

        if (success)
        {
            Console.WriteLine($"Script executed successfully: {result}");
        }
        else
        {
            Console.WriteLine($"Script execution failed: {error}");
        }
		Console.ReadKey();
    }

    public class ScriptGlobals
    {
        public object item { get; set; }
    }

    public static async Task<(bool, bool?, string?)> ExecuteScript(string scriptCode, object value)
    {
        try
        {
            scriptCode = $"return {scriptCode.Trim()};";

            var scriptOptions = ScriptOptions.Default
                .AddReferences(typeof(Math).Assembly)
                .AddImports("System", "System.Math", "System.Text");

            // Use a global object to pass variables into the script
            var globals = new ScriptGlobals { item = value };

            // Evaluate the script
            var result = await CSharpScript.EvaluateAsync<bool>(scriptCode, scriptOptions, globals);

            return (true, result, null);
        }
        catch (CompilationErrorException ex)
        {
            return (false, null, string.Join(Environment.NewLine, ex.Diagnostics));
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }
}
