using Microsoft.UI.Xaml.Controls;
using Expense_Flow.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Expense_Flow.Views.Expenses;

public sealed partial class ExpenseDialog : ContentDialog
{
    public Expense Expense { get; private set; }
    private bool _isEditMode;
    public ObservableCollection<Project> Projects { get; } = new();
    public ObservableCollection<PaymentMode> PaymentModes { get; } = new();
    public ObservableCollection<Subscription> Subscriptions { get; } = new();

    public ExpenseDialog(Expense? expense = null)
    {
        InitializeComponent();
        _isEditMode = expense != null;
        Title = _isEditMode ? "Edit Expense" : "Add Expense";
        PrimaryButtonText = _isEditMode ? "Update" : "Create";
        SecondaryButtonText = "Cancel";

        Expense = expense ?? new Expense();
        
        LoadDataAsync();

        // Set initial values if in edit mode
        if (_isEditMode && expense != null)
        {
            NameTextBox.Text = expense.Name ?? string.Empty;
            InvoiceNumberTextBox.Text = expense.InvoiceNumber ?? string.Empty;
            InvoiceAmountTextBox.Text = expense.InvoiceAmount.ToString();
            DescriptionTextBox.Text = expense.Description ?? string.Empty;
        }
    }

    private async void LoadDataAsync()
    {
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

            if (_isEditMode && Expense.PaymentModeId.HasValue && Expense.PaymentModeId > 0)
            {
                PaymentModeComboBox.SelectedItem = PaymentModes.FirstOrDefault(pm => pm.Id == Expense.PaymentModeId.Value);
            }
            else if (PaymentModes.Count > 0)
            {
                PaymentModeComboBox.SelectedIndex = 0;
            }
        }

        // Load Subscriptions
        var SubscriptionService = App.Host!.Services.GetRequiredService<Services.ISubscriptionService>();
        var accountsResult = await SubscriptionService.GetAllSubscriptionsAsync();
        
        if (accountsResult.Success && accountsResult.Data != null)
        {
            Subscriptions.Clear();
            Subscriptions.Add(new Subscription { Id = 0, Name = "(None)" });
            foreach (var Subscription in accountsResult.Data)
            {
                Subscriptions.Add(Subscription);
            }

            if (_isEditMode && Expense.SubscriptionId.HasValue)
            {
                SubscriptionComboBox.SelectedItem = Subscriptions.FirstOrDefault(a => a.Id == Expense.SubscriptionId.Value);
            }
            else
            {
                SubscriptionComboBox.SelectedIndex = 0;
            }
        }
    }

    private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // Validate Name
        if (string.IsNullOrWhiteSpace(NameTextBox.Text))
        {
            args.Cancel = true;
            ErrorTextBlock.Text = "Name is required.";
            ErrorTextBlock.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            return;
        }

        // Validate Invoice Number
        if (string.IsNullOrWhiteSpace(InvoiceNumberTextBox.Text))
        {
            args.Cancel = true;
            ErrorTextBlock.Text = "Invoice number is required.";
            ErrorTextBlock.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            return;
        }

        // Validate Invoice Amount
        if (!decimal.TryParse(InvoiceAmountTextBox.Text, out var amount) || amount <= 0)
        {
            args.Cancel = true;
            ErrorTextBlock.Text = "Valid invoice amount is required.";
            ErrorTextBlock.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            return;
        }

        // Validate Project
        if (ProjectComboBox.SelectedItem is not Project project)
        {
            args.Cancel = true;
            ErrorTextBlock.Text = "Project is required.";
            ErrorTextBlock.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            return;
        }

        // Validate Payment Mode
        if (PaymentModeComboBox.SelectedItem is not PaymentMode paymentMode)
        {
            args.Cancel = true;
            ErrorTextBlock.Text = "Payment mode is required.";
            ErrorTextBlock.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            return;
        }

        // Set expense properties
        Expense.Name = NameTextBox.Text.Trim();
        Expense.InvoiceNumber = InvoiceNumberTextBox.Text.Trim();
        Expense.InvoiceAmount = amount;
        Expense.ProjectId = project.Id;
        Expense.PaymentModeId = paymentMode.Id;
        Expense.Description = string.IsNullOrWhiteSpace(DescriptionTextBox.Text) ? null : DescriptionTextBox.Text.Trim();

        // Set subscription if selected
        if (SubscriptionComboBox.SelectedItem is Subscription Subscription && Subscription.Id > 0)
        {
            Expense.SubscriptionId = Subscription.Id;
        }
        else
        {
            Expense.SubscriptionId = null;
        }
    }

    private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        Expense = null!;
    }
}
