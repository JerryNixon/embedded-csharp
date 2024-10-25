using embedded_csharp;

internal class Program
{
    private static void Main(string[] args)
    {
        var item = new item("1", "John Doe", new DateOnly(2000, 1, 2), true, "{ \"option\": true }");
        var claim = new claims(true, true, true, true);

        while (true)
        {
            Console.ForegroundColor = ConsoleColor.White;

            Console.WriteLine();

            var samples = new[]
            {
                "@item.Id == 1",
                "@item.Id == Math.Min(1, 2)",
                "@item.Name.Contains(\"J\")",
                "@item.Name.Substring(1, 1) == \"o\"",
                "Regex.IsMatch(@item.Name, \"j\", RegexOptions.IgnoreCase)",
                "@item.Name.Length > 5",
                "@item.Created.Year == 2000",
                "@claims.Read == true",
                "@claims.Read",
                "@claims.Read == @claims.Update",
                "Equals(@claims.Read, @claims.Update)",
                "@item.Id == 1 && @claims.Read",
                "(@claims.Read != @claims.Update) || @item.Admin",
                "new[] { @item.Admin, @claims.Read }.All(x => x)",
                "int.TryParse(@item.Id.ToString(), out int number) ? number == 1 : false",
                "JsonDocument.Parse(@item.Info).RootElement.GetProperty(\"option\").GetBoolean() == true"
            };

            foreach (var sample in samples)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Running sample: {sample}");
                Console.ForegroundColor = ConsoleColor.Gray;
                Run(sample);
            }

            var disallowedSamples = new[]
            {
                // Using reflection (should be disallowed)
                "@item.GetType().GetProperties().Length > 0",
                "typeof(int) != null",

                // Accessing environment information (should be disallowed)
                "Environment.OSVersion.ToString().Contains(\"Windows\")",
                "Environment.UserName == \"admin\"",

                // Using disallowed namespace or methods
                "File.Exists(\"C:\\test.txt\")",
                "Process.GetCurrentProcess().ProcessName",

                // Trying to access a property that doesn't exist
                "@item.NonExistentProperty == 1",

                // Incorrect syntax (should trigger a compilation error)
                "@item.Id ==",
                "Math.Max(",
                "Math.Max()",
                "new NonExistentType();"
            };

            foreach (var sample in disallowedSamples)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Running disallowed sample: {sample}");
                Console.ForegroundColor = ConsoleColor.Gray;
                Run(sample);
            }

            Console.ForegroundColor = ConsoleColor.White;

            Console.WriteLine();
            Console.WriteLine($"@{item}");
            Console.WriteLine($"@{claim}");

            Console.WriteLine();
            Console.WriteLine("Write your own predicate like: @item.Id == 1");
            Run(Console.ReadLine()!);

            Console.ReadLine();

        }

        void Run(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                Console.WriteLine("Invalid input. Please provide a valid predicate.");
            }

            var runner = new CustomCodeRunner();
            if (!runner.Compile(code!, out var errors))
            {
                Console.WriteLine(errors);
                return;
            }

            try
            {
                var result = runner.Execute(item.ToArray(), claim.ToArray());
                Console.WriteLine($"Execution result: {result}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Execution failed: {ex.Message}");
            }
        }
    }
}

record item(string Id, string Name, DateOnly Created, bool Admin, string Info)
{
    public (string, object)[] ToArray()
    {
        return
        [
            ("Id", Id),
            ("Name", Name),
            ("Created", Created),
            ("Admin", Admin),
            ("Info", Info)
        ];
    }
}

record claims(bool Create, bool Read, bool Update, bool Delete)
{
    public (string, object)[] ToArray()
    {
        return
        [
            ("Create", Create),
            ("Read", Read),
            ("Update", Update),
            ("Delete", Delete)
        ];
    }
}