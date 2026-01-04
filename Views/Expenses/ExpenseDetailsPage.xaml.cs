using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Extensions.DependencyInjection;
using Expense_Flow.Models;
using Expense_Flow.Services;
using System;
using System.IO;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace Expense_Flow.Views.Expenses;

public sealed partial class ExpenseDetailsPage : Page
{
    private Expense? _expense;
    private readonly IExpenseService _expenseService;
    private readonly IFileStorageService _fileStorageService;
    private readonly ISettingsService _settingsService;

    public ExpenseDetailsPage()
    {
        InitializeComponent();
        _expenseService = App.Host!.Services.GetRequiredService<IExpenseService>();
        _fileStorageService = App.Host!.Services.GetRequiredService<IFileStorageService>();
        _settingsService = App.Host!.Services.GetRequiredService<ISettingsService>();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is int expenseId)
        {
            var result = await _expenseService.GetExpenseByIdAsync(expenseId);
            if (result.Success && result.Data != null)
            {
                _expense = result.Data;
                LoadExpenseDetails();
            }
        }
    }

    private async void LoadExpenseDetails()
    {
        if (_expense == null) return;

        var currencySymbol = _settingsService.GetCurrencySymbol();

        // Header
        ExpenseNameText.Text = _expense.Name;

        // Amount
        AmountText.Text = $"{currencySymbol}{_expense.Amount:N2}";

        // Basic Information
        ExpenseTypeText.Text = _expense.ExpenseType?.Name ?? "N/A";
        ProjectText.Text = _expense.Project?.Name ?? "N/A";
        PaymentDateText.Text = _expense.PaymentDate.ToString("MMM dd, yyyy");
        CreatedAtText.Text = _expense.CreatedAt.ToString("MMM dd, yyyy hh:mm tt");
        DescriptionText.Text = string.IsNullOrWhiteSpace(_expense.Description) 
            ? "No description provided" 
            : _expense.Description;

        // Payment Details
        PaymentAmountText.Text = $"{currencySymbol}{_expense.PaymentAmount:N2}";
        PaymentModeText.Text = _expense.PaymentMode?.Name ?? "N/A";

        // Invoice Details
        if (_expense.HasInvoice)
        {
            InvoiceCard.Visibility = Visibility.Visible;
            InvoiceNumberText.Text = _expense.InvoiceNumber ?? "N/A";
            InvoiceDateText.Text = _expense.InvoiceDate?.ToString("MMM dd, yyyy") ?? "N/A";

            if (_expense.BillingPeriodStart.HasValue)
            {
                BillingStartPanel.Visibility = Visibility.Visible;
                BillingStartText.Text = _expense.BillingPeriodStart.Value.ToString("MMM dd, yyyy");
            }

            if (_expense.BillingPeriodEnd.HasValue)
            {
                BillingEndPanel.Visibility = Visibility.Visible;
                BillingEndText.Text = _expense.BillingPeriodEnd.Value.ToString("MMM dd, yyyy");
            }
        }

        // Subscription/Vendor
        if (_expense.SubscriptionId.HasValue && _expense.Subscription != null)
        {
            SubscriptionCard.Visibility = Visibility.Visible;
            SubscriptionText.Text = _expense.Subscription.Name;
        }

        // File Viewer
        if (_expense.HasInvoiceFile && _expense.InvoiceFileGuid.HasValue)
        {
            FileViewerCard.Visibility = Visibility.Visible;
            await LoadInvoiceFile(_expense.InvoiceFileGuid.Value, _expense.InvoiceFileName);
        }
    }

    private async System.Threading.Tasks.Task LoadInvoiceFile(Guid fileGuid, string? fileName)
    {
        try
        {
            var result = await _fileStorageService.GetInvoiceFileAsync(fileGuid);
            if (!result.Success || result.FileStream == null)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load invoice file: {result.ErrorMessage}");
                return;
            }

            var extension = Path.GetExtension(fileName)?.ToLower();
            System.Diagnostics.Debug.WriteLine($"Loading file with extension: {extension}, filename: {fileName}");

            if (extension == ".jpg" || extension == ".jpeg" || extension == ".png")
            {
                // Display image
                InvoiceImage.Visibility = Visibility.Visible;
                PdfPlaceholder.Visibility = Visibility.Collapsed;

                using (var stream = result.FileStream.AsRandomAccessStream())
                {
                    var bitmap = new BitmapImage();
                    await bitmap.SetSourceAsync(stream);
                    InvoiceImage.Source = bitmap;
                }
            }
            else if (extension == ".pdf")
            {
                // Show PDF placeholder
                InvoiceImage.Visibility = Visibility.Collapsed;
                PdfPlaceholder.Visibility = Visibility.Visible;
                PdfFileName.Text = fileName ?? "document.pdf";
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Unsupported file extension: {extension}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading file: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    private async void DownloadFile_Click(object sender, RoutedEventArgs e)
    {
        if (_expense == null || !_expense.HasInvoiceFile || !_expense.InvoiceFileGuid.HasValue)
            return;

        try
        {
            var picker = new FileSavePicker();
            picker.SuggestedStartLocation = PickerLocationId.Downloads;
            
            var extension = Path.GetExtension(_expense.InvoiceFileName)?.ToLower() ?? ".pdf";
            picker.FileTypeChoices.Add("Invoice File", new[] { extension });
            picker.SuggestedFileName = _expense.InvoiceFileName ?? $"invoice_{_expense.Id}{extension}";

            var window = (Microsoft.UI.Xaml.Application.Current as App)?.Window;
            if (window != null)
            {
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            }

            var file = await picker.PickSaveFileAsync();
            if (file == null) return;

            var result = await _fileStorageService.GetInvoiceFileAsync(_expense.InvoiceFileGuid.Value);
            if (result.Success && result.FileStream != null)
            {
                using var fileStream = await file.OpenStreamForWriteAsync();
                await result.FileStream.CopyToAsync(fileStream);

                var successDialog = new ContentDialog
                {
                    Title = "Success",
                    Content = "File downloaded successfully!",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await successDialog.ShowAsync();
            }
        }
        catch (Exception ex)
        {
            var errorDialog = new ContentDialog
            {
                Title = "Error",
                Content = $"Failed to download file: {ex.Message}",
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await errorDialog.ShowAsync();
        }
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        if (Frame.CanGoBack)
        {
            Frame.GoBack();
        }
    }

    private async void EditExpense_Click(object sender, RoutedEventArgs e)
    {
        if (_expense == null) return;

        var dialog = new ExpenseDialog(_expense)
        {
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        
        if (result == ContentDialogResult.Primary && dialog.Expense != null)
        {
            var updateResult = await _expenseService.UpdateExpenseAsync(dialog.Expense);
            if (updateResult.Success)
            {
                _expense = dialog.Expense;
                LoadExpenseDetails();
            }
        }
    }

    private async void DeleteExpense_Click(object sender, RoutedEventArgs e)
    {
        if (_expense == null) return;

        var confirmDialog = new ContentDialog
        {
            Title = "Delete Expense",
            Content = $"Are you sure you want to delete '{_expense.Name}'? This action cannot be undone.",
            PrimaryButtonText = "Delete",
            SecondaryButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Secondary,
            XamlRoot = this.XamlRoot
        };

        var result = await confirmDialog.ShowAsync();
        
        if (result == ContentDialogResult.Primary)
        {
            var deleteResult = await _expenseService.DeleteExpenseAsync(_expense.Id);
            if (deleteResult.Success)
            {
                if (Frame.CanGoBack)
                {
                    Frame.GoBack();
                }
            }
        }
    }
}
