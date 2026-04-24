using System;
using System.Linq;
using DocuFiller.Services;
using DocuFiller.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DocuFiller.E2ERegression;

/// <summary>
/// Version-compatible service factory using DI container.
/// Automatically adapts to both d81cd00 (9-param constructor with IDataParser)
/// and M004+ (8-param constructor without IDataParser).
/// </summary>
public static class ServiceFactory
{
    /// <summary>
    /// Build a ServiceProvider with all required services registered.
    /// Conditionally registers IDataParser/DataParserService if they exist.
    /// </summary>
    public static IServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();

        // Logging
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));

        // Core services (present in both versions)
        services.AddSingleton<IFileService, FileService>();
        services.AddSingleton<IExcelDataParser, ExcelDataParserService>();
        services.AddSingleton<IProgressReporter, ProgressReporterService>();
        services.AddSingleton<ContentControlProcessor>();
        services.AddSingleton<CommentManager>();
        services.AddSingleton<ISafeTextReplacer, SafeTextReplacer>();
        services.AddSingleton<ISafeFormattedContentReplacer, SafeFormattedContentReplacer>();

        // Conditional: register IDataParser -> DataParserService if types exist
        // (present on d81cd00, deleted in M004)
        var dataParserInterface = FindType("DocuFiller.Services.Interfaces.IDataParser");
        var dataParserImpl = FindType("DocuFiller.Services.DataParserService");

        if (dataParserInterface != null && dataParserImpl != null)
        {
            services.AddSingleton(dataParserInterface, dataParserImpl);
        }

        // Build intermediate provider for IServiceProvider registration
        var tempProvider = services.BuildServiceProvider();
        services.AddSingleton<IServiceProvider>(tempProvider);

        // DocumentProcessorService — DI auto-resolves constructor params
        services.AddSingleton<IDocumentProcessor, DocumentProcessorService>();

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Create a fully constructed DocumentProcessorService.
    /// </summary>
    public static DocumentProcessorService CreateProcessor()
    {
        var provider = BuildServiceProvider();
        return (DocumentProcessorService)provider.GetRequiredService<IDocumentProcessor>();
    }

    /// <summary>
    /// Get a fully constructed IDocumentProcessor.
    /// </summary>
    public static IDocumentProcessor GetProcessor()
    {
        var provider = BuildServiceProvider();
        return provider.GetRequiredService<IDocumentProcessor>();
    }

    /// <summary>
    /// Get a fully constructed IExcelDataParser.
    /// </summary>
    public static IExcelDataParser GetExcelParser()
    {
        var provider = BuildServiceProvider();
        return provider.GetRequiredService<IExcelDataParser>();
    }

    private static Type? FindType(string fullName)
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a =>
            {
                try { return a.GetTypes(); }
                catch { return Type.EmptyTypes; }
            })
            .FirstOrDefault(t => t.FullName == fullName);
    }
}
