using System;
using System.Linq;
using System.Threading.Tasks;

public class Program
{
    // Store the row as a field
    private static readonly (string Name, object Value)[] sampleRow = new (string Name, object Value)[]
    {
        ("id", 123),
        ("name", "John Doe"),
        ("is_active", true),
        ("created_date", new DateTime(2023, 9, 6)),
        ("price", 19.99m),
        ("last_login", null!)  // Column with null value
    };

    private static async Task Main(string[] args)
    {
        // Output the default row so the user can see the structure and values
        OutputSampleRow();

        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("Now enter the validation script. Example: row.id > 10");
            var scriptCode = Console.ReadLine()?.Trim();

            await RunScriptEvaluation(scriptCode!);
        }
    }

    // Method to dynamically output the sample row to the user
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
        Console.WriteLine("You can reference these columns in your script using row[\"column-name\"]");
        Console.WriteLine("For example: row[\"id\"] > 100 or row[\"is_active\"] == true or row[\"last_login\"] == null");
        Console.WriteLine();
    }

    // Reusable method for running script evaluations
    private static async Task RunScriptEvaluation(string scriptCode)
    {
        Console.WriteLine("Running...");
        var embeddedScript = new EmbeddedCSharp(scriptCode);

        // Evaluate the script with the sample row
        var result = await embeddedScript.EvaluateAsync(sampleRow);
        Console.WriteLine(result.ToString());
    }
}
