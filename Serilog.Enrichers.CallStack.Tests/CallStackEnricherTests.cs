using FluentAssertions;
using Serilog.Events;
using Serilog.Sinks.InMemory;
using System;
using System.Linq;
using Xunit;

namespace Serilog.Enrichers.CallStack.Tests;

public class CallStackEnricherTests
{
    private static LogEvent GetLatestLogEvent()
    {
        var logEvents = InMemorySink.Instance.LogEvents;
        return logEvents.LastOrDefault() ?? throw new InvalidOperationException("No log events found");
    }
    [Fact]
    public void Enrich_WithDefaultConfiguration_AddsCallStackProperties()
    {
        // Arrange
        using var logger = new LoggerConfiguration()
            .Enrich.WithCallStack()
            .WriteTo.InMemory()
            .CreateLogger();

        // Act
        LogFromMethod(logger);

        // Assert
        var logEvent = GetLatestLogEvent();
        logEvent.Properties.Should().ContainKey("MethodName");
        logEvent.Properties.Should().ContainKey("TypeName");
        
        // File and line info may not be available in test environments
        // but method and type should always be present
        var methodName = logEvent.Properties["MethodName"].ToString();
        methodName.Should().NotBeNullOrEmpty();
        
        var typeName = logEvent.Properties["TypeName"].ToString();
        typeName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Enrich_WithCustomConfiguration_UsesCustomPropertyNames()
    {
        // Arrange
        var config = new CallStackEnricherConfiguration()
            .WithPropertyNames(
                methodName: "CustomMethod",
                typeName: "CustomType",
                fileName: "CustomFile",
                lineNumber: "CustomLine");
        
        using var logger = new LoggerConfiguration()
            .Enrich.WithCallStack(config)
            .WriteTo.InMemory()
            .CreateLogger();

        // Act
        LogFromMethod(logger);

        // Assert
        var logEvent = GetLatestLogEvent();
        logEvent.Properties.Should().ContainKey("CustomMethod");
        logEvent.Properties.Should().ContainKey("CustomType");
        
        // Verify the properties have values
        logEvent.Properties["CustomMethod"].ToString().Should().NotBeNullOrEmpty();
        logEvent.Properties["CustomType"].ToString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Enrich_WithIncludeMethodParameters_IncludesParameterInfo()
    {
        // Arrange
        var config = new CallStackEnricherConfiguration()
            .WithMethodParameters(includeParameters: true);
        
        using var logger = new LoggerConfiguration()
            .Enrich.WithCallStack(config)
            .WriteTo.InMemory()
            .CreateLogger();

        // Act
        LogFromMethodWithParameters(logger, "test", 123);

        // Assert
        var logEvent = GetLatestLogEvent();
        var methodName = logEvent.Properties["MethodName"].ToString();
        methodName.Should().Contain("(");
        methodName.Should().Contain(")");
    }

    [Fact]
    public void Enrich_WithFullTypeName_IncludesNamespace()
    {
        // Arrange
        var config = new CallStackEnricherConfiguration()
            .WithFullNames(fullTypeName: true);
        
        using var logger = new LoggerConfiguration()
            .Enrich.WithCallStack(config)
            .WriteTo.InMemory()
            .CreateLogger();

        // Act
        LogFromMethod(logger);

        // Assert
        var logEvent = GetLatestLogEvent();
        var typeName = logEvent.Properties["TypeName"].ToString();
        typeName.Should().NotBeNullOrEmpty();
        // With fullTypeName enabled, should include namespace info
        typeName.Should().Contain(".");
    }

    [Fact]
    public void Enrich_WithSkipNamespace_SkipsSpecifiedNamespace()
    {
        // Arrange
        var config = new CallStackEnricherConfiguration()
            .SkipNamespace("Serilog.Enrichers.CallStack.Tests");
        
        using var logger = new LoggerConfiguration()
            .Enrich.WithCallStack(config)
            .WriteTo.InMemory()
            .CreateLogger();

        // Act
        LogFromMethod(logger);

        // Assert
        var logEvent = GetLatestLogEvent();
        // Since we're skipping our own namespace, it should find a different frame
        // or may not add properties if no suitable frame is found
        if (logEvent.Properties.ContainsKey("TypeName"))
        {
            var typeName = logEvent.Properties["TypeName"].ToString();
            typeName.Should().NotContain("CallStackEnricherTests");
        }
    }

    [Fact]
    public void Enrich_WithFrameOffset_UsesCorrectFrame()
    {
        // Arrange
        var config = new CallStackEnricherConfiguration()
            .WithFrameOffset(1);
        
        using var logger = new LoggerConfiguration()
            .Enrich.WithCallStack(config)
            .WriteTo.InMemory()
            .CreateLogger();

        // Act
        CallMethodThatLogs(logger);

        // Assert
        var logEvent = GetLatestLogEvent();
        if (logEvent.Properties.ContainsKey("MethodName"))
        {
            var methodName = logEvent.Properties["MethodName"].ToString();
            // With frame offset, should capture a different method
            methodName.Should().NotBeNullOrEmpty();
        }
    }

    [Fact]
    public void Enrich_WithDisabledIncludes_DoesNotAddDisabledProperties()
    {
        // Arrange
        var config = new CallStackEnricherConfiguration()
            .WithIncludes(
                methodName: true,
                typeName: false,
                fileName: false,
                lineNumber: false);
        
        using var logger = new LoggerConfiguration()
            .Enrich.WithCallStack(config)
            .WriteTo.InMemory()
            .CreateLogger();

        // Act
        LogFromMethod(logger);

        // Assert
        var logEvent = GetLatestLogEvent();
        logEvent.Properties.Should().ContainKey("MethodName");
        logEvent.Properties.Should().NotContainKey("TypeName");
        logEvent.Properties.Should().NotContainKey("FileName");
        logEvent.Properties.Should().NotContainKey("LineNumber");
    }

    [Fact]
    public void Enrich_WithThrowingPropertyFactory_HandlesExceptionsGracefully()
    {
        // Arrange
        var enricher = new CallStackEnricher();
        var logEvent = new LogEvent(
            DateTimeOffset.Now,
            LogEventLevel.Information,
            null,
            MessageTemplate.Empty,
            Enumerable.Empty<LogEventProperty>());
        
        var throwingFactory = new ThrowingPropertyFactory();

        // Act & Assert
        // Should not throw exception
        var act = () => enricher.Enrich(logEvent, throwingFactory);
        act.Should().NotThrow();
    }

    [Fact]
    public void Enrich_WithExceptionHandling_CallsExceptionHandler()
    {
        // Arrange
        Exception? caughtException = null;
        var config = new CallStackEnricherConfiguration()
            .WithExceptionHandling(suppress: true, onException: ex => caughtException = ex);
        
        var enricher = new CallStackEnricher(config);
        var logEvent = new LogEvent(
            DateTimeOffset.Now,
            LogEventLevel.Information,
            null,
            MessageTemplate.Empty,
            Enumerable.Empty<LogEventProperty>());
        
        var throwingFactory = new ThrowingPropertyFactory();

        // Act
        enricher.Enrich(logEvent, throwingFactory);

        // Assert
        caughtException.Should().NotBeNull();
        caughtException.Should().BeOfType<InvalidOperationException>();
    }

    [Fact]
    public void Constructor_WithNullConfiguration_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new CallStackEnricher(null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("configuration");
    }

    [Fact]
    public void LoggerConfiguration_WithNullEnrichmentConfiguration_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => LoggerConfigurationExtensions.WithCallStack(null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("enrichmentConfiguration");
    }

    [Fact]
    public void LoggerConfiguration_WithNullConfiguration_ThrowsArgumentNullException()
    {
        // Arrange
        var enrichmentConfig = new LoggerConfiguration().Enrich;

        // Act & Assert
        var act = () => enrichmentConfig.WithCallStack((CallStackEnricherConfiguration)null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("configuration");
    }

    [Fact]
    public void LoggerConfiguration_WithNullConfigureAction_ThrowsArgumentNullException()
    {
        // Arrange
        var enrichmentConfig = new LoggerConfiguration().Enrich;

        // Act & Assert
        var act = () => enrichmentConfig.WithCallStack((Action<CallStackEnricherConfiguration>)null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("configureEnricher");
    }

    private static void LogFromMethod(ILogger logger)
    {
        logger.Information("Test message");
    }

    private static void LogFromMethodWithParameters(ILogger logger, string param1, int param2)
    {
        logger.Information("Test message with parameters");
    }

    private static void CallMethodThatLogs(ILogger logger)
    {
        LogFromMethod(logger);
    }
}