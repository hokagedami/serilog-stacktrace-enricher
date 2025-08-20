using System;

namespace Serilog.Enrichers.CallStack;

/// <summary>
/// A lazy property that defers computation until the value is actually needed for serialization.
/// </summary>
internal sealed class LazyLogProperty
{
    private readonly string _name;
    private readonly Lazy<string> _value;

    public LazyLogProperty(string name, Func<string> valueFactory)
    {
        _name = name ?? throw new ArgumentNullException(nameof(name));
        _value = new Lazy<string>(valueFactory ?? throw new ArgumentNullException(nameof(valueFactory)));
    }

    /// <summary>
    /// Gets the property name.
    /// </summary>
    public string Name => _name;

    /// <summary>
    /// Gets the computed value, evaluating it only when first accessed.
    /// </summary>
    public string Value => _value.Value;

    /// <summary>
    /// Checks if the value has been computed yet.
    /// </summary>
    public bool IsValueCreated => _value.IsValueCreated;

    /// <summary>
    /// Returns the string representation of this property.
    /// This triggers the lazy evaluation if not already done.
    /// </summary>
    /// <returns>The computed string value.</returns>
    public override string ToString()
    {
        return Value;
    }

    /// <summary>
    /// Implicit conversion to string for convenience.
    /// </summary>
    /// <param name="lazyProperty">The lazy property to convert.</param>
    /// <returns>The computed string value.</returns>
    public static implicit operator string(LazyLogProperty lazyProperty)
    {
        return lazyProperty?.Value ?? string.Empty;
    }
}