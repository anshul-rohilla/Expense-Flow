using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using Expense_Flow.ViewModels;
using Expense_Flow.Models;
using System;

namespace Expense_Flow.Views.Expenses;

public sealed partial class ExpensesPage : Page
{
    public ExpensesViewModel ViewModel { get; }

    public ExpensesPage()
    {
        InitializeComponent();
        ViewModel = App.Host!.Services.GetRequiredService<ExpensesViewModel>();
        DataContext = ViewModel;
        Loaded += ExpensesPage_Loaded;
    }

    private async void ExpensesPage_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await ViewModel.LoadExpensesCommand.ExecuteAsync(null);
        await ViewModel.LoadProjectsCommand.ExecuteAsync(null);
    }

    private async void AddExpense_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var dialog = new ExpenseDialog
        {
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        
        if (result == ContentDialogResult.Primary && dialog.Expense != null)
        {
            var expenseService = App.Host!.Services.GetRequiredService<Services.IExpenseService>();
            var createResult = await expenseService.CreateExpenseAsync(dialog.Expense);

            if (createResult.Success)
            {
                await ViewModel.LoadExpensesCommand.ExecuteAsync(null);
            }
            else
            {
                var errorDialog = new ContentDialog
                {
                    Title = "Error",
                    Content = createResult.GetErrorMessage(),
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await errorDialog.ShowAsync();
            }
        }
    }

    private async void EditExpense_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is Expense expense)
        {
            var dialog = new ExpenseDialog(expense)
            {
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            
            if (result == ContentDialogResult.Primary && dialog.Expense != null)
            {
                var expenseService = App.Host!.Services.GetRequiredService<Services.IExpenseService>();
                var updateResult = await expenseService.UpdateExpenseAsync(dialog.Expense);

                if (updateResult.Success)
                {
                    await ViewModel.LoadExpensesCommand.ExecuteAsync(null);
                }
                else
                {
                    var errorDialog = new ContentDialog
                    {
                        Title = "Error",
                        Content = updateResult.GetErrorMessage(),
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    };
                    await errorDialog.ShowAsync();
                }
            }
        }
    }

    private async void DeleteExpense_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is Expense expense)
        {
            var confirmDialog = new ContentDialog
            {
                Title = "Delete Expense",
                Content = $"Are you sure you want to delete expense '{expense.InvoiceNumber}'? This action cannot be undone.",
                PrimaryButtonText = "Delete",
                SecondaryButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Secondary,
                XamlRoot = this.XamlRoot
            };

            var result = await confirmDialog.ShowAsync();
            
            if (result == ContentDialogResult.Primary)
            {
                await ViewModel.DeleteExpenseCommand.ExecuteAsync(expense);
            }
        }
    }
}
