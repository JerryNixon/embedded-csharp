public class Program
{
    private static readonly (string Name, object Value)[] sampleRow = new (string Name, object Value)[]
    {
        ("id", 123),
        ("name", "John Doe"),
        ("is_active", true),
        ("created_date", new DateTime(2023, 9, 6)),
        ("price", 19.99m),
        ("last_login", null!)
    };

    private static async Task Main(string[] args)
    {
        OutputSampleRow();

        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("Now enter the validation script. Example: @item.id > 10");
            var scriptCode = Console.ReadLine()?.Trim();

            await RunScriptEvaluation(scriptCode!);
        }
    }

    private static void OutputSampleRow()
    {
        Console.WriteLine("Sample SQL Table Row:");
        Console.WriteLine("------------------------------");
        Console.WriteLine($"{"Column Name",-15} {"Type",-15} {"Value",-15}");
        Console.WriteLine("------------------------------");

        foreach (var (name, value) in sampleRow)
        {
            string type = value?.GetType().Name ?? "null"; // Get the type or show "null"
            string displayValue = value?.ToString() ?? "null"; // Display "null" if the value is null
            Console.WriteLine($"{name,-15} {type,-15} {displayValue,-15}");
        }

        Console.WriteLine();
        Console.WriteLine("You can reference these columns in your script using @item.<column-name>");
        Console.WriteLine("For example: @item.id > 100 or @item.is_active == true or @item.last_login == null");
        Console.WriteLine();
    }

    private static async Task RunScriptEvaluation(string scriptCode)
    {
        Console.WriteLine("Running...");
        var embeddedScript = new EmbeddedCSharp();
        var result = await embeddedScript.EvaluateAsync(scriptCode, sampleRow);
        Console.WriteLine(result.ToString());
    }
}
