using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using Expense_Flow.ViewModels;
using System;

namespace Expense_Flow.Views.Dashboard;

public sealed partial class DashboardPage : Page
{
    public DashboardViewModel ViewModel { get; }

    public DashboardPage()
    {
        InitializeComponent();
        ViewModel = App.Host!.Services.GetRequiredService<DashboardViewModel>();
        DataContext = ViewModel;
        Loaded += DashboardPage_Loaded;
    }

    private async void DashboardPage_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        try
        {
            // Load all dashboard data
            await ViewModel.LoadDashboardDataCommand.ExecuteAsync(null);
        }
        catch (Exception ex)
        {
            ShowErrorDialog($"Error loading dashboard: {ex.Message}");
        }
    }

    private void RefreshButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _ = ViewModel.LoadDashboardDataCommand.ExecuteAsync(null);
    }

    private void AddExpense_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var shellPage = FindShellPage();
        shellPage?.SelectNavigationItemByTag("Expenses");
    }

    private void NewProject_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var shellPage = FindShellPage();
        shellPage?.SelectNavigationItemByTag("Projects");
    }

    private void ViewReports_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var shellPage = FindShellPage();
        shellPage?.SelectNavigationItemByTag("Reports");
    }

    private void ViewAllExpenses_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var shellPage = FindShellPage();
        shellPage?.SelectNavigationItemByTag("Expenses");
    }

    private Shell.ShellPage? FindShellPage()
    {
        var parent = this.Parent;
        while (parent != null)
        {
            if (parent is Frame frame && frame.Parent is Microsoft.UI.Xaml.Controls.NavigationView navView && navView.Parent is Microsoft.UI.Xaml.Controls.Grid grid && grid.Parent is Shell.ShellPage shellPage)
            {
                return shellPage;
            }
            parent = (parent as Microsoft.UI.Xaml.FrameworkElement)?.Parent;
        }
        return null;
    }

    private async void ShowErrorDialog(string message)
    {
        var errorDialog = new ContentDialog
        {
            Title = "Error",
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = this.XamlRoot,
            Style = Microsoft.UI.Xaml.Application.Current.Resources["ModernContentDialogStyle"] as Microsoft.UI.Xaml.Style
        };
        await errorDialog.ShowAsync();
    }
}
