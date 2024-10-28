# embedded-csharp

`embedded-csharp` enables developers to interactively test C# expressions with dynamic inputs in a live console. Using the Roslyn compiler, it compiles and evaluates simple predicates against custom `@item` and `@claim` objects, allowing quick validation of code for use in configurations like Data API builder, where custom logic and permissions might be applied.

### Getting Started

Launch the console, and start by entering a predicate. Here are some examples:

```csharp
@item.Id == 1
@item.Name.Contains("J")
@item.Created.Year == 2000
@claims.Read
Regex.IsMatch(@item.Name, "j", RegexOptions.IgnoreCase)
JsonDocument.Parse(@item.Info).RootElement.GetProperty("option").GetBoolean() == true
```

> 💡 Tip: Enter a blank snippet for additional examples.

When running the console, you’ll have access to two injected objects:
- `@item`: Represents a data entity with properties like `Id`, `Name`, `Created` (date), `Admin` (boolean), and `Info` (JSON string).
- `@claim`: Represents user permissions with properties `Create`, `Read`, `Update`, and `Delete` (all boolean).

Enter your custom predicate to validate it in real time. To access sample expressions, simply press Enter without input.

### Key Features

1. **Predicate Evaluation**: Designed for lightweight validation of simple C# predicates that return boolean values.
2. **Dynamic Parsing**: Injects `@item` and `@claim` into the code context, enabling dynamic property access.
3. **Namespace & Reflection Safety**: Restricts code to approved namespaces, forbidding access to reflection and environmental types to prevent unintended side effects. Only whitelisted methods, like `Regex.IsMatch` and `JsonDocument`, are permitted.
4. **Loop and Complexity Restriction**: Prevents the use of loops and complex constructs, keeping expressions concise and within the bounds of simple predicates.

### Example Usage

After compiling, the code runs against the provided `@item` and `@claim` properties. For instance:

```csharp
@item.Name.Substring(1, 1) == "o"        // Checks if the second character in Name is "o"
@claims.Read && @item.Admin             // Evaluates true if the user has read permissions and Admin status
```

### Requirements

This project restricts:
- **Reflection**: `GetType()` and `typeof()` are disabled to avoid runtime inspection.
- **Environmental Types**: Forbids unsafe system types like `Environment`, `File`, and `Process` to maintain code safety.
- **Namespace Whitelisting**: Limits namespaces to those needed for typical Data API logic (`System.Text.Json`, `System.Math`, etc.), blocking unauthorized libraries.

### Technical Details

- **Namespace Checking**: The `InspectNamespaces` method validates each symbol’s namespace against a predefined list, logging unauthorized usage to the console.
- **Error Handling**: If compilation or execution errors arise, detailed messages help identify invalid constructs.
- **Live Console Feedback**: Each expression entered is immediately compiled, checked, and executed, with results shown in the console.

`embedded-csharp` is ideal for testing custom validation scripts or permission checks in an interactive, real-time environment. This ensures that C# snippets intended for configuration files work as expected before deployment.