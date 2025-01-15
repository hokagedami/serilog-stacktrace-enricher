using Serilog.Configuration;
using System;

namespace Serilog.Enrichers.CallStack;

/// <summary>
/// Extensions for configuring the CallStackEnricher.
/// </summary>
public static class LoggerConfigurationExtensions
{
    /// <summary>
    /// Enriches log events with call stack information using default configuration.
    /// </summary>
    /// <param name="enrichmentConfiguration">The logger enrichment configuration.</param>
    /// <returns>Configuration object allowing method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when enrichmentConfiguration is null.</exception>
    public static LoggerConfiguration WithCallStack(this LoggerEnrichmentConfiguration enrichmentConfiguration)
    {
        if (enrichmentConfiguration == null)
            throw new ArgumentNullException(nameof(enrichmentConfiguration));

        return enrichmentConfiguration.With<CallStackEnricher>();
    }

    /// <summary>
    /// Enriches log events with call stack information using the specified configuration.
    /// </summary>
    /// <param name="enrichmentConfiguration">The logger enrichment configuration.</param>
    /// <param name="configuration">The call stack enricher configuration.</param>
    /// <returns>Configuration object allowing method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when enrichmentConfiguration or configuration is null.</exception>
    public static LoggerConfiguration WithCallStack(
        this LoggerEnrichmentConfiguration enrichmentConfiguration,
        CallStackEnricherConfiguration configuration)
    {
        if (enrichmentConfiguration == null)
            throw new ArgumentNullException(nameof(enrichmentConfiguration));
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        return enrichmentConfiguration.With(new CallStackEnricher(configuration));
    }

    /// <summary>
    /// Enriches log events with call stack information using a configuration builder.
    /// </summary>
    /// <param name="enrichmentConfiguration">The logger enrichment configuration.</param>
    /// <param name="configureEnricher">Action to configure the enricher.</param>
    /// <returns>Configuration object allowing method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when enrichmentConfiguration or configureEnricher is null.</exception>
    public static LoggerConfiguration WithCallStack(
        this LoggerEnrichmentConfiguration enrichmentConfiguration,
        Action<CallStackEnricherConfiguration> configureEnricher)
    {
        if (enrichmentConfiguration == null)
            throw new ArgumentNullException(nameof(enrichmentConfiguration));
        if (configureEnricher == null)
            throw new ArgumentNullException(nameof(configureEnricher));

        var configuration = new CallStackEnricherConfiguration();
        configureEnricher(configuration);

        return enrichmentConfiguration.With(new CallStackEnricher(configuration));
    }
}