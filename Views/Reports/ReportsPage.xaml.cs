using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using Expense_Flow.ViewModels;
using System;
using Microsoft.UI.Xaml;

namespace Expense_Flow.Views.Reports;

public sealed partial class ReportsPage : Page
{
    public ReportsViewModel ViewModel { get; }

    public ReportsPage()
    {
        InitializeComponent();
        ViewModel = App.Host!.Services.GetRequiredService<ReportsViewModel>();
        DataContext = ViewModel;
        Loaded += ReportsPage_Loaded;
        
        // Subscribe to property changes
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // Update UI based on property changes
        if (e.PropertyName == nameof(ViewModel.TotalAmount))
        {
            TotalAmountText.Text = $"${ViewModel.TotalAmount:N2}";
        }
        else if (e.PropertyName == nameof(ViewModel.TransactionCount))
        {
            TransactionCountText.Text = ViewModel.TransactionCount.ToString();
        }
        else if (e.PropertyName == nameof(ViewModel.AverageAmount))
        {
            AverageAmountText.Text = $"${ViewModel.AverageAmount:N2}";
        }
        else if (e.PropertyName == nameof(ViewModel.IsBusy))
        {
            LoadingRing.IsActive = ViewModel.IsBusy;
            LoadingRing.Visibility = ViewModel.IsBusy ? Visibility.Visible : Visibility.Collapsed;
        }
        else if (e.PropertyName == nameof(ViewModel.ReportData))
        {
            UpdateReportDataVisibility();
        }
    }

    private void UpdateReportDataVisibility()
    {
        if (ViewModel.ReportData.Count > 0)
        {
            EmptyState.Visibility = Visibility.Collapsed;
            ReportDataScroller.Visibility = Visibility.Visible;
            ReportDataRepeater.ItemsSource = ViewModel.ReportData;
        }
        else
        {
            EmptyState.Visibility = Visibility.Visible;
            ReportDataScroller.Visibility = Visibility.Collapsed;
        }
    }

    private async void ReportsPage_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        try
        {
            // Initialize report cards
            TotalAmountText.Text = "$0.00";
            TransactionCountText.Text = "0";
            AverageAmountText.Text = "$0.00";
            UpdateReportDataVisibility();
        }
        catch (Exception ex)
        {
            ShowErrorDialog($"Error loading reports page: {ex.Message}");
        }
    }

    private async void GenerateReport_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        try
        {
            await ViewModel.GenerateReportCommand.ExecuteAsync(null);
            UpdateReportDataVisibility();
        }
        catch (Exception ex)
        {
            ShowErrorDialog($"Error generating report: {ex.Message}");
        }
    }

    private void ReportType_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (sender is ComboBox combo && combo.SelectedItem is ComboBoxItem item)
            {
                var reportType = item.Tag?.ToString();
                if (Enum.TryParse<ReportType>(reportType, out var type))
                {
                    ViewModel.SelectedReportType = type;
                }
            }
        }
        catch (Exception ex)
        {
            ShowErrorDialog($"Error changing report type: {ex.Message}");
        }
    }

    private async void ExportReport_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var successDialog = new ContentDialog
        {
            Title = "Export Feature",
            Content = "Export functionality will be available in the next update. Report data will be exported to CSV format.",
            CloseButtonText = "OK",
            XamlRoot = this.XamlRoot,
            Style = Microsoft.UI.Xaml.Application.Current.Resources["ModernContentDialogStyle"] as Microsoft.UI.Xaml.Style
        };
        await successDialog.ShowAsync();
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
