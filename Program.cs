using embedded_csharp;

internal class Program
{
    private static void Main(string[] args)
    {
        var item = new item("1", "John Doe", new DateOnly(2000, 1, 2), true);
        var claim = new claims(true, true, true, true);

        Console.WriteLine($"@{item}");
        Console.WriteLine($"@{claim}");

        while (true)
        {
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("Write a predicate like:");
            Console.WriteLine("   @item.Id == 1");
            Console.WriteLine("   @item.Name.Contains('J')");
            Console.WriteLine("   @item.Name.Length > 5");
            Console.WriteLine("   @item.Created.Year == 2000");
            Console.WriteLine("   @claims.Read == true");
            Console.WriteLine("   @claims.Read");
            Console.WriteLine("   @claims.Read == @claims.Update");
            Console.WriteLine("   Equals(@claims.Read, @claims.Update)");
            Console.WriteLine("   @item.Id == 1 && !@claims.Read");
            Console.WriteLine("   (@claims.Read != @claims.Update) || @item.Admin");
            Console.WriteLine("   new[] { @item.Admin, @claims.Read }.All(x => x)");
            Console.ForegroundColor = ConsoleColor.Yellow;

            var code = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(code))
            {
                Console.WriteLine("Invalid input. Please provide a valid predicate.");
            }

            var runner = new CustomCodeRunner();
            if (!runner.Compile(code!, out var errors))
            {
                Console.WriteLine(errors);
                continue;
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

record item(string Id, string Name, DateOnly Created, bool Admin)
{
    public (string, object)[] ToArray()
    {
        return
        [
            ("Id", Id),
            ("Name", Name),
            ("Created", Created),
            ("Admin", Admin)
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