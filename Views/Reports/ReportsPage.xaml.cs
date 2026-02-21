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
        
        // Subscribe to ViewModel property changes
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        
        Loaded += ReportsPage_Loaded;
    }

    private void ReportsPage_Loaded(object sender, RoutedEventArgs e)
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
            e.PropertyName == nameof(ViewModel.ReportData) ||
            e.PropertyName == nameof(ViewModel.HighestExpenseLabel) ||
            e.PropertyName == nameof(ViewModel.DateRangeLabel))
        {
            DispatcherQueue.TryEnqueue(() => UpdateUI());
        }
    }

    private void UpdateUI()
    {
        try
        {
            // Update summary cards
            TotalAmountText.Text = ViewModel.TotalAmountFormatted ?? ViewModel.FormatZero;
            TransactionCountText.Text = ViewModel.TransactionCount.ToString();
            AverageAmountText.Text = ViewModel.AverageAmountFormatted ?? ViewModel.FormatZero;

            // Update insight cards
            HighestExpenseName.Text = ViewModel.HighestExpenseLabel ?? "N/A";
            HighestExpenseAmount.Text = ViewModel.HighestExpenseAmountFormatted ?? ViewModel.FormatZero;
            LowestExpenseName.Text = ViewModel.LowestExpenseLabel ?? "N/A";
            LowestExpenseAmount.Text = ViewModel.LowestExpenseAmountFormatted ?? ViewModel.FormatZero;
            TopCategoryName.Text = ViewModel.MostFrequentCategory ?? "N/A";
            TopCategoryCount.Text = ViewModel.MostFrequentCategoryCount > 0 
                ? $"{ViewModel.MostFrequentCategoryCount} transactions" 
                : "";
            DateRangeText.Text = ViewModel.DateRangeLabel ?? "";

            // Show/hide states
            if (ViewModel.ReportData != null && ViewModel.ReportData.Count > 0)
            {
                EmptyState.Visibility = Visibility.Collapsed;
                ReportDataScroller.Visibility = Visibility.Visible;
                InsightsPanel.Visibility = Visibility.Visible;
                BuildReportItems();
            }
            else
            {
                EmptyState.Visibility = Visibility.Visible;
                ReportDataScroller.Visibility = Visibility.Collapsed;
                InsightsPanel.Visibility = Visibility.Collapsed;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ReportsPage] Error updating UI: {ex.Message}");
        }
    }

    private void BuildReportItems()
    {
        ReportDataContainer.Children.Clear();
        
        // Color palette for bars
        var barBrushes = new Microsoft.UI.Xaml.Media.Brush[]
        {
            (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["PrimaryGradientBrush"],
            (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["SuccessGradientBrush"],
            (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["WarningGradientBrush"],
            (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["PurpleGradientBrush"],
            (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["ErrorGradientBrush"],
        };

        int colorIndex = 0;
        foreach (var item in ViewModel.ReportData)
        {
            var border = new Border
            {
                Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["AcrylicInAppFillColorDefaultBrush"],
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(16),
                Margin = new Thickness(0, 0, 0, 4),
            };

            var outerStack = new StackPanel { Spacing = 10 };

            // Top row: label, count, amount
            var topGrid = new Grid { ColumnSpacing = 12 };
            topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var label = new TextBlock
            {
                Text = item.Label,
                FontSize = 14,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            Grid.SetColumn(label, 0);

            var countBadge = new Border
            {
                Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CardBackgroundFillColorSecondaryBrush"],
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(8, 2, 8, 2),
                VerticalAlignment = VerticalAlignment.Center
            };
            countBadge.Child = new TextBlock
            {
                Text = $"{item.Count} txn",
                FontSize = 11,
                Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
            };
            Grid.SetColumn(countBadge, 1);

            var amount = new TextBlock
            {
                Text = item.AmountFormatted,
                FontSize = 16,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["PrimaryBrush"],
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(amount, 2);

            topGrid.Children.Add(label);
            topGrid.Children.Add(countBadge);
            topGrid.Children.Add(amount);
            outerStack.Children.Add(topGrid);

            // Percentage bar
            if (item.Percentage > 0)
            {
                var barGrid = new Grid { ColumnSpacing = 8 };
                barGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                barGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var barBackground = new Border
                {
                    Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CardBackgroundFillColorSecondaryBrush"],
                    CornerRadius = new CornerRadius(4),
                    Height = 8,
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };

                var barContainer = new Grid();
                barContainer.Children.Add(barBackground);

                var barFill = new Border
                {
                    Background = barBrushes[colorIndex % barBrushes.Length],
                    CornerRadius = new CornerRadius(4),
                    Height = 8,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Width = 0 // Set via binding below
                };

                // Use loaded event to set width based on actual container width
                barContainer.Loaded += (s, ev) =>
                {
                    if (s is Grid g && g.ActualWidth > 0)
                    {
                        barFill.Width = g.ActualWidth * item.Percentage / 100.0;
                    }
                };
                barContainer.SizeChanged += (s, ev) =>
                {
                    if (s is Grid g && g.ActualWidth > 0)
                    {
                        barFill.Width = g.ActualWidth * item.Percentage / 100.0;
                    }
                };

                barContainer.Children.Add(barFill);
                Grid.SetColumn(barContainer, 0);

                var pctText = new TextBlock
                {
                    Text = $"{item.Percentage:F1}%",
                    FontSize = 11,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
                    VerticalAlignment = VerticalAlignment.Center,
                    MinWidth = 42,
                    TextAlignment = TextAlignment.Right
                };
                Grid.SetColumn(pctText, 1);

                barGrid.Children.Add(barContainer);
                barGrid.Children.Add(pctText);
                outerStack.Children.Add(barGrid);
            }

            border.Child = outerStack;
            ReportDataContainer.Children.Add(border);
            colorIndex++;
        }
    }

    private void ReportType_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ViewModel == null) return;
        if (sender is ComboBox combo && combo.SelectedItem is ComboBoxItem item)
        {
            var reportType = item.Tag?.ToString() ?? "Overall";
            ViewModel.SelectedReportType = reportType switch
            {
                "ByProject" => ReportType.ByProject,
                "ByProjectGroup" => ReportType.ByProjectGroup,
                "ByPaymentMode" => ReportType.ByPaymentMode,
                "ByContact" => ReportType.ByContact,
                "ByVendor" => ReportType.ByVendor,
                "ByExpenseType" => ReportType.ByExpenseType,
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

    private void StartDate_Changed(object sender, DatePickerValueChangedEventArgs e)
    {
        ViewModel.StartDate = e.NewDate.DateTime;
    }

    private void EndDate_Changed(object sender, DatePickerValueChangedEventArgs e)
    {
        ViewModel.EndDate = e.NewDate.DateTime;
    }
}
