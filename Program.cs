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
			if (string.IsNullOrEmpty(value)) value = null;

			Console.WriteLine("What is the validation script? Example: item > 10");
			var scriptCode = Console.ReadLine()?.Trim();

			await RunScriptEvaluation(scriptCode!, value!);
		}
	}

	// Reusable method for running script evaluations
	private static async Task RunScriptEvaluation(string scriptCode, object value)
	{
		Console.WriteLine("Running...");
		var embeddedScript = new EmbeddedScript(scriptCode);
		var result = await embeddedScript.EvaluateAsync(value);
		Console.WriteLine(result.ToString());
	}
}
