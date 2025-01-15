using System;
using System.Collections.Generic;

namespace Serilog.Enrichers.CallStack;

/// <summary>
/// Configuration for the CallStackEnricher.
/// </summary>
public class CallStackEnricherConfiguration
{
    /// <summary>
    /// Gets or sets whether to include the method name in the log event.
    /// Default is true.
    /// </summary>
    public bool IncludeMethodName { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to include method parameters in the method name.
    /// Default is false.
    /// </summary>
    public bool IncludeMethodParameters { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to use full parameter types (with namespace) when including method parameters.
    /// Default is false.
    /// </summary>
    public bool UseFullParameterTypes { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to include the type name in the log event.
    /// Default is true.
    /// </summary>
    public bool IncludeTypeName { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to use the full type name (with namespace) or just the class name.
    /// Default is false (use class name only).
    /// </summary>
    public bool UseFullTypeName { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to include the file name in the log event.
    /// Default is true.
    /// </summary>
    public bool IncludeFileName { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to use the full file path or just the file name.
    /// Default is false (use file name only).
    /// </summary>
    public bool UseFullFileName { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to include the line number in the log event.
    /// Default is true.
    /// </summary>
    public bool IncludeLineNumber { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to include the column number in the log event.
    /// Default is false.
    /// </summary>
    public bool IncludeColumnNumber { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to include the assembly name in the log event.
    /// Default is false.
    /// </summary>
    public bool IncludeAssemblyName { get; set; } = false;

    /// <summary>
    /// Gets or sets the property name for the method name.
    /// Default is "MethodName".
    /// </summary>
    public string MethodNamePropertyName { get; set; } = "MethodName";

    /// <summary>
    /// Gets or sets the property name for the type name.
    /// Default is "TypeName".
    /// </summary>
    public string TypeNamePropertyName { get; set; } = "TypeName";

    /// <summary>
    /// Gets or sets the property name for the file name.
    /// Default is "FileName".
    /// </summary>
    public string FileNamePropertyName { get; set; } = "FileName";

    /// <summary>
    /// Gets or sets the property name for the line number.
    /// Default is "LineNumber".
    /// </summary>
    public string LineNumberPropertyName { get; set; } = "LineNumber";

    /// <summary>
    /// Gets or sets the property name for the column number.
    /// Default is "ColumnNumber".
    /// </summary>
    public string ColumnNumberPropertyName { get; set; } = "ColumnNumber";

    /// <summary>
    /// Gets or sets the property name for the assembly name.
    /// Default is "AssemblyName".
    /// </summary>
    public string AssemblyNamePropertyName { get; set; } = "AssemblyName";

    /// <summary>
    /// Gets or sets the frame offset to use when selecting the relevant stack frame.
    /// Default is 0 (use the first non-skipped frame).
    /// </summary>
    public int FrameOffset { get; set; } = 0;

    /// <summary>
    /// Gets or sets whether to suppress exceptions that occur during enrichment.
    /// Default is true.
    /// </summary>
    public bool SuppressExceptions { get; set; } = true;

    /// <summary>
    /// Gets or sets the action to execute when an exception occurs during enrichment.
    /// This is only called when SuppressExceptions is true.
    /// Default is null.
    /// </summary>
    public Action<Exception>? OnException { get; set; }

    /// <summary>
    /// Gets the collection of namespaces to skip when walking the stack trace.
    /// </summary>
    public ICollection<string> SkipNamespaces { get; } = new HashSet<string>(StringComparer.Ordinal);

    /// <summary>
    /// Gets the collection of type names to skip when walking the stack trace.
    /// </summary>
    public ICollection<string> SkipTypes { get; } = new HashSet<string>(StringComparer.Ordinal);

    /// <summary>
    /// Adds a namespace to skip when walking the stack trace.
    /// </summary>
    /// <param name="namespace">The namespace to skip.</param>
    /// <returns>This configuration instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when namespace is null.</exception>
    public CallStackEnricherConfiguration SkipNamespace(string @namespace)
    {
        if (@namespace == null)
            throw new ArgumentNullException(nameof(@namespace));
        
        SkipNamespaces.Add(@namespace);
        return this;
    }

    /// <summary>
    /// Adds a type name to skip when walking the stack trace.
    /// </summary>
    /// <param name="typeName">The full type name to skip.</param>
    /// <returns>This configuration instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when typeName is null.</exception>
    public CallStackEnricherConfiguration SkipType(string typeName)
    {
        if (typeName == null)
            throw new ArgumentNullException(nameof(typeName));
        
        SkipTypes.Add(typeName);
        return this;
    }

    /// <summary>
    /// Sets the frame offset to use when selecting the relevant stack frame.
    /// </summary>
    /// <param name="offset">The frame offset (0-based).</param>
    /// <returns>This configuration instance for method chaining.</returns>
    public CallStackEnricherConfiguration WithFrameOffset(int offset)
    {
        FrameOffset = Math.Max(0, offset);
        return this;
    }

    /// <summary>
    /// Configures exception handling.
    /// </summary>
    /// <param name="suppress">Whether to suppress exceptions.</param>
    /// <param name="onException">Optional action to execute when exceptions occur.</param>
    /// <returns>This configuration instance for method chaining.</returns>
    public CallStackEnricherConfiguration WithExceptionHandling(bool suppress, Action<Exception>? onException = null)
    {
        SuppressExceptions = suppress;
        OnException = onException;
        return this;
    }

    /// <summary>
    /// Configures which information to include in the log event.
    /// </summary>
    /// <param name="methodName">Whether to include method name.</param>
    /// <param name="typeName">Whether to include type name.</param>
    /// <param name="fileName">Whether to include file name.</param>
    /// <param name="lineNumber">Whether to include line number.</param>
    /// <param name="columnNumber">Whether to include column number.</param>
    /// <param name="assemblyName">Whether to include assembly name.</param>
    /// <returns>This configuration instance for method chaining.</returns>
    public CallStackEnricherConfiguration WithIncludes(
        bool methodName = true,
        bool typeName = true,
        bool fileName = true,
        bool lineNumber = true,
        bool columnNumber = false,
        bool assemblyName = false)
    {
        IncludeMethodName = methodName;
        IncludeTypeName = typeName;
        IncludeFileName = fileName;
        IncludeLineNumber = lineNumber;
        IncludeColumnNumber = columnNumber;
        IncludeAssemblyName = assemblyName;
        return this;
    }

    /// <summary>
    /// Configures the property names used for call stack information.
    /// </summary>
    /// <param name="methodName">Property name for method name.</param>
    /// <param name="typeName">Property name for type name.</param>
    /// <param name="fileName">Property name for file name.</param>
    /// <param name="lineNumber">Property name for line number.</param>
    /// <param name="columnNumber">Property name for column number.</param>
    /// <param name="assemblyName">Property name for assembly name.</param>
    /// <returns>This configuration instance for method chaining.</returns>
    public CallStackEnricherConfiguration WithPropertyNames(
        string? methodName = null,
        string? typeName = null,
        string? fileName = null,
        string? lineNumber = null,
        string? columnNumber = null,
        string? assemblyName = null)
    {
        if (!string.IsNullOrEmpty(methodName))
            MethodNamePropertyName = methodName;
        if (!string.IsNullOrEmpty(typeName))
            TypeNamePropertyName = typeName;
        if (!string.IsNullOrEmpty(fileName))
            FileNamePropertyName = fileName;
        if (!string.IsNullOrEmpty(lineNumber))
            LineNumberPropertyName = lineNumber;
        if (!string.IsNullOrEmpty(columnNumber))
            ColumnNumberPropertyName = columnNumber;
        if (!string.IsNullOrEmpty(assemblyName))
            AssemblyNamePropertyName = assemblyName;
        return this;
    }

    /// <summary>
    /// Configures whether to use full names or short names.
    /// </summary>
    /// <param name="fullTypeName">Whether to use full type names (with namespace).</param>
    /// <param name="fullFileName">Whether to use full file paths.</param>
    /// <param name="fullParameterTypes">Whether to use full parameter type names.</param>
    /// <returns>This configuration instance for method chaining.</returns>
    public CallStackEnricherConfiguration WithFullNames(
        bool fullTypeName = false,
        bool fullFileName = false,
        bool fullParameterTypes = false)
    {
        UseFullTypeName = fullTypeName;
        UseFullFileName = fullFileName;
        UseFullParameterTypes = fullParameterTypes;
        return this;
    }

    /// <summary>
    /// Configures method parameter inclusion.
    /// </summary>
    /// <param name="includeParameters">Whether to include method parameters.</param>
    /// <param name="useFullParameterTypes">Whether to use full parameter type names.</param>
    /// <returns>This configuration instance for method chaining.</returns>
    public CallStackEnricherConfiguration WithMethodParameters(
        bool includeParameters = true,
        bool useFullParameterTypes = false)
    {
        IncludeMethodParameters = includeParameters;
        UseFullParameterTypes = useFullParameterTypes;
        return this;
    }
}