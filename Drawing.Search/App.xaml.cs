using System;
using System.Threading;
using System.Windows;
using Drawing.Search.Caching.Interfaces;
using Drawing.Search.CADIntegration;
using Drawing.Search.CADIntegration.Interfaces;
using Drawing.Search.CADIntegration.TeklaWrappers;
using Drawing.Search.Common.Interfaces;
using Drawing.Search.Common.SearchTypes;
using Drawing.Search.Core;
using Drawing.Search.Core.CacheService;
using Drawing.Search.Core.SearchService;
using Microsoft.Extensions.DependencyInjection;

namespace Drawing.Search
{
    /// <summary>
    ///     The main application class for the Drawing Search tool.
    ///     Handles application startup and dependency injection configuration.
    /// </summary>
    public partial class App
    {
        /// <summary>
        ///     Gets the global service provider for the application.
        ///     Allows resolving services and handling dependency injection.
        /// </summary>
        private static IServiceProvider ServiceProvider { get; set; }

        /// <summary>
        ///     Handles application startup events. Configures services, builds the service provider,
        ///     and initializes the main window.
        /// </summary>
        /// <param name="e">Event arguments for the application startup event.</param>
        protected override void OnStartup(StartupEventArgs e)
        {
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            ServiceProvider = serviceCollection.BuildServiceProvider();

            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
            base.OnStartup(e);
        }

        /// <summary>
        ///     Configures dependency injection services for the application.
        ///     Registers services, view models, and other components in the DI container.
        /// </summary>
        /// <param name="services">The service collection to configure.</param>
        private void ConfigureServices(IServiceCollection services)
        {
            // Registers the Drawing Handler for CAD integration
            services.AddSingleton<IDrawingHandler, TeklaDrawingHandler>();

            // Registers the search service
            services.AddSingleton<SearchService>();

            // Registers the search cache service
            services.AddSingleton<ICacheService, TeklaCacheService>();
            services.AddSingleton<ISearchCache, TeklaSearchCache>();

            // Registers the logger
            services.AddSingleton<ISearchLogger, SearchLogger>();

            // Registers the search driver
            services.AddSingleton<SearchDriver>();
            services.AddSingleton<SynchronizationContext>();

            // Registers the search view model
            services.AddSingleton<SearchViewModel>();

            // Registers the main application window
            services.AddSingleton<MainWindow>();
        }
    }
}