# Serilog.Enrichers.CallStack

A Serilog enricher that adds call stack information to log events in an exception-like format. This enricher helps with debugging and tracing by providing detailed context about where log events originated, displaying the call stack in an intuitive format similar to exception stack traces.

## Features

- **Exception-like Format**: Display call stack in familiar format: `Method:Line --> Method:Line --> Method:Line`
- **Single Property**: Consolidates call stack into one `CallStack` property for cleaner logs
- **Configurable Depth**: Control the number of frames to include (default: 5)
- **Method Parameters**: Optional parameter information in method names
- **Type Information**: Include declaring type names with optional namespace
- **Line Numbers**: Precise source code line numbers for exact location tracking
- **Backward Compatibility**: Legacy format with individual properties still available
- **Frame Filtering**: Skip specific namespaces or types when walking the call stack
- **Exception Handling**: Configurable exception handling to prevent logging failures
- **Flexible Configuration**: Extensive configuration options for customization

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
[15:30:45 INF] Hello, world! {CallStack="Program.Main:12 --> Program.<Main>$:8"}
```

### Exception-like Format (Default)

```csharp
var logger = new LoggerConfiguration()
    .Enrich.WithCallStack(config => config
        .WithCallStackFormat(useExceptionLikeFormat: true, maxFrames: 3)
        .WithMethodParameters(includeParameters: true)
        .WithFullNames(fullTypeName: false)
        .SkipNamespace("System")
        .SkipNamespace("Microsoft"))
    .WriteTo.Console(outputTemplate: 
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} | {CallStack}{NewLine}{Exception}")
    .CreateLogger();
```

### Legacy Format

```csharp
var logger = new LoggerConfiguration()
    .Enrich.WithCallStack(config => config
        .WithCallStackFormat(useExceptionLikeFormat: false) // Use individual properties
        .WithIncludes(methodName: true, typeName: true, fileName: true, lineNumber: true)
        .WithFullNames(fullTypeName: true)
        .WithMethodParameters(includeParameters: true))
    .WriteTo.Console()
    .CreateLogger();
```

## Configuration Options

### Call Stack Format

Choose between the new exception-like format or legacy individual properties:

```csharp
var config = new CallStackEnricherConfiguration()
    .WithCallStackFormat(
        useExceptionLikeFormat: true,    // Default: true
        maxFrames: 5,                    // Default: 5, -1 for unlimited
        callStackPropertyName: "CallStack"); // Default: "CallStack"
```

**Exception-like Format Output:**
```
CallStack: "UserService.ProcessUser:45 --> UserController.CreateUser:23 --> Program.Main:12"
```

**Legacy Format Output:**
```
MethodName: "ProcessUser", TypeName: "UserService", FileName: "UserService.cs", LineNumber: 45
```

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

### Console Output with Exception-like Format

```csharp
var logger = new LoggerConfiguration()
    .Enrich.WithCallStack()
    .WriteTo.Console(outputTemplate: 
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} | Call Stack: {CallStack}{NewLine}{Exception}")
    .CreateLogger();
```

### Console Output with Legacy Format

```csharp
var logger = new LoggerConfiguration()
    .Enrich.WithCallStack(config => config.WithCallStackFormat(useExceptionLikeFormat: false))
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

### Exception-like Format (Default)

With the new exception-like format, log events include a single CallStack property:

```json
{
  "@t": "2025-07-29T00:30:45.123Z",
  "@l": "Information",
  "@m": "Processing user request",
  "CallStack": "UserService.ProcessRequest(String userId, UserRequest request):45 --> UserController.CreateUser:23 --> Program.Main:12"
}
```

### Legacy Format

When using legacy format (`useExceptionLikeFormat: false`), individual properties are included:

```json
{
  "@t": "2025-07-29T00:30:45.123Z",
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

1. **Use exception-like format for readability**: The default format provides intuitive call stack traces
2. **Limit frame depth in production**: Use `maxFrames` to control overhead (default: 5 frames)
3. **Skip framework namespaces**: Focus on your application code by skipping system namespaces
4. **Consider performance impact**: Call stack walking has overhead, tune `maxFrames` accordingly
5. **Enable exception suppression in production**: Prevent logging failures from breaking your application
6. **Use structured logging sinks**: JSON-based sinks work best with the call stack properties
7. **Choose appropriate format**: Exception-like for debugging, legacy for detailed property access

## Compatibility

- **.NET Standard 2.0+**: Compatible with .NET Framework 4.6.1+, .NET Core 2.0+, .NET 5+
- **Serilog 3.0+**: Requires Serilog version 3.0 or higher

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## Changelog

### Version 1.0.24 (2025-07-29)
- **Exception-like Format**: New default format displays call stack like exception traces
- **Single CallStack Property**: Consolidates call stack into one property for cleaner logs
- **Configurable Depth**: `MaxFrames` setting to limit call stack depth (default: 5)
- **Backward Compatibility**: Legacy format still available via configuration
- **Enhanced Configuration**: New `WithCallStackFormat()` method for format control

### Version 1.0.22 (2025-07-28)
- **NuGet Publishing**: Added automatic NuGet publishing workflow
- **CI/CD**: GitHub Actions integration for automated builds and publishing

### Version 1.0.20 (2025-07-28)
- **Stability**: Resolved failing tests by improving frame detection and test expectations
- **Bug Fix**: Removed InMemorySink.Clear() call that caused compilation error
- **Documentation**: Updated project status to stable

### Version 1.0.17 (2025-07-28)
- **Release**: Prepared v1.0.0 release candidate
- **Testing**: Improved test stability and isolation
- **Performance**: Enhanced stack trace frame processing optimization

### Version 1.0.13 (2025-07-28)
- **Documentation**: Enhanced README with performance optimization notes
- **Filtering**: Improved namespace filtering documentation and functionality

### Version 1.0.9 (2025-07-28)
- **Performance**: Optimized stack trace frame processing for high-throughput scenarios
- **Testing**: Enhanced testing framework and utilities
- **Core Features**: Improved CallStackEnricher exception handling

### Version 1.0.4 (2025-07-28)
- **Configuration**: Enhanced CallStackEnricherConfiguration fluent API
- **Core Functionality**: Implemented core call stack enrichment functionality

### Version 1.0.1 (2025-07-28)
- **Initial Release**: Project setup and solution structure
- **Core Features**: Basic call stack enrichment functionality
- **Configuration**: Comprehensive configuration options
- **Documentation**: Complete documentation and examples