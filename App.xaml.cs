using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Expense_Flow.Data;
using Expense_Flow.Services;
using Expense_Flow.ViewModels;

namespace Expense_Flow
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private Window? _window;
        public static IHost? Host { get; private set; }

        public Window? Window => _window;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();
            
            Host = Microsoft.Extensions.Hosting.Host
                .CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    var dbPath = System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "ExpenseFlow",
                        "expenseflow.db"
                    );

                    services.AddDbContext<ExpenseFlowDbContext>(options =>
                        options.UseSqlite($"Data Source={dbPath}"));

                    services.AddSingleton<DatabaseService>();
                    services.AddSingleton<ISettingsService, SettingsService>();
                    services.AddTransient(typeof(IRepository<>), typeof(Repository<>));
                    services.AddSingleton<IUserService, UserService>();
                    services.AddSingleton<IUserProfileService, UserProfileService>();
                    services.AddSingleton<INavigationService, NavigationService>();
                    services.AddTransient<IContactService, ContactService>();
                    services.AddTransient<ISubscriptionService, SubscriptionService>();
                    services.AddTransient<IPaymentModeService, PaymentModeService>();
                    services.AddTransient<IProjectService, ProjectService>();
                    services.AddTransient<IProjectGroupService, ProjectGroupService>();
                    services.AddTransient<IExpenseService, ExpenseService>();
                    services.AddTransient<IExpenseTypeService, ExpenseTypeService>();
                    services.AddSingleton<IFileStorageService, FileStorageService>();

                    services.AddTransient<DashboardViewModel>();
                    services.AddTransient<ExpensesViewModel>();
                    services.AddTransient<ProjectsViewModel>();
                    services.AddTransient<ContactsViewModel>();
                    services.AddTransient<PaymentModesViewModel>();
                    services.AddTransient<SubscriptionsViewModel>();
                    services.AddTransient<ReportsViewModel>();
                    services.AddTransient<SettingsViewModel>();
                })
                .Build();
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            try
            {
                var databaseService = Host!.Services.GetRequiredService<DatabaseService>();
                await databaseService.InitializeAsync();

                _window = new MainWindow();
                _window.Activate();
            }
            catch (Exception ex)
            {
                // Log the error and show a message
                System.Diagnostics.Debug.WriteLine($"Application startup error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                
                // Try to show error window
                try
                {
                    var errorWindow = new Window
                    {
                        Title = "Startup Error"
                    };
                    
                    var errorPanel = new StackPanel
                    {
                        Padding = new Microsoft.UI.Xaml.Thickness(20),
                        Spacing = 10
                    };
                    
                    errorPanel.Children.Add(new TextBlock
                    {
                        Text = "Application Startup Error",
                        FontSize = 20,
                        FontWeight = Microsoft.UI.Text.FontWeights.Bold
                    });
                    
                    errorPanel.Children.Add(new TextBlock
                    {
                        Text = ex.Message,
                        TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap
                    });
                    
                    errorPanel.Children.Add(new TextBlock
                    {
                        Text = "Please check the debug output for more details.",
                        FontSize = 12,
                        Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray)
                    });
                    
                    errorWindow.Content = errorPanel;
                    errorWindow.Activate();
                }
                catch
                {
                    // If we can't even show an error window, just exit
                    System.Environment.Exit(1);
                }
            }
        }
    }
}
