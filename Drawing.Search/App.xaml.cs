﻿using System;
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
        private Mutex _mutex;

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
            base.OnStartup(e);

            const string appName = "Drawing.Search";
            _mutex = new Mutex(true, appName, out var createdNew);
            if (!createdNew)
            {
                MessageBox.Show("Application is already running.", "Warning", MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                Current.Shutdown();
                return;
            }

            try
            {
                var serviceCollection = new ServiceCollection();
                ConfigureServices(serviceCollection);
                ServiceProvider = serviceCollection.BuildServiceProvider();

                var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to start application: {ex.Message}", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Current.Shutdown();
                return;
            }
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

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            if (_mutex != null)
            {
                _mutex.ReleaseMutex();
                _mutex.Dispose();
            }
        }
    }
}