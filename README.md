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

## Performance Optimizations

This enricher includes several performance optimizations to minimize impact on your application:

### Caching System
- **Reflection Results Caching**: Method and type information is cached to avoid repeated reflection calls
- **Thread-Safe Design**: All caches use concurrent collections for multi-threaded environments
- **Automatic Memory Management**: Caches have reasonable size limits to prevent memory bloat

### String Operations
- **StringBuilder Pooling**: Reusable StringBuilder instances reduce memory allocations
- **Optimized String Building**: Efficient concatenation for call stack formatting
- **Memory Efficient**: Pool management prevents excessive memory usage

### Lazy Evaluation
- **Deferred Computation**: Call stacks are only built when actually serialized to output
- **Smart Frame Capture**: Stack frames are captured lazily to avoid unnecessary work
- **Conditional Processing**: Only processes call stacks when logging level/filters require it

### Async-Friendly Processing
- **State Machine Detection**: Automatically detects and filters async state machine frames
- **Original Method Recovery**: Shows actual method names instead of compiler-generated ones
- **Cleaner Output**: Removes async noise for more readable call stacks

### Framework-Specific Optimizations
- **.NET 6.0+**: Enhanced string operations and improved stack trace capabilities
- **.NET 8.0+**: Span<T> optimizations for zero-allocation processing where possible
- **Conditional Compilation**: Different code paths for optimal performance per framework

### Performance Configuration

```csharp
// Optimize for high-throughput scenarios
Log.Logger = new LoggerConfiguration()
    .Enrich.WithCallStack(config => config
        .WithCallStackFormat(maxFrames: 3)  // Limit frames for better performance
        .WithAsyncSupport(filterAsyncNoise: true)  // Clean async output
        .SkipNamespace("System")  // Skip system namespaces
        .SkipNamespace("Microsoft"))  // Skip framework code
    .CreateLogger();
```

## Compatibility

This package supports multiple target frameworks with optimizations for newer runtimes:

- **.NET Standard 2.0**: Universal compatibility with .NET Framework 4.6.1+, .NET Core 2.0+
- **.NET Framework 4.8**: Enhanced support for legacy .NET Framework applications
- **.NET 6.0**: Modern .NET with improved performance and features
- **.NET 7.0**: Latest performance optimizations and runtime improvements  
- **.NET 8.0**: Cutting-edge features with Span<T> optimizations and enhanced performance
- **Serilog 3.0+**: Requires Serilog version 3.0 or higher

### Runtime-Specific Features

- **.NET 6.0+**: Enhanced stack trace capabilities and improved string operations
- **.NET 8.0+**: Span<T> optimizations for better memory efficiency
- **All frameworks**: Full backward compatibility maintained

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## Changelog

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