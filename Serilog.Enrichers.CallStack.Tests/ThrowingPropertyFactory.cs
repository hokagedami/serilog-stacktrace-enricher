using Serilog.Core;
using Serilog.Events;
using System;

namespace Serilog.Enrichers.CallStack.Tests;

/// <summary>
/// Test helper class that throws exceptions when creating properties.
/// Used to test exception handling in the enricher.
/// </summary>
public class ThrowingPropertyFactory : ILogEventPropertyFactory
{
    /// <summary>
    /// Always throws an InvalidOperationException when called.
    /// </summary>
    /// <param name="name">The property name.</param>
    /// <param name="value">The property value.</param>
    /// <param name="destructureObjects">Whether to destructure the value.</param>
    /// <returns>Never returns - always throws an exception.</returns>
    /// <exception cref="InvalidOperationException">Always thrown to test exception handling.</exception>
    public LogEventProperty CreateProperty(string name, object? value, bool destructureObjects = false)
    {
        throw new InvalidOperationException("Test exception from ThrowingPropertyFactory");
    }
}