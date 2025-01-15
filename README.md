# Serilog.Enrichers.CallStack

A Serilog enricher that adds call stack information to log events, including method names, file names, and line numbers. This enricher helps with debugging and tracing by providing detailed context about where log events originated.

## Features

- **Method Names**: Capture the calling method name with optional parameter information
- **Type Names**: Include the declaring type name (class/struct) with optional namespace
- **File Information**: Add source file names with optional full paths
- **Line Numbers**: Include source code line numbers for precise location tracking
- **Column Numbers**: Optional column number information
- **Assembly Names**: Include assembly information when needed
- **Flexible Configuration**: Extensive configuration options for customization
- **Exception Handling**: Configurable exception handling to prevent logging failures
- **Frame Filtering**: Skip specific namespaces or types when walking the call stack
- **Frame Offset**: Choose which frame in the call stack to capture

## Installation

```bash
dotnet add package Serilog.Enrichers.CallStack
```

## Quick Start

### Basic Usage

```csharp
using Serilog;
using Serilog.Enrichers.CallStack;

var logger = new LoggerConfiguration()
    .Enrich.WithCallStack()
    .WriteTo.Console()
    .CreateLogger();

logger.Information("Hello, world!");
```

This will produce log output similar to:
```
[15:30:45 INF] Hello, world! {MethodName="Main", TypeName="Program", FileName="Program.cs", LineNumber=12}
```

### With Configuration

```csharp
var logger = new LoggerConfiguration()
    .Enrich.WithCallStack(config => config
        .WithIncludes(methodName: true, typeName: true, fileName: true, lineNumber: true)
        .WithFullNames(fullTypeName: true)
        .WithMethodParameters(includeParameters: true)
        .SkipNamespace("System")
        .SkipNamespace("Microsoft"))
    .WriteTo.Console()
    .CreateLogger();
```

## Configuration Options

### Include/Exclude Information

```csharp
var config = new CallStackEnricherConfiguration()
    .WithIncludes(
        methodName: true,      // Include method names
        typeName: true,        // Include type names
        fileName: true,        // Include file names
        lineNumber: true,      // Include line numbers
        columnNumber: false,   // Include column numbers
        assemblyName: false);  // Include assembly names
```

### Property Names

Customize the property names used in log events:

```csharp
var config = new CallStackEnricherConfiguration()
    .WithPropertyNames(
        methodName: "Method",
        typeName: "Class",
        fileName: "File",
        lineNumber: "Line",
        columnNumber: "Column",
        assemblyName: "Assembly");
```

### Full vs. Short Names

Control whether to use full names (with namespaces/paths) or short names:

```csharp
var config = new CallStackEnricherConfiguration()
    .WithFullNames(
        fullTypeName: true,        // Use "MyApp.Services.UserService" vs "UserService"
        fullFileName: true,        // Use full path vs just filename
        fullParameterTypes: true); // Use full type names in parameters
```

### Method Parameters

Include method parameter information in the method name:

```csharp
var config = new CallStackEnricherConfiguration()
    .WithMethodParameters(
        includeParameters: true,
        useFullParameterTypes: false);

// Results in: "ProcessUser(String name, Int32 id)" instead of just "ProcessUser"
```

### Skip Frames

Skip specific namespaces or types when walking the call stack:

```csharp
var config = new CallStackEnricherConfiguration()
    .SkipNamespace("System")
    .SkipNamespace("Microsoft")
    .SkipNamespace("Serilog")
    .SkipType("MyApp.Infrastructure.LoggingWrapper");
```

### Frame Offset

Choose which frame in the call stack to capture:

```csharp
var config = new CallStackEnricherConfiguration()
    .WithFrameOffset(1); // Skip 1 frame up the call stack
```

### Exception Handling

Configure how exceptions during enrichment are handled:

```csharp
var config = new CallStackEnricherConfiguration()
    .WithExceptionHandling(
        suppress: true,  // Don't throw exceptions
        onException: ex => Console.WriteLine($"Enricher error: {ex.Message}"));
```

## Advanced Configuration Examples

### Minimal Configuration

For production environments where you want minimal overhead:

```csharp
var config = new CallStackEnricherConfiguration()
    .WithIncludes(
        methodName: true,
        typeName: true,
        fileName: false,      // Skip file names to reduce overhead
        lineNumber: false,    // Skip line numbers
        columnNumber: false,
        assemblyName: false)
    .WithFullNames(fullTypeName: false); // Use short type names
```

### Development Configuration

For development environments where you want maximum detail:

```csharp
var config = new CallStackEnricherConfiguration()
    .WithIncludes(
        methodName: true,
        typeName: true,
        fileName: true,
        lineNumber: true,
        columnNumber: true,
        assemblyName: true)
    .WithFullNames(
        fullTypeName: true,
        fullFileName: true,
        fullParameterTypes: true)
    .WithMethodParameters(includeParameters: true)
    .WithExceptionHandling(suppress: false); // Throw exceptions for debugging
```

### Filtering Configuration

Skip common framework types and focus on application code:

```csharp
var config = new CallStackEnricherConfiguration()
    .SkipNamespace("System")
    .SkipNamespace("Microsoft")
    .SkipNamespace("Serilog")
    .SkipNamespace("Newtonsoft")
    .SkipType("MyApp.Infrastructure.LoggingService")
    .WithFrameOffset(0);
```

## Integration with Different Sinks

### Console Output

```csharp
var logger = new LoggerConfiguration()
    .Enrich.WithCallStack()
    .WriteTo.Console(outputTemplate: 
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} " +
        "({TypeName}.{MethodName} in {FileName}:{LineNumber}){NewLine}{Exception}")
    .CreateLogger();
```

### JSON Output (for structured logging)

```csharp
var logger = new LoggerConfiguration()
    .Enrich.WithCallStack()
    .WriteTo.File(new JsonFormatter(), "log.json")
    .CreateLogger();
```

### Seq Integration

```csharp
var logger = new LoggerConfiguration()
    .Enrich.WithCallStack()
    .WriteTo.Seq("http://localhost:5341")
    .CreateLogger();
```

## Performance Considerations

- **Debug vs Release**: Call stack information is more accurate in Debug builds
- **File/Line Info**: Including file names and line numbers requires debug symbols
- **Method Parameters**: Including parameter information adds overhead
- **Frame Skipping**: Use skip configurations to avoid walking unnecessary frames
- **Exception Handling**: Enable exception suppression in production

## Debug Symbols

For file names and line numbers to work properly, ensure your application is built with debug symbols:

```xml
<PropertyGroup>
  <DebugType>portable</DebugType>
  <DebugSymbols>true</DebugSymbols>
</PropertyGroup>
```

## Example Output

With full configuration, log events will include rich call stack information:

```json
{
  "@t": "2024-01-15T15:30:45.123Z",
  "@l": "Information",
  "@m": "Processing user request",
  "MethodName": "ProcessRequest(String userId, UserRequest request)",
  "TypeName": "MyApp.Services.UserService",
  "FileName": "UserService.cs",
  "LineNumber": 45,
  "ColumnNumber": 12,
  "AssemblyName": "MyApp.Services"
}
```

## Best Practices

1. **Use appropriate configuration for your environment**: Detailed information for development, minimal for production
2. **Skip framework namespaces**: Focus on your application code by skipping system namespaces
3. **Consider performance impact**: Call stack walking has overhead, especially with full configuration
4. **Enable exception suppression in production**: Prevent logging failures from breaking your application
5. **Use structured logging sinks**: JSON-based sinks work best with the additional properties

## Compatibility

- **.NET Standard 2.0+**: Compatible with .NET Framework 4.6.1+, .NET Core 2.0+, .NET 5+
- **Serilog 3.0+**: Requires Serilog version 3.0 or higher

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## Changelog

### Version 1.0.0
- Initial release
- Core call stack enrichment functionality
- Comprehensive configuration options
- Full test coverage
- Documentation and examples