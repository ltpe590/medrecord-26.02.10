using Core.AI;
using Core.Configuration;
using Core.Data.Context;
using Core.Http;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Profiles;
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
using WPF.Services;
using WPF.ViewModels;
using WPF.Views;
using WPF.Windows;
using FellowOakDicom;
using FellowOakDicom.Imaging;

namespace WPF
{
    public partial class App : Application
    {
        private static IHost? _host;
        private static string _logFile = Path.Combine(AppContext.BaseDirectory, "logs", $"startup_{DateTime.Now:yyyyMMdd_HHmmss}.log");

        public static IServiceProvider Services
            => _host?.Services ?? throw new InvalidOperationException("Host not initialized");

        private static void Log(string message)
        {
            var msg = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
            Debug.WriteLine(msg);
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_logFile)!);
                File.AppendAllText(_logFile, msg + Environment.NewLine);
            }
            catch { /* Ignore file logging errors */ }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            Log("=== WPF App OnStartup BEGIN ===");

            try
            {
                base.OnStartup(e);
                Log("✅ base.OnStartup() completed");

                // Global exception handlers - capture unhandled exceptions to a log file for diagnosis
                AppDomain.CurrentDomain.UnhandledException += (s, ev) => LogUnhandledException(ev.ExceptionObject as Exception, "AppDomain.CurrentDomain.UnhandledException");
                this.DispatcherUnhandledException += (s, ev) =>
                {
                    LogUnhandledException(ev.Exception, "Application.DispatcherUnhandledException");
                    // TEMPORARY: Don't handle - let it crash so we can see the error!
                    ev.Handled = false;  // Changed from true to false for debugging
                };
                TaskScheduler.UnobservedTaskException += (s, ev) =>
                {
                    LogUnhandledException(ev.Exception, "TaskScheduler.UnobservedTaskException");
                    ev.SetObserved();
                };
                Log("✅ Exception handlers registered");

                // Debugger.Launch() removed - it blocks the UI startup
                // If you need to attach a debugger, use Debug → Attach to Process in Visual Studio
                // Or start with F5 (Start Debugging) instead of Ctrl+F5

                Log("⏳ Building host...");
                _host = Host.CreateDefaultBuilder()
                    .ConfigureLogging(ConfigureLogging)
                    .ConfigureServices(ConfigureServices)
                    .Build();
                Log("✅ Host built successfully");

                // Apply saved theme before any windows appear
                Log("⏳ Applying saved theme...");
                var appSettings2 = _host.Services.GetRequiredService<Core.Interfaces.Services.IAppSettingsService>();
                WPF.Services.ThemeService.Apply(appSettings2);
                Log("✅ Theme applied");

                Log("⏳ Starting host...");
                _host.Start();
                Log("✅ Host started successfully");

                // Apply saved theme (accent color + dark mode) before any window is shown
                Log("⏳ Applying saved theme...");
                try
                {
                    var appSettings = Services.GetRequiredService<IAppSettingsService>();
                    ThemeService.Apply(appSettings);
                    Log("✅ Theme applied");
                }
                catch (Exception themeEx)
                {
                    Log($"⚠️ Theme apply failed (non-fatal): {themeEx.Message}");
                }

                // CRITICAL: Set ShutdownMode BEFORE showing any windows
                ShutdownMode = ShutdownMode.OnMainWindowClose;
                Log("✅ Set ShutdownMode to OnMainWindowClose");

                // CREATE AND SHOW MAIN WINDOW FIRST (keeps app alive)
                Log("⏳ Resolving MainWindow from DI...");
                var mainWindow = Services.GetRequiredService<MainWindow>();
                Log($"✅ MainWindow resolved: {mainWindow != null}");

                // Set as main window
                MainWindow = mainWindow;
                Log("✅ Set Application.MainWindow");
                
                // Show MainWindow immediately (keeps app alive)
                mainWindow!.WindowState = WindowState.Maximized;
                mainWindow.Show();
                Log("✅ MainWindow shown (app will stay alive now)");

                // NOW SHOW LOGIN WINDOW MODAL ON TOP OF MAINWINDOW
                Log("⏳ Showing LoginWindow as modal dialog...");
                var loginWindow = Services.GetRequiredService<WPF.Windows.LoginWindow>();
                loginWindow.Owner = mainWindow;  // Set MainWindow as owner
                Log("✅ LoginWindow resolved");
                
                var loginResult = loginWindow.ShowDialog();
                Log($"✅ LoginWindow closed. DialogResult: {loginResult}");

                if (loginResult != true || string.IsNullOrEmpty(loginWindow.AuthToken))
                {
                    Log("❌ Login cancelled or failed. Closing MainWindow and shutting down.");
                    mainWindow.Close();  // Close MainWindow, which will trigger shutdown
                    return;
                }

                Log($"✅ Login successful! Auth token length: {loginWindow.AuthToken.Length}");

                // Pass auth token to MainWindow ViewModel (fire-and-forget, don't block UI)
                Log("⏳ Passing auth token to MainWindow ViewModel...");
                if (mainWindow.DataContext is MainWindowViewModel vm)
                {
                    // Don't block - let it load in background
                    _ = vm.SetAuthTokenAndInitializeAsync(loginWindow.AuthToken);
                    Log("✅ Auth token passed (loading patients in background)");

                    // Auto-detect Ollama if it is configured as the active provider
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            var aiSvc = Services.GetRequiredService<IAiService>();
                            if (aiSvc.CurrentSettings.Provider == AiProvider.Ollama &&
                                !string.IsNullOrWhiteSpace(aiSvc.CurrentSettings.OllamaBaseUrl))
                            {
                                Log("⏳ Auto-detecting Ollama...");
                                var probe = await aiSvc.ProbeOllamaAsync(aiSvc.CurrentSettings.OllamaBaseUrl);
                                if (probe.IsAvailable)
                                    Log($"✅ Ollama detected — {probe.Models.Count} model(s): {string.Join(", ", probe.Models)}");
                                else
                                    Log($"⚠️ Ollama not running: {probe.Error}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Log($"⚠️ Ollama auto-detect failed (non-fatal): {ex.Message}");
                        }
                    });
                }
                else
                {
                    Log("❌ WARNING: MainWindow.DataContext is not MainWindowViewModel!");
                }
                
                // MainWindow is already showing, just activate it
                mainWindow.Activate();
                mainWindow.Focus();
                Log("✅ MainWindow activated and ready to use");

                Log("=== WPF App OnStartup COMPLETED SUCCESSFULLY ===");
            }
            catch (Exception ex)
            {
                Log($"❌ EXCEPTION in OnStartup: {ex.GetType().Name}");
                Log($"❌ Message: {ex.Message}");
                Log($"❌ Stack Trace: {ex.StackTrace}");

                // Also log to file
                LogUnhandledException(ex, "OnStartup");

                // Show error to user
                MessageBox.Show(
                    $"Application failed to start:\n\n{ex.Message}\n\nCheck Debug output for details.\n\nLog file: {_logFile}",
                    "Startup Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                // Re-throw so debugger can catch it
                throw;
            }
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
            Debug.WriteLine("=== ConfigureServices START ===");

            try
            {
                Debug.WriteLine("⏳ Building configuration...");
                IConfiguration configuration = BuildConfiguration();
                services.AddSingleton(configuration);
                services.AddSingleton<IConfiguration>(configuration);
                Debug.WriteLine("✅ Configuration registered");

                // AppSettings
                Debug.WriteLine("⏳ Registering AppSettings...");
                services.AddSingleton<IAppSettingsService>(provider =>
                {
                    var config = provider.GetRequiredService<IConfiguration>();
                    var settings = new AppSettings();
                    config.GetSection("AppSettings").Bind(settings);
                    Debug.WriteLine($"   API Base URL from config: {settings.ApiBaseUrl}");
                    return settings;
                });
                services.AddSingleton<IAuthSession, AuthSession>();
                Debug.WriteLine("✅ AppSettings registered");

                // AI service — singleton so provider switch in Settings is immediately visible everywhere
                services.AddSingleton<IAiService>(provider =>
                {
                    var s = provider.GetRequiredService<IAppSettingsService>();
                    var settings = new AiSettings
                    {
                        Provider      = Enum.TryParse<AiProvider>(s.AiProvider, out var p) ? p : AiProvider.None,
                        ClaudeApiKey  = s.ClaudeApiKey,
                        ClaudeModel   = s.ClaudeModel,
                        OpenAiApiKey  = s.OpenAiApiKey,
                        OpenAiModel   = s.OpenAiModel,
                        OllamaBaseUrl = s.OllamaBaseUrl,
                        OllamaModel   = s.OllamaModel,
                    };
                    return new AiService(settings);
                });
                Debug.WriteLine("✅ IAiService (AiService) registered as singleton");

                // Infrastructure
                Debug.WriteLine("⏳ Registering DbContext...");
                services.AddDbContext<ApplicationDbContext>((provider, options) =>
                {
                    var settings = provider.GetRequiredService<IAppSettingsService>();
                    options.UseSqlServer(settings.ConnectionString);
                });
                Debug.WriteLine("✅ DbContext registered");

                // HTTP
                Debug.WriteLine("⏳ Registering HTTP services...");
                services.AddHttpClient<IApiService, ApiService>();
                services.AddScoped<IPatientHttpClient, PatientHttpClient>();
                services.AddSingleton<IConnectionService, Core.Services.ConnectionService>();
                Debug.WriteLine("✅ HTTP services registered");

                // Specialty profiles - MUST BE REGISTERED FIRST before services that depend on them
                Debug.WriteLine("⏳ Registering ISpecialtyProfile (ObGyneProfile)...");
                services.AddSingleton<ISpecialtyProfile, ObGyneProfile>();
                Debug.WriteLine("✅ ISpecialtyProfile registered");

                // Repositories
                Debug.WriteLine("⏳ Scanning and registering Repositories...");
                services.Scan(scan => scan
                    .FromAssembliesOf(typeof(IPatientRepository), typeof(PatientRepository))
                    .AddClasses(c => c.Where(t => t.Name.EndsWith("Repository")))
                    .AsImplementedInterfaces()
                    .WithScopedLifetime());
                Debug.WriteLine("✅ Repositories registered");

                // Core Services
                Debug.WriteLine("⏳ Scanning and registering Services...");
                services.Scan(scan => scan
                    .FromAssembliesOf(typeof(IUserService), typeof(UserService))
                    .AddClasses(c => c.Where(t => t.Name.EndsWith("Service") && t != typeof(Core.AI.AiService) && t != typeof(WPF.Services.VoiceDictationService)))
                    .AsImplementedInterfaces()
                    .WithScopedLifetime());
                Debug.WriteLine("✅ Services scanned and registered");

                Debug.WriteLine("⏳ Registering IVisitService explicitly...");
                services.AddScoped<IVisitService, VisitService>();
                Debug.WriteLine("✅ IVisitService registered");

                // UI
                Debug.WriteLine("⏳ Registering UI components...");
                
                // Login components
                services.AddTransient<WPF.Services.IBiometricService, WPF.Services.BiometricService>();
                services.AddSingleton<WPF.Services.VoiceDictationService>();
                services.AddTransient<LoginViewModel>();
                services.AddTransient<WPF.Windows.LoginWindow>();
                Debug.WriteLine("✅ Login components registered");
                
                // Main window components
                services.AddTransient<MainWindow>(provider => new MainWindow(
                    provider.GetRequiredService<MainWindowViewModel>(),
                    provider.GetRequiredService<WPF.Services.VoiceDictationService>(),
                    provider.GetRequiredService<IAiService>()
                ));
                services.AddTransient<MainWindowViewModel>();
                services.AddTransient<VisitPageViewModel>();
                services.AddTransient<RegisterPatientWindow>();
                services.AddTransient<RegisterPatientViewModel>();
                services.AddTransient<SettingsWindow>(provider => new SettingsWindow(
                    provider.GetRequiredService<SettingsViewModel>(),
                    provider.GetRequiredService<ILoggerFactory>().CreateLogger<SettingsWindow>()
                ));
                services.AddTransient<SettingsViewModel>(provider => new SettingsViewModel(
                    provider.GetRequiredService<IAppSettingsService>(),
                    provider.GetRequiredService<IConnectionService>(),
                    provider.GetRequiredService<IUserService>(),
                    provider.GetRequiredService<ILogger<SettingsViewModel>>(),
                    provider.GetRequiredService<IAiService>()
                ));
                services.AddTransient<DebugWindow>();
                services.AddSingleton<IVisitMapper, VisitMapper>();
                Debug.WriteLine("✅ UI components registered");

                Debug.WriteLine("=== ConfigureServices COMPLETED ===");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ EXCEPTION in ConfigureServices: {ex.GetType().Name}");
                Debug.WriteLine($"❌ Message: {ex.Message}");
                Debug.WriteLine($"❌ Stack Trace: {ex.StackTrace}");
                throw;
            }
        }

        // -----------------------------
        // Configuration
        // -----------------------------
        private static IConfiguration BuildConfiguration()
        {
            try
            {
                var currentDir = AppContext.BaseDirectory;
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

        private static void LogUnhandledException(Exception? ex, string source)
        {
            try
            {
                var baseDir = AppContext.BaseDirectory;
                var logs = Path.Combine(baseDir, "logs");
                Directory.CreateDirectory(logs);
                var file = Path.Combine(logs, $"crash_{DateTime.Now:yyyyMMdd_HHmmss}.log");

                using (var sw = new StreamWriter(file, false))
                {
                    sw.WriteLine($"Source: {source}");
                    sw.WriteLine($"Time: {DateTime.Now:O}");
                    if (ex != null)
                    {
                        sw.WriteLine("Exception: ");
                        sw.WriteLine(ex.ToString());
                    }
                    else
                    {
                        sw.WriteLine("Exception object was null.");
                    }
                }

                Debug.WriteLine($"Unhandled exception logged to: {file}");
            }
            catch
            {
                // Swallow any logging failures to avoid escalation during crash handling
            }
        }
    }
}


