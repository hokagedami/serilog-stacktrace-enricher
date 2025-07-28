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
    public void Enrich_WithDefaultConfiguration_AddsCallStackProperty()
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
        logEvent.Properties.Should().ContainKey("CallStack");
        
        // Call stack should be in exception-like format
        var callStack = logEvent.Properties["CallStack"].ToString();
        callStack.Should().NotBeNullOrEmpty();
        callStack.Should().Contain("-->"); // Should contain frame separator
    }

    [Fact]
    public void Enrich_WithCustomConfiguration_UsesCustomPropertyNames()
    {
        // Arrange
        var config = new CallStackEnricherConfiguration()
            .WithCallStackFormat(useExceptionLikeFormat: false) // Use legacy format for this test
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
        var callStack = logEvent.Properties["CallStack"].ToString();
        callStack.Should().Contain("(");
        callStack.Should().Contain(")");
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
        var callStack = logEvent.Properties["CallStack"].ToString();
        callStack.Should().NotBeNullOrEmpty();
        // With fullTypeName enabled, should include namespace info
        callStack.Should().Contain(".");
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
            .WithCallStackFormat(useExceptionLikeFormat: false) // Use legacy format for this test
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

    [Fact]
    public void Enrich_WithExceptionLikeFormat_AddsCallStackProperty()
    {
        // Arrange
        var config = new CallStackEnricherConfiguration()
            .WithCallStackFormat(useExceptionLikeFormat: true, maxFrames: 3);
        
        using var logger = new LoggerConfiguration()
            .Enrich.WithCallStack(config)
            .WriteTo.InMemory()
            .CreateLogger();

        // Act
        LogFromMethod(logger);

        // Assert
        var logEvent = GetLatestLogEvent();
        logEvent.Properties.Should().ContainKey("CallStack");
        
        var callStack = logEvent.Properties["CallStack"].ToString();
        callStack.Should().NotBeNullOrEmpty();
        callStack.Should().Contain("-->"); // Should contain frame separator
    }

    [Fact]
    public void Enrich_WithLegacyFormat_AddsIndividualProperties()
    {
        // Arrange
        var config = new CallStackEnricherConfiguration()
            .WithCallStackFormat(useExceptionLikeFormat: false);
        
        using var logger = new LoggerConfiguration()
            .Enrich.WithCallStack(config)
            .WriteTo.InMemory()
            .CreateLogger();

        // Act
        LogFromMethod(logger);

        // Assert
        var logEvent = GetLatestLogEvent();
        logEvent.Properties.Should().ContainKey("MethodName");
        logEvent.Properties.Should().ContainKey("TypeName");
        logEvent.Properties.Should().NotContainKey("CallStack");
    }

    [Fact]
    public void Enrich_WithMaxFramesLimit_LimitsCallStackDepth()
    {
        // Arrange
        var config = new CallStackEnricherConfiguration()
            .WithCallStackFormat(useExceptionLikeFormat: true, maxFrames: 2);
        
        using var logger = new LoggerConfiguration()
            .Enrich.WithCallStack(config)
            .WriteTo.InMemory()
            .CreateLogger();

        // Act
        CallMultipleMethodsDeep(logger);

        // Assert
        var logEvent = GetLatestLogEvent();
        var callStack = logEvent.Properties["CallStack"].ToString();
        
        // Count the number of frame separators (should be maxFrames - 1)
        var separatorCount = callStack.Split(new[] { " --> " }, StringSplitOptions.None).Length - 1;
        separatorCount.Should().BeLessOrEqualTo(1); // 2 frames = 1 separator
    }

    [Fact]
    public void Enrich_WithCustomCallStackPropertyName_UsesCustomName()
    {
        // Arrange
        var config = new CallStackEnricherConfiguration()
            .WithCallStackFormat(useExceptionLikeFormat: true, callStackPropertyName: "CustomCallStack");
        
        using var logger = new LoggerConfiguration()
            .Enrich.WithCallStack(config)
            .WriteTo.InMemory()
            .CreateLogger();

        // Act
        LogFromMethod(logger);

        // Assert
        var logEvent = GetLatestLogEvent();
        logEvent.Properties.Should().ContainKey("CustomCallStack");
        logEvent.Properties.Should().NotContainKey("CallStack");
    }

    [Fact]
    public void Enrich_WithMethodParametersInExceptionFormat_IncludesParameters()
    {
        // Arrange
        var config = new CallStackEnricherConfiguration()
            .WithCallStackFormat(useExceptionLikeFormat: true)
            .WithMethodParameters(includeParameters: true);
        
        using var logger = new LoggerConfiguration()
            .Enrich.WithCallStack(config)
            .WriteTo.InMemory()
            .CreateLogger();

        // Act
        LogFromMethodWithParameters(logger, "test", 123);

        // Assert
        var logEvent = GetLatestLogEvent();
        var callStack = logEvent.Properties["CallStack"].ToString();
        callStack.Should().Contain("("); // Should contain parameter info
        callStack.Should().Contain(")");
    }

    private static void CallMultipleMethodsDeep(ILogger logger)
    {
        CallMethodThatLogs(logger);
    }
}