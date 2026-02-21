using Microsoft.UI.Xaml.Controls;
using Expense_Flow.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using Windows.System;

namespace Expense_Flow.Views.Settings;

public sealed partial class ExpenseTypeDialog : ContentDialog
{
    public ExpenseType ExpenseType { get; private set; }
    private bool _isEditMode;

    public ExpenseTypeDialog(ExpenseType? expenseType = null)
    {
        InitializeComponent();
        _isEditMode = expenseType != null;
        Title = _isEditMode ? "Edit Type" : "Add Type";
        PrimaryButtonText = _isEditMode ? "Update" : "Create";
        SecondaryButtonText = "Cancel";

        ExpenseType = expenseType ?? new ExpenseType();

        if (_isEditMode && expenseType != null)
        {
            NameTextBox.Text = expenseType.Name ?? string.Empty;
        }
    }

    private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        ErrorTextBlock.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;

        if (string.IsNullOrWhiteSpace(NameTextBox.Text))
        {
            args.Cancel = true;
            ErrorTextBlock.Text = "Name is required.";
            ErrorTextBlock.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            return;
        }

        if (!_isEditMode)
        {
            var orgService = App.Host!.Services.GetRequiredService<Services.IOrganizationService>();
            ExpenseType.OrganizationId = orgService.GetCurrentOrganizationId();
        }

        ExpenseType.Name = NameTextBox.Text.Trim();
        ExpenseType.Emoji = string.Empty; // No emoji needed
    }

    private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        ExpenseType = null!;
    }
}
