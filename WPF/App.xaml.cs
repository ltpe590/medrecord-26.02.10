using Core.Configuration;
using Core.Data.Context;
using Core.Http;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Repositories;
using Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.IO;
using System.Windows;
using WPF.Mappers;
using WPF.ViewModels;
using WPF.Views;

namespace WPF
{
    public partial class App : Application
    {
        private static IHost? _host;

        public static IServiceProvider Services
            => _host?.Services ?? throw new InvalidOperationException("Host not initialized");

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            _host = Host.CreateDefaultBuilder()
                .ConfigureLogging(ConfigureLogging)
                .ConfigureServices(ConfigureServices)  // <-- FIXED: Call the actual method!
                .Build();

            _host.Start();

            // Resolve and show main window
            var mainWindow = Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _host?.StopAsync().GetAwaiter().GetResult();
            _host?.Dispose();
            base.OnExit(e);
        }

        // -----------------------------
        // Logging
        // -----------------------------
        private static void ConfigureLogging(ILoggingBuilder logging)
        {
            logging.ClearProviders();
            logging.AddDebug(); // Visual Studio Output window
            logging.SetMinimumLevel(LogLevel.Information);
        }

        // -----------------------------
        // Dependency Injection
        // -----------------------------
        private static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
        {
            Debug.WriteLine("=== ConfigureServices called ===");

            IConfiguration configuration = BuildConfiguration();
            services.AddSingleton(configuration);
            services.AddSingleton<IConfiguration>(configuration);

            // AppSettings
            services.AddSingleton<IAppSettingsService>(provider =>
            {
                var config = provider.GetRequiredService<IConfiguration>();
                var settings = new AppSettings();
                config.GetSection("AppSettings").Bind(settings);
                return settings;
            });
            services.AddSingleton<IAuthSession, AuthSession>();

            // Infrastructure
            services.AddDbContext<ApplicationDbContext>((provider, options) =>
            {
                var settings = provider.GetRequiredService<IAppSettingsService>();
                options.UseSqlServer(settings.ConnectionString);
            });
            
            // HTTP - FIXED: Only IApiService uses AddHttpClient!
            services.AddHttpClient<IApiService, ApiService>();
            services.AddScoped<IPatientHttpClient, PatientHttpClient>();  // Uses IApiService internally
            services.AddSingleton<IConnectionService, Core.Services.ConnectionService>();

            // Repositories
            services.Scan(scan => scan
                .FromAssembliesOf(typeof(IPatientRepository), typeof(PatientRepository))
                .AddClasses(c => c.Where(t => t.Name.EndsWith("Repository")))
                .AsImplementedInterfaces()
                .WithScopedLifetime());

            // Core Services
            services.Scan(scan => scan
                .FromAssembliesOf(typeof(IUserService), typeof(UserService))
                .AddClasses(c => c.Where(t => t.Name.EndsWith("Service")))
                .AsImplementedInterfaces()
                .WithScopedLifetime());

            services.AddScoped<IVisitService, VisitService>();

            // UI
            services.AddTransient<MainWindow>();
            services.AddTransient<MainWindowViewModel>();
            services.AddTransient<RegisterPatientWindow>();
            services.AddTransient<RegisterPatientViewModel>();
            services.AddTransient<SettingsWindow>();
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<DebugWindow>();
            services.AddSingleton<IVisitMapper, VisitMapper>();

            Debug.WriteLine("=== ConfigureServices completed ===");
        }

        // -----------------------------
        // Configuration
        // -----------------------------
        private static IConfiguration BuildConfiguration()
        {
            try
            {
                var currentDir = Directory.GetCurrentDirectory();
                Debug.WriteLine($"Current directory: {currentDir}");

                var config = new ConfigurationBuilder()
                    .SetBasePath(currentDir)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
                    .Build();

                var apiUrl = config["AppSettings:ApiBaseUrl"];
                Debug.WriteLine($"API Base URL from config: {apiUrl}");

                return config;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"BuildConfiguration EXCEPTION: {ex.Message}");
                throw;
            }
        }
    }
}
