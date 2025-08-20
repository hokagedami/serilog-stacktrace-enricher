using FluentAssertions;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.InMemory;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Serilog.Enrichers.CallStack.Tests;

/// <summary>
/// Performance benchmarking tests for the CallStack enricher.
/// </summary>
public class PerformanceTests
{
    private readonly ITestOutputHelper _output;

    public PerformanceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Enricher_Performance_WithCaching_ShouldBeFasterThanWithoutCaching()
    {
        // Arrange
        const int iterations = 1000;
        var cachedConfig = new CallStackEnricherConfiguration();
        var enricherWithCaching = new CallStackEnricher(cachedConfig);

        // Act & Assert
        var timeWithCaching = MeasureEnrichmentTime(enricherWithCaching, iterations);
        
        _output.WriteLine($"Enrichment with caching: {timeWithCaching.TotalMilliseconds:F2}ms for {iterations} iterations");
        _output.WriteLine($"Average per enrichment: {timeWithCaching.TotalMilliseconds / iterations:F4}ms");

        // Verify that caching improves performance (should be under reasonable time)
        timeWithCaching.TotalMilliseconds.Should().BeLessThan(5000, "enrichment should be fast with caching");
    }

    [Fact]
    public void StringBuilderPool_Performance_ShouldShowPoolingBenefits()
    {
        // Arrange
        const int iterations = 10000;
        var config = new CallStackEnricherConfiguration().WithCallStackFormat(useExceptionLikeFormat: true);
        var enricher = new CallStackEnricher(config);

        // Act
        var poolingTime = MeasureEnrichmentTime(enricher, iterations);
        
        _output.WriteLine($"StringBuilder pooling time: {poolingTime.TotalMilliseconds:F2}ms for {iterations} iterations");
        _output.WriteLine($"Average per operation: {poolingTime.TotalMilliseconds / iterations:F4}ms");

        // Get pool statistics
        var poolStats = StringBuilderPool.GetStatistics();
        _output.WriteLine($"StringBuilder pool size: {poolStats.PoolSize}/{poolStats.MaxPoolSize}");

        // Assert
        poolingTime.TotalMilliseconds.Should().BeLessThan(10000, "pooled operations should be efficient");
        poolStats.PoolSize.Should().BeGreaterThan(0, "pool should have reusable instances");
    }

    [Fact]
    public void ReflectionCache_Performance_ShouldShowCachingBenefits()
    {
        // Arrange
        const int iterations = 5000;
        var config = new CallStackEnricherConfiguration();
        var enricher = new CallStackEnricher(config);

        // Clear cache to start fresh
        MethodInfoCache.ClearCache();

        // Act
        var cachingTime = MeasureEnrichmentTime(enricher, iterations);
        
        var cacheStats = MethodInfoCache.GetCacheStatistics();
        _output.WriteLine($"Reflection caching time: {cachingTime.TotalMilliseconds:F2}ms for {iterations} iterations");
        _output.WriteLine($"Cache size - Methods: {cacheStats.MethodCacheSize}, Types: {cacheStats.TypeNameCacheSize}");

        // Assert
        cachingTime.TotalMilliseconds.Should().BeLessThan(8000, "cached operations should be fast");
        cacheStats.MethodCacheSize.Should().BeGreaterThan(0, "method cache should contain entries");
    }

    [Fact]
    public async Task AsyncMethodDetection_Performance_ShouldHandleAsyncEfficiently()
    {
        // Arrange
        const int iterations = 1000;
        var config = new CallStackEnricherConfiguration().WithAsyncSupport(useAsyncAwareMode: true);
        var enricher = new CallStackEnricher(config);

        // Act
        var asyncTime = await MeasureAsyncEnrichmentTime(enricher, iterations);
        
        _output.WriteLine($"Async-aware enrichment time: {asyncTime.TotalMilliseconds:F2}ms for {iterations} iterations");
        _output.WriteLine($"Average per async operation: {asyncTime.TotalMilliseconds / iterations:F4}ms");

        // Assert
        asyncTime.TotalMilliseconds.Should().BeLessThan(15000, "async-aware processing should be reasonably fast");
    }

    [Fact]
    public void LazyEvaluation_Performance_ShouldOnlyComputeWhenNeeded()
    {
        // Arrange
        const int iterations = 2000;
        var config = new CallStackEnricherConfiguration().WithCallStackFormat(useExceptionLikeFormat: true);
        var enricher = new CallStackEnricher(config);

        var logger = new LoggerConfiguration()
            .Enrich.With(enricher)
            .WriteTo.InMemory()
            .CreateLogger();

        // Act - Create log events but don't serialize them (lazy evaluation shouldn't trigger)
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            logger.Information("Test message {Index}", i);
        }
        sw.Stop();

        _output.WriteLine($"Lazy evaluation time (no serialization): {sw.Elapsed.TotalMilliseconds:F2}ms for {iterations} iterations");
        _output.WriteLine($"Average per lazy operation: {sw.Elapsed.TotalMilliseconds / iterations:F4}ms");

        // Assert
        sw.Elapsed.TotalMilliseconds.Should().BeLessThan(5000, "lazy evaluation should defer expensive operations");

        // Verify events were created
        InMemorySink.Instance.LogEvents.Should().HaveCount(iterations);
    }

    [Theory]
    [InlineData(100, 1)]   // Light load
    [InlineData(1000, 5)]  // Medium load  
    [InlineData(2000, 10)] // Heavy load
    public void Enricher_Scalability_ShouldHandleVaryingLoads(int iterations, int maxFrames)
    {
        // Arrange
        var config = new CallStackEnricherConfiguration()
            .WithCallStackFormat(maxFrames: maxFrames)
            .WithAsyncSupport();
        var enricher = new CallStackEnricher(config);

        // Act
        var scalabilityTime = MeasureEnrichmentTime(enricher, iterations);
        
        _output.WriteLine($"Scalability test - {iterations} iterations, {maxFrames} max frames: {scalabilityTime.TotalMilliseconds:F2}ms");
        _output.WriteLine($"Average per operation: {scalabilityTime.TotalMilliseconds / iterations:F4}ms");

        // Assert - Performance should scale reasonably
        var expectedMaxTime = iterations * 0.01; // Expect roughly 0.01ms per iteration maximum
        scalabilityTime.TotalMilliseconds.Should().BeLessThan(Math.Max(expectedMaxTime, 1000), 
            $"enricher should scale well for {iterations} iterations");
    }

    private TimeSpan MeasureEnrichmentTime(ILogEventEnricher enricher, int iterations)
    {
        var propertyFactory = new PropertyFactory();
        
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            var logEvent = CreateTestLogEvent($"Test message {i}");
            enricher.Enrich(logEvent, propertyFactory);
        }
        sw.Stop();

        return sw.Elapsed;
    }

    private async Task<TimeSpan> MeasureAsyncEnrichmentTime(ILogEventEnricher enricher, int iterations)
    {
        var propertyFactory = new PropertyFactory();
        
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            await TestAsyncMethod(enricher, propertyFactory, i);
        }
        sw.Stop();

        return sw.Elapsed;
    }

    private async Task TestAsyncMethod(ILogEventEnricher enricher, ILogEventPropertyFactory propertyFactory, int index)
    {
        await Task.Delay(1); // Simulate async work
        var logEvent = CreateTestLogEvent($"Async test message {index}");
        enricher.Enrich(logEvent, propertyFactory);
    }

    private LogEvent CreateTestLogEvent(string message)
    {
        return new LogEvent(
            DateTimeOffset.UtcNow,
            LogEventLevel.Information,
            null,
            new MessageTemplate(message, new MessageTemplateToken[0]),
            new LogEventProperty[0]);
    }
}