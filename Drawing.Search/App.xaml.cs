using System;
using System.Windows;
using Drawing.Search.Core;
using Drawing.Search.Core.CADIntegrationService;
using Drawing.Search.Core.CADIntegrationService.Interfaces;
using Drawing.Search.Core.SearchService;
using Microsoft.Extensions.DependencyInjection;

namespace Drawing.Search
{
    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        public static IServiceProvider ServiceProvider { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            ServiceProvider = serviceCollection.BuildServiceProvider();
            
            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
            base.OnStartup(e);
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IDrawingHandler,TeklaDrawingHandler>();
            services.AddSingleton<MainWindow>();
            services.AddSingleton<SearchService>();
            services.AddSingleton<SearchViewModel>();
        }
    }
}