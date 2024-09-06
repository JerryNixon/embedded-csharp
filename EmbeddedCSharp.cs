using System.Dynamic;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

public class EmbeddedCSharp
{
    public class ScriptGlobals
    {
        public dynamic item { get; }

        public ScriptGlobals((string Name, object Value)[] values)
        {
            item = new ExpandoObject();
            foreach (var (name, value) in values)
            {
                ((IDictionary<string, object>)item).Add(name, ParseToNativeType(value));
            }
        }
    }

    public Exception? ScriptError { get; private set; }
    private ScriptRunner<bool>? _compiledScript;

    public async Task<Return> EvaluateAsync(string scriptCode, params (string Name, object Value)[] values)
    {
        if (_compiledScript == null)
        {
            try
            {
                scriptCode = $"return {scriptCode.Trim()};";

                var scriptOptions = ScriptOptions.Default
                    .AddReferences(typeof(Math).Assembly)
                    .AddReferences(typeof(Microsoft.CSharp.RuntimeBinder.Binder).Assembly)
                    .AddImports("System", "System.Math", "System.Text");

                var script = CSharpScript.Create<bool>(scriptCode, scriptOptions, typeof(ScriptGlobals));
                _compiledScript = script.CreateDelegate();
            }
            catch (Exception ex)
            {
                ScriptError = ex;
                return new Return(Error: ex.Message);
            }
        }

        return await EvaluateScript(values);
    }

    private async Task<Return> EvaluateScript(params (string Name, object Value)[] values)
    {
        if (ScriptError != null)
        {
            return new Return(Error: ScriptError.Message);
        }

        try
        {
            var globals = new ScriptGlobals(values);
            var result = await _compiledScript!(globals);
            return new Return(Success: true, Valid: result);
        }
        catch (Exception ex)
        {
            return new Return(Error: ex.Message);
        }
    }

    public static object? ParseToNativeType(object value)
    {
        if (value == null)
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

    public record Return(bool Success = false, bool Valid = false, string? Error = null);
}
