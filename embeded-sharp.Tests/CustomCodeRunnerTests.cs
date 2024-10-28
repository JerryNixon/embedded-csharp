using embedded_csharp;

public class CustomCodeRunnerTests
{
    private readonly CustomCodeRunner _runner;
    private readonly (string Name, object Value)[] _itemData;
    private readonly (string Name, object Value)[] _claimData;

    public CustomCodeRunnerTests()
    {
        _runner = new CustomCodeRunner();
        _itemData = new item("1", "John Doe", new DateOnly(2000, 1, 2), true, "{ \"option\": true }").ToArray();
        _claimData = new claims(true, true, true, true).ToArray();
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

    [Theory]
    [InlineData("@item.Id == 1")]
    [InlineData("@item.Id == Math.Min(1, 2)")]
    [InlineData("@item.Name.Contains(\"J\")")]
    [InlineData("Regex.IsMatch(@item.Name, \"j\", RegexOptions.IgnoreCase)")]
    [InlineData("@item.Created.Year == 2000")]
    [InlineData("JsonDocument.Parse(@item.Info).RootElement.GetProperty(\"option\").GetBoolean() == true")]
    public void ValidCode_ShouldEvaluateSuccessfully(string code)
    {
        Assert.True(_runner.Compile(code, out string? errors), errors);
        Assert.True(_runner.Execute(_itemData, _claimData));
    }

    [Theory]
    [InlineData("@item.NonExistentProperty == 1")]
    [InlineData("@claims.InvalidField")]
    public void ExecutionFailureDueToNonExistentProperty_ShouldFailAtExecution(string code)
    {
        Assert.True(_runner.Compile(code, out string? errors), errors);
        var exception = Assert.Throws<InvalidOperationException>(() => _runner.Execute(_itemData, _claimData));
        Assert.Contains("The property does not exist", exception.Message);
    }

    [Theory]
    [InlineData("@item.GetType().GetProperties().Length > 0")]
    [InlineData("typeof(int) != null")]
    public void ReflectionCode_ShouldFail(string code)
    {
        Assert.False(_runner.Compile(code, out string? errors));
        Assert.Contains("Reflection is not allowed", errors);
    }

    [Theory]
    [InlineData("Environment.OSVersion.ToString().Contains(\"Windows\")")]
    [InlineData("Environment.UserName == \"admin\"")]
    public void EnvironmentAccessCode_ShouldFail(string code)
    {
        Assert.False(_runner.Compile(code, out string? errors));
        Assert.Contains("Environmental types not allowed", errors);
    }

    [Theory]
    [InlineData("File.Exists(\"C:\\test.txt\")")]
    [InlineData("Process.GetCurrentProcess().ProcessName")]
    public void DisallowedNamespacesCode_ShouldFail(string code)
    {
        Assert.False(_runner.Compile(code, out string? errors), code);
        Assert.NotNull(errors);
        Assert.NotEmpty(errors);
    }

    [Theory]
    [InlineData("@item.Id ==")]
    [InlineData("Math.Max(")]
    [InlineData("new NonExistentType();")]
    public void SyntaxErrors_ShouldFail(string code)
    {
        Assert.False(_runner.Compile(code, out string? errors), code);
        Assert.NotNull(errors);
        Assert.NotEmpty(errors);
    }
}
