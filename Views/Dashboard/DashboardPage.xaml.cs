using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using Expense_Flow.ViewModels;
using System;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml;
using Expense_Flow.Models;

namespace Expense_Flow.Views.Dashboard;

public sealed partial class DashboardPage : Page
{
    public DashboardViewModel ViewModel { get; }

    public DashboardPage()
    {
        InitializeComponent();
        ViewModel = App.Host!.Services.GetRequiredService<DashboardViewModel>();
        DataContext = ViewModel;
        
        // Subscribe to collection changes
        ViewModel.RecentExpenses.CollectionChanged += RecentExpenses_CollectionChanged;
        
        // Set the default date range in the ComboBox to match ViewModel default
        Loaded += DashboardPage_Loaded;
    }

    private void DashboardPage_Loaded(object sender, RoutedEventArgs e)
    {
        // Ensure ComboBox selection matches ViewModel default (Last30Days = index 2)
        if (DateRangeComboBox.SelectedIndex != 2)
        {
            DateRangeComboBox.SelectedIndex = 2;
        }
    }

    private void RecentExpenses_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        UpdateExpenseListVisibility();
    }

    private void UpdateExpenseListVisibility()
    {
        if (ViewModel.RecentExpenses.Count > 0)
        {
            ExpenseListScroller.Visibility = Visibility.Visible;
            ExpenseEmptyState.Visibility = Visibility.Collapsed;
        }
        else
        {
            ExpenseListScroller.Visibility = Visibility.Collapsed;
            ExpenseEmptyState.Visibility = Visibility.Visible;
        }
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        
        try
        {
            // Load all dashboard data every time we navigate to this page
            await ViewModel.LoadDashboardDataCommand.ExecuteAsync(null);
            
            // Update visibility after data loads
            UpdateExpenseListVisibility();
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

    private void DateRangeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Avoid null reference during initial load
        if (ViewModel == null || sender is not ComboBox comboBox || comboBox.SelectedItem is not ComboBoxItem item)
            return;

        var tag = item.Tag?.ToString();
        if (string.IsNullOrEmpty(tag))
            return;

        ViewModel.SelectedDateRange = tag switch
        {
            "CurrentMonth" => ViewModels.DateRangeFilter.CurrentMonth,
            "PreviousMonth" => ViewModels.DateRangeFilter.PreviousMonth,
            "Last30Days" => ViewModels.DateRangeFilter.Last30Days,
            "Last2Months" => ViewModels.DateRangeFilter.Last2Months,
            "Last3Months" => ViewModels.DateRangeFilter.Last3Months,
            "Last1Year" => ViewModels.DateRangeFilter.Last1Year,
            _ => ViewModels.DateRangeFilter.Last30Days
        };
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

    private void ExpenseItem_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is Expense expense)
        {
            // Navigate to the project details page
            var shellPage = FindShellPage();
            if (shellPage != null)
            {
                // Use the dedicated method to navigate to project details
                shellPage.NavigateToProjectDetails(expense.ProjectId);
            }
        }
    }

    private void ExpenseCard_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is Controls.ExpenseCard card && card.Expense != null)
        {
            Frame.Navigate(typeof(Expenses.ExpenseDetailsPage), card.Expense.Id);
        }
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
