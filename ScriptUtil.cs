using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

public class ScriptUtil
{
    public class ScriptGlobals
    {
        public dynamic item { get; }

        // Constructor to handle casting
        public ScriptGlobals(object value)
        {
            item = ParseToNativeType(value);
        }
    }

    public Exception? ScriptError { get; }

    private readonly ScriptRunner<bool>? _compiledScript;

    public ScriptUtil(string scriptCode)
    {
        try
        {
            // Prepare the script for compilation
            scriptCode = $"return {scriptCode.Trim()};";

            var scriptOptions = ScriptOptions.Default
                .AddReferences(typeof(Math).Assembly)
                .AddReferences(typeof(Microsoft.CSharp.RuntimeBinder.Binder).Assembly)
                .AddImports("System", "System.Math", "System.Text");

            // Compile the script once in the constructor
            var script = CSharpScript.Create<bool>(scriptCode, scriptOptions, typeof(ScriptGlobals));
            _compiledScript = script.CreateDelegate();
        }
        catch (Exception ex)
        {
            ScriptError = ex;
        }
    }

    public async Task<(bool, bool?, string?)> EvaluateAsync(object value)
    {
        // Return if there was an error during script compilation
        if (ScriptError != null)
        {
            return (false, null, ScriptError.Message);
        }

        try
        {
            var globals = new ScriptGlobals(value);

            var result = await _compiledScript!(globals); // Safe as we ensure _compiledScript is not null if ScriptError is null
            return (true, result, null);
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }

    public static object ParseToNativeType(object value)
    {
        var strValue = value.ToString();
        return strValue switch
        {
            _ when bool.TryParse(strValue, out bool boolResult) => boolResult,
            _ when byte.TryParse(strValue, out byte byteResult) => byteResult,
            _ when sbyte.TryParse(strValue, out sbyte sbyteResult) => sbyteResult,
            _ when short.TryParse(strValue, out short shortResult) => shortResult,
            _ when ushort.TryParse(strValue, out ushort ushortResult) => ushortResult,
            _ when int.TryParse(strValue, out int intResult) => intResult,
            _ when uint.TryParse(strValue, out uint uintResult) => uintResult,
            _ when long.TryParse(strValue, out long longResult) => longResult,
            _ when ulong.TryParse(strValue, out ulong ulongResult) => ulongResult,
            _ when float.TryParse(strValue, out float floatResult) => floatResult,
            _ when double.TryParse(strValue, out double doubleResult) => doubleResult,
            _ when decimal.TryParse(strValue, out decimal decimalResult) => decimalResult,
            _ when char.TryParse(strValue, out char charResult) => charResult,
            _ when DateTime.TryParse(strValue, out DateTime dateResult) => dateResult,
            _ when Guid.TryParse(strValue, out Guid guidResult) => guidResult,
            _ => value
        };
    }
}
