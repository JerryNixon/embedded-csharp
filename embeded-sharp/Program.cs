using embedded_csharp;

internal class Program
{
    private static void Main(string[] args)
    {
        var item = new item("1", "John Doe", new DateOnly(2000, 1, 2), true, "{ \"option\": true }");
        var claim = new claims(true, true, true, true);

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

        Console.ForegroundColor = ConsoleColor.Cyan;

        Console.WriteLine();
        Console.WriteLine($"@{item}");
        Console.WriteLine($"@{claim}");

        Console.ForegroundColor = ConsoleColor.Gray;

        Console.WriteLine();
        Console.WriteLine("Write your own predicate like: @item.Id == 1");
        Console.WriteLine("--------------------------------------------");

        while (true)
        {
            Console.ForegroundColor = ConsoleColor.White;

            Console.Write("/> ");
            var code = Console.ReadLine();

            if (string.IsNullOrEmpty(code))
            {
                Console.WriteLine("Samples:");
                foreach (var sample in samples)
                {
                    Console.WriteLine($"    {sample}");
                }
                continue;
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Run(code);

            Console.WriteLine();
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