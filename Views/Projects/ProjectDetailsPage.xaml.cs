using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Extensions.DependencyInjection;
using Expense_Flow.Models;
using Expense_Flow.Services;
using System;
using System.Linq;

namespace Expense_Flow.Views.Projects;

public sealed partial class ProjectDetailsPage : Page
{
    private Project? _project;
    private readonly IProjectService _projectService;
    private readonly IExpenseService _expenseService;
    private readonly ISettingsService _settingsService;

    public ProjectDetailsPage()
    {
        InitializeComponent();
        _projectService = App.Host!.Services.GetRequiredService<IProjectService>();
        _expenseService = App.Host!.Services.GetRequiredService<IExpenseService>();
        _settingsService = App.Host!.Services.GetRequiredService<ISettingsService>();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is int projectId)
        {
            var result = await _projectService.GetProjectByIdAsync(projectId);
            if (result.Success && result.Data != null)
            {
                _project = result.Data;
                LoadProjectDetails();
            }
        }
    }

    private async void LoadProjectDetails()
    {
        if (_project == null) return;

        var currencySymbol = _settingsService.GetCurrencySymbol();

        // Header
        ProjectNameText.Text = _project.Name;
        ProjectDescriptionText.Text = _project.Description ?? "No description";

        // Stats
        BudgetText.Text = _project.MonthlyBudget.HasValue ? 
            $"{currencySymbol}{_project.MonthlyBudget.Value:N2}" : "No budget set";

        // Calculate total spent
        var expensesResult = await _expenseService.GetExpensesByProjectAsync(_project.Id);
        decimal totalSpent = 0;
        
        if (expensesResult.Success && expensesResult.Data != null)
        {
            var expenses = expensesResult.Data.ToList();
            totalSpent = expenses.Sum(e => e.Amount);
            
            // Load expenses list
            ExpensesRepeater.ItemsSource = expenses;
            EmptyState.Visibility = expenses.Any() ? Visibility.Collapsed : Visibility.Visible;
        }
        else
        {
            EmptyState.Visibility = Visibility.Visible;
        }

        TotalSpentText.Text = $"{currencySymbol}{totalSpent:N2}";

        var remaining = (_project.MonthlyBudget ?? 0) - totalSpent;
        RemainingText.Text = $"{currencySymbol}{remaining:N2}";

        // Info
        CreatedAtText.Text = _project.CreatedAt.ToString("MMM dd, yyyy");
        ModifiedAtText.Text = _project.ModifiedAt?.ToString("MMM dd, yyyy") ?? "Not modified";
        PaymentModeText.Text = _project.DefaultPaymentMode?.Name ?? "Not set";
        StatusText.Text = _project.IsDefault ? "Default Project" : "Active";
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        if (Frame.CanGoBack)
        {
            Frame.GoBack();
        }
    }

    private async void EditProject_Click(object sender, RoutedEventArgs e)
    {
        if (_project == null) return;

        var dialog = new ProjectDialog(_project)
        {
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        
        if (result == ContentDialogResult.Primary && dialog.Project != null)
        {
            var updateResult = await _projectService.UpdateProjectAsync(dialog.Project);
            if (updateResult.Success)
            {
                _project = dialog.Project;
                LoadProjectDetails();
            }
        }
    }

    private async void AddExpense_Click(object sender, RoutedEventArgs e)
    {
        if (_project == null) return;

        var dialog = new Expenses.ExpenseDialog()
        {
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        
        if (result == ContentDialogResult.Primary && dialog.Expense != null)
        {
            // Set the ProjectId after dialog closes
            dialog.Expense.ProjectId = _project.Id;
            
            var createResult = await _expenseService.CreateExpenseAsync(dialog.Expense);
            if (createResult.Success)
            {
                LoadProjectDetails();
            }
            else
            {
                // Show error dialog
                var errorDialog = new ContentDialog
                {
                    Title = "Error Creating Expense",
                    Content = createResult.GetErrorMessage(),
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await errorDialog.ShowAsync();
            }
        }
    }

    private void ExpenseCard_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Controls.ExpenseCard card && card.Expense != null)
        {
            Frame.Navigate(typeof(Expenses.ExpenseDetailsPage), card.Expense.Id);
        }
    }

    private async void EditExpense_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is Expense expense)
        {
            var dialog = new Expenses.ExpenseDialog(expense)
            {
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            
            if (result == ContentDialogResult.Primary && dialog.Expense != null)
            {
                var updateResult = await _expenseService.UpdateExpenseAsync(dialog.Expense);
                if (updateResult.Success)
                {
                    LoadProjectDetails();
                }
                else
                {
                    var errorDialog = new ContentDialog
                    {
                        Title = "Error Updating Expense",
                        Content = updateResult.GetErrorMessage(),
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    };
                    await errorDialog.ShowAsync();
                }
            }
        }
    }

    private async void DeleteExpense_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is Expense expense)
        {
            var confirmDialog = new ContentDialog
            {
                Title = "Delete Expense",
                Content = $"Are you sure you want to delete expense '{expense.Name}'? This action cannot be undone.",
                PrimaryButtonText = "Delete",
                SecondaryButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Secondary,
                XamlRoot = this.XamlRoot
            };

            var result = await confirmDialog.ShowAsync();
            
            if (result == ContentDialogResult.Primary)
            {
                var deleteResult = await _expenseService.DeleteExpenseAsync(expense.Id);
                if (deleteResult.Success)
                {
                    LoadProjectDetails();
                }
                else
                {
                    var errorDialog = new ContentDialog
                    {
                        Title = "Error Deleting Expense",
                        Content = deleteResult.GetErrorMessage(),
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    };
                    await errorDialog.ShowAsync();
                }
            }
        }
    }
}
