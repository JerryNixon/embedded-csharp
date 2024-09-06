public class Program
{
	private static async Task Main(string[] args)
	{
		Console.WriteLine("With item 123");
		Console.WriteLine("With script 'item > 10'");

		await RunScriptEvaluation("item > 10", 123);

		while (true)
		{
			Console.WriteLine();
			Console.WriteLine("Now you try.");
			Console.WriteLine("---");
			Console.WriteLine();

			Console.WriteLine("What is the value of the field? Example: 123");
			var value = Console.ReadLine()?.Trim();

			Console.WriteLine("What is the validation script? Example: item > 10");
			var scriptCode = Console.ReadLine()?.Trim();

			await RunScriptEvaluation(scriptCode!, value!);
		}
	}

	// Reusable method for running script evaluations
	private static async Task RunScriptEvaluation(string scriptCode, object value)
	{
		Console.WriteLine("Running...");
		var scriptUtil = new ScriptUtil(scriptCode);

		var (success, result, error) = await scriptUtil.EvaluateAsync(value);
		Console.WriteLine($"Success: {success} Result: {result} Error: {error ?? "None"}");
	}
}
