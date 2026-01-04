using Microsoft.UI.Xaml.Controls;
using Expense_Flow.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace Expense_Flow.Views.Expenses;

public sealed partial class ExpenseDialog : ContentDialog
{
    public Expense Expense { get; private set; }
    private bool _isEditMode;
    private Stream? _uploadedFileStream;
    private string? _uploadedFileName;

    public ObservableCollection<Project> Projects { get; } = new();
    public ObservableCollection<PaymentMode> PaymentModes { get; } = new();
    public ObservableCollection<Subscription> Subscriptions { get; } = new();
    public ObservableCollection<ExpenseType> ExpenseTypes { get; } = new();

    public ExpenseDialog(Expense? expense = null)
    {
        InitializeComponent();
        _isEditMode = expense != null;
        Title = _isEditMode ? "Edit Expense" : "Add Expense";
        PrimaryButtonText = _isEditMode ? "Update" : "Create";
        SecondaryButtonText = "Cancel";

        Expense = expense ?? new Expense
        {
            Amount = 0,
            PaymentAmount = 0,
            PaymentDate = DateTime.Now,
            HasInvoice = false
        };
        
        LoadDataAsync();

        // Set initial values if in edit mode
        if (_isEditMode && expense != null)
        {
            NameTextBox.Text = expense.Name ?? string.Empty;
            AmountTextBox.Text = expense.Amount.ToString("0.00");
            DescriptionTextBox.Text = expense.Description ?? string.Empty;
            PaymentAmountTextBox.Text = expense.PaymentAmount.ToString("0.00");
            
            if (expense.PaymentDate != default)
            {
                PaymentDatePicker.Date = expense.PaymentDate;
            }

            HasInvoiceCheckBox.IsChecked = expense.HasInvoice;
            
            if (expense.HasInvoice)
            {
                InvoiceDetailsPanel.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                InvoiceNumberTextBox.Text = expense.InvoiceNumber ?? string.Empty;
                
                if (expense.InvoiceDate.HasValue)
                {
                    InvoiceDatePicker.Date = expense.InvoiceDate.Value;
                }

                if (expense.BillingPeriodStart.HasValue)
                {
                    BillingStartDatePicker.Date = expense.BillingPeriodStart.Value;
                }

                if (expense.BillingPeriodEnd.HasValue)
                {
                    BillingEndDatePicker.Date = expense.BillingPeriodEnd.Value;
                }

                if (expense.HasInvoiceFile)
                {
                    FileNameText.Text = $"?? {expense.InvoiceFileName ?? "Attached file"}";
                }
            }
        }
    }

    private async void LoadDataAsync()
    {
        try
        {
            // Load Expense Types
            var expenseTypeService = App.Host!.Services.GetRequiredService<Services.IExpenseTypeService>();
            var expenseTypesResult = await expenseTypeService.GetAllExpenseTypesAsync();
            
            if (expenseTypesResult.Success && expenseTypesResult.Data != null)
            {
                ExpenseTypes.Clear();
                foreach (var type in expenseTypesResult.Data)
                {
                    ExpenseTypes.Add(type);
                }

                if (_isEditMode && Expense.ExpenseTypeId > 0)
                {
                    ExpenseTypeComboBox.SelectedItem = ExpenseTypes.FirstOrDefault(et => et.Id == Expense.ExpenseTypeId);
                }
                else if (ExpenseTypes.Count > 0)
                {
                    ExpenseTypeComboBox.SelectedIndex = 0;
                }
            }

            // Load Projects
            var projectService = App.Host!.Services.GetRequiredService<Services.IProjectService>();
            var projectsResult = await projectService.GetAllProjectsAsync();
            
            if (projectsResult.Success && projectsResult.Data != null)
            {
                Projects.Clear();
                foreach (var project in projectsResult.Data.Where(p => !p.IsArchived))
                {
                    Projects.Add(project);
                }

                if (_isEditMode && Expense.ProjectId > 0)
                {
                    ProjectComboBox.SelectedItem = Projects.FirstOrDefault(p => p.Id == Expense.ProjectId);
                }
                else if (Projects.Count > 0)
                {
                    ProjectComboBox.SelectedIndex = 0;
                }
            }

            // Load Payment Modes
            var paymentModeService = App.Host!.Services.GetRequiredService<Services.IPaymentModeService>();
            var paymentModesResult = await paymentModeService.GetAllPaymentModesAsync();
            
            if (paymentModesResult.Success && paymentModesResult.Data != null)
            {
                PaymentModes.Clear();
                foreach (var pm in paymentModesResult.Data)
                {
                    PaymentModes.Add(pm);
                }

                if (_isEditMode && Expense.PaymentModeId > 0)
                {
                    PaymentModeComboBox.SelectedItem = PaymentModes.FirstOrDefault(pm => pm.Id == Expense.PaymentModeId);
                }
                else if (PaymentModes.Count > 0)
                {
                    PaymentModeComboBox.SelectedIndex = 0;
                }
            }

            // Load Subscriptions
            var subscriptionService = App.Host!.Services.GetRequiredService<Services.ISubscriptionService>();
            var subscriptionsResult = await subscriptionService.GetAllSubscriptionsAsync();
            
            if (subscriptionsResult.Success && subscriptionsResult.Data != null)
            {
                Subscriptions.Clear();
                Subscriptions.Add(new Subscription { Id = 0, Name = "(None)" });
                foreach (var subscription in subscriptionsResult.Data)
                {
                    Subscriptions.Add(subscription);
                }

                if (_isEditMode && Expense.SubscriptionId.HasValue)
                {
                    SubscriptionComboBox.SelectedItem = Subscriptions.FirstOrDefault(s => s.Id == Expense.SubscriptionId.Value);
                }
                else
                {
                    SubscriptionComboBox.SelectedIndex = 0;
                }
            }
        }
        catch (Exception ex)
        {
            ShowError($"Error loading data: {ex.Message}");
        }
    }

    private void HasInvoiceCheckBox_CheckedChanged(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        InvoiceDetailsPanel.Visibility = HasInvoiceCheckBox.IsChecked == true 
            ? Microsoft.UI.Xaml.Visibility.Visible 
            : Microsoft.UI.Xaml.Visibility.Collapsed;
    }

    private async void UploadFileButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        try
        {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".pdf");

            // Get the window handle for the picker
            var window = (Microsoft.UI.Xaml.Application.Current as App)?.Window;
            if (window != null)
            {
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            }

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                var fileSize = (await file.GetBasicPropertiesAsync()).Size;
                var maxSize = 10 * 1024 * 1024UL; // 10 MB as ulong

                if (fileSize > maxSize)
                {
                    ShowError($"File size ({fileSize / 1024 / 1024:F2} MB) exceeds 10 MB limit.");
                    return;
                }

                _uploadedFileStream = await file.OpenStreamForReadAsync();
                _uploadedFileName = file.Name;
                FileNameText.Text = $"?? {file.Name} ({fileSize / 1024:F2} KB)";
            }
        }
        catch (Exception ex)
        {
            ShowError($"Error uploading file: {ex.Message}");
        }
    }

    private void ExpenseTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ValidateSubscriptionType();
    }

    private void SubscriptionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ValidateSubscriptionType();
    }

    private void ValidateSubscriptionType()
    {
        if (SubscriptionComboBox.SelectedItem is Subscription subscription && 
            subscription.Id > 0 &&
            ExpenseTypeComboBox.SelectedItem is ExpenseType expenseType)
        {
            if (!string.IsNullOrEmpty(subscription.Type) && subscription.Type != expenseType.Name)
            {
                SubscriptionValidationText.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            }
            else
            {
                SubscriptionValidationText.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
            }
        }
        else
        {
            SubscriptionValidationText.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
        }
    }

    private async void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // Get a deferral to perform async operations
        var deferral = args.GetDeferral();
        
        try
        {
            // Reset error
            ErrorTextBlock.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
            
            System.Diagnostics.Debug.WriteLine("[ExpenseDialog] Starting validation...");

            // Validate Name
            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                ShowError("Expense name is required.");
                args.Cancel = true;
                return;
            }

            // Validate Expense Type
            if (ExpenseTypeComboBox.SelectedItem is not ExpenseType expenseType)
            {
                ShowError("Expense Type is required.");
                args.Cancel = true;
                return;
            }

            // Validate Amount
            if (!decimal.TryParse(AmountTextBox.Text, out var amount) || amount < 0)
            {
                ShowError("Amount must be 0 or greater.");
                args.Cancel = true;
                return;
            }

            // Validate Project
            if (ProjectComboBox.SelectedItem is not Project project)
            {
                ShowError("Project is required.");
                args.Cancel = true;
                return;
            }

            // Validate Payment Amount
            if (!decimal.TryParse(PaymentAmountTextBox.Text, out var paymentAmount) || paymentAmount <= 0)
            {
                ShowError("Payment Amount is required and must be greater than 0.");
                args.Cancel = true;
                return;
            }

            // Validate Payment Mode
            if (PaymentModeComboBox.SelectedItem is not PaymentMode paymentMode)
            {
                ShowError("Payment Mode is required.");
                args.Cancel = true;
                return;
            }

            // Validate Payment Date
            if (!PaymentDatePicker.Date.HasValue)
            {
                ShowError("Payment Date is required.");
                args.Cancel = true;
                return;
            }

            // Validate Invoice Details if HasInvoice is checked
            if (HasInvoiceCheckBox.IsChecked == true)
            {
                if (string.IsNullOrWhiteSpace(InvoiceNumberTextBox.Text))
                {
                    ShowError("Invoice Number is required when invoice details are provided.");
                    args.Cancel = true;
                    return;
                }

                if (!InvoiceDatePicker.Date.HasValue)
                {
                    ShowError("Invoice Date is required when invoice details are provided.");
                    args.Cancel = true;
                    return;
                }

                // Validate invoice date is not in future
                if (InvoiceDatePicker.Date.Value.DateTime > DateTime.Now)
                {
                    ShowError("Invoice Date cannot be in the future.");
                    args.Cancel = true;
                    return;
                }

                // Validate billing period if provided
                if (BillingStartDatePicker.Date.HasValue && BillingEndDatePicker.Date.HasValue)
                {
                    if (BillingStartDatePicker.Date.Value > BillingEndDatePicker.Date.Value)
                    {
                        ShowError("Billing Period Start Date cannot be after End Date.");
                        args.Cancel = true;
                        return;
                    }

                    // Validate billing dates are not in future
                    if (BillingStartDatePicker.Date.Value.DateTime > DateTime.Now)
                    {
                        ShowError("Billing Period Start Date cannot be in the future.");
                        args.Cancel = true;
                        return;
                    }

                    if (BillingEndDatePicker.Date.Value.DateTime > DateTime.Now)
                    {
                        ShowError("Billing Period End Date cannot be in the future.");
                        args.Cancel = true;
                        return;
                    }
                }
                else if (BillingStartDatePicker.Date.HasValue && !BillingEndDatePicker.Date.HasValue)
                {
                    ShowError("Billing Period End Date is required when Start Date is provided.");
                    args.Cancel = true;
                    return;
                }
                else if (!BillingStartDatePicker.Date.HasValue && BillingEndDatePicker.Date.HasValue)
                {
                    ShowError("Billing Period Start Date is required when End Date is provided.");
                    args.Cancel = true;
                    return;
                }
            }

            // Validate Payment Date is not in future
            if (PaymentDatePicker.Date.Value.DateTime > DateTime.Now)
            {
                ShowError("Payment Date cannot be in the future.");
                args.Cancel = true;
                return;
            }

            // Validate Subscription Type Match
            if (SubscriptionComboBox.SelectedItem is Subscription subscription && subscription.Id > 0)
            {
                if (!string.IsNullOrEmpty(subscription.Type) && subscription.Type != expenseType.Name)
                {
                    ShowError($"Subscription/Vendor type '{subscription.Type}' does not match Expense Type '{expenseType.Name}'.");
                    args.Cancel = true;
                    return;
                }
            }

            // Set expense properties
            Expense.Name = NameTextBox.Text.Trim();
            Expense.Description = DescriptionTextBox.Text?.Trim();
            Expense.ExpenseTypeId = expenseType.Id;
            Expense.Amount = amount;
            Expense.ProjectId = project.Id;
            Expense.PaymentAmount = paymentAmount;
            Expense.PaymentModeId = paymentMode.Id;
            Expense.PaymentDate = PaymentDatePicker.Date.Value.DateTime;

            // Invoice details
            Expense.HasInvoice = HasInvoiceCheckBox.IsChecked == true;
            
            if (Expense.HasInvoice)
            {
                Expense.InvoiceNumber = InvoiceNumberTextBox.Text?.Trim();
                Expense.InvoiceDate = InvoiceDatePicker.Date?.DateTime;
                Expense.BillingPeriodStart = BillingStartDatePicker.Date?.DateTime;
                Expense.BillingPeriodEnd = BillingEndDatePicker.Date?.DateTime;

                // Handle file upload
                if (_uploadedFileStream != null && !string.IsNullOrEmpty(_uploadedFileName))
                {
                    System.Diagnostics.Debug.WriteLine("[ExpenseDialog] Uploading file...");
                    var fileStorageService = App.Host!.Services.GetRequiredService<Services.IFileStorageService>();
                    var result = await fileStorageService.SaveInvoiceFileAsync(_uploadedFileStream, _uploadedFileName);
                    
                    if (result.Success && result.FileGuid.HasValue)
                    {
                        Expense.InvoiceFileGuid = result.FileGuid.Value;
                        Expense.InvoiceFileName = _uploadedFileName;
                        System.Diagnostics.Debug.WriteLine($"[ExpenseDialog] File uploaded successfully: {result.FileGuid}");
                    }
                    else
                    {
                        ShowError($"Failed to upload file: {result.ErrorMessage}");
                        args.Cancel = true;
                        return;
                    }
                }
            }
            else
            {
                Expense.InvoiceNumber = null;
                Expense.InvoiceDate = null;
                Expense.BillingPeriodStart = null;
                Expense.BillingPeriodEnd = null;
                Expense.InvoiceFileGuid = null;
                Expense.InvoiceFileName = null;
            }

            // Subscription
            if (SubscriptionComboBox.SelectedItem is Subscription selectedSubscription && selectedSubscription.Id > 0)
            {
                Expense.SubscriptionId = selectedSubscription.Id;
            }
            else
            {
                Expense.SubscriptionId = null;
            }

            // Log success
            System.Diagnostics.Debug.WriteLine("[ExpenseDialog] Validation passed successfully. Dialog will close.");
            
            // All validations passed - DO NOT set args.Cancel (leave it false by default)
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ExpenseDialog] Exception in validation: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[ExpenseDialog] Stack trace: {ex.StackTrace}");
            ShowError($"Error saving expense: {ex.Message}");
            args.Cancel = true;
        }
        finally
        {
            deferral.Complete();
        }
    }

    private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // Cleanup
        _uploadedFileStream?.Dispose();
        Expense = null!;
    }

    private void ShowError(string message)
    {
        ErrorTextBlock.Text = message;
        ErrorTextBlock.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
        
        // Log to debug output
        System.Diagnostics.Debug.WriteLine($"[ExpenseDialog] Validation Error: {message}");
        
        // Try to scroll to top to show error (if in ScrollViewer)
        try
        {
            if (ErrorTextBlock.Parent is StackPanel stackPanel && 
                stackPanel.Parent is ScrollViewer scrollViewer)
            {
                scrollViewer.ChangeView(null, 0, null, false);
            }
        }
        catch { }
    }
}
