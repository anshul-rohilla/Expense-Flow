using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using Expense_Flow.ViewModels;
using System;

namespace Expense_Flow.Views.Reports;

public sealed partial class ReportsPage : Page
{
    public ReportsViewModel ViewModel { get; }

    public ReportsPage()
    {
        InitializeComponent();
        ViewModel = App.Host!.Services.GetRequiredService<ReportsViewModel>();
        DataContext = ViewModel;
        
        // Subscribe to ViewModel property changes
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        
        Loaded += ReportsPage_Loaded;
    }

    private void ReportsPage_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        UpdateUI();
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.TotalAmount) ||
            e.PropertyName == nameof(ViewModel.TotalAmountFormatted) ||
            e.PropertyName == nameof(ViewModel.TransactionCount) ||
            e.PropertyName == nameof(ViewModel.AverageAmount) ||
            e.PropertyName == nameof(ViewModel.AverageAmountFormatted) ||
            e.PropertyName == nameof(ViewModel.ReportData))
        {
            DispatcherQueue.TryEnqueue(() => UpdateUI());
        }
    }

    private void UpdateUI()
    {
        try
        {
            // Update summary cards with formatted values
            TotalAmountText.Text = ViewModel.TotalAmountFormatted ?? "?0.00";
            TransactionCountText.Text = ViewModel.TransactionCount.ToString();
            AverageAmountText.Text = ViewModel.AverageAmountFormatted ?? "?0.00";

            // Show/hide empty state
            if (ViewModel.ReportData != null && ViewModel.ReportData.Count > 0)
            {
                EmptyState.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
                ReportDataScroller.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                ReportDataRepeater.ItemsSource = ViewModel.ReportData;
            }
            else
            {
                EmptyState.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                ReportDataScroller.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ReportsPage] Error updating UI: {ex.Message}");
        }
    }

    private void ReportType_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox combo && combo.SelectedItem is ComboBoxItem item)
        {
            var reportType = item.Tag?.ToString() ?? "Overall";
            ViewModel.SelectedReportType = reportType switch
            {
                "ByProject" => ReportType.ByProject,
                "ByProjectGroup" => ReportType.ByProjectGroup,
                "ByPaymentMode" => ReportType.ByPaymentMode,
                "ByContact" => ReportType.ByContact,
                _ => ReportType.Overall
            };
        }
    }

    private async void GenerateReport_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        LoadingRing.IsActive = true;
        LoadingRing.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
        
        await ViewModel.GenerateReportCommand.ExecuteAsync(null);
        
        LoadingRing.IsActive = false;
        LoadingRing.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
    }

    private async void ExportReport_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await ViewModel.ExportReportCommand.ExecuteAsync(null);
        
        var dialog = new ContentDialog
        {
            Title = "Export Report",
            Content = "Report export functionality will be available in the next update!",
            CloseButtonText = "OK",
            XamlRoot = this.XamlRoot
        };
        await dialog.ShowAsync();
    }
}
