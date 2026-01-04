using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using Expense_Flow.ViewModels;
using Expense_Flow.Models;
using Expense_Flow.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System;

namespace Expense_Flow.Views.Settings;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel? ViewModel { get; private set; }

    public SettingsPage()
    {
        InitializeComponent();
        
        try
        {
            if (App.Host?.Services != null)
            {
                ViewModel = App.Host.Services.GetRequiredService<SettingsViewModel>();
                DataContext = ViewModel;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error initializing SettingsViewModel: {ex.Message}");
        }
        
        Loaded += SettingsPage_Loaded;
    }

    private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
    {
        // Set database path manually to avoid x:Bind issues
        if (ViewModel != null)
        {
            DatabasePathText.Text = ViewModel.DatabasePath;
            
            // Set currency ComboBox selection
            var currencyTag = ViewModel.SelectedCurrency;
            foreach (ComboBoxItem item in CurrencyComboBox.Items)
            {
                if (item.Tag?.ToString() == currencyTag)
                {
                    CurrencyComboBox.SelectedItem = item;
                    break;
                }
            }
            
            // Set date format ComboBox selection
            var dateFormatTag = ViewModel.SelectedDateFormat;
            foreach (ComboBoxItem item in DateFormatComboBox.Items)
            {
                if (item.Tag?.ToString() == dateFormatTag)
                {
                    DateFormatComboBox.SelectedItem = item;
                    break;
                }
            }
            
            // Set theme ComboBox selection
            var themeTag = ViewModel.SelectedTheme switch
            {
                ElementTheme.Light => "Light",
                ElementTheme.Dark => "Dark",
                _ => "Default"
            };
            foreach (ComboBoxItem item in ThemeSelector.Items)
            {
                if (item.Tag?.ToString() == themeTag)
                {
                    ThemeSelector.SelectedItem = item;
                    break;
                }
            }
            
            // Set launch on startup toggle
            LaunchOnStartupToggle.IsOn = ViewModel.LaunchOnStartup;
        }
        else
        {
            DatabasePathText.Text = "Error loading settings";
            
            try
            {
                if (App.Host?.Services != null)
                {
                    ViewModel = App.Host.Services.GetRequiredService<SettingsViewModel>();
                    DataContext = ViewModel;
                    DatabasePathText.Text = ViewModel.DatabasePath;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing SettingsViewModel on Loaded: {ex.Message}");
                DatabasePathText.Text = $"Error: {ex.Message}";
            }
        }
    }

    private void ThemeSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ViewModel != null && sender is ComboBox combo && combo.SelectedItem is ComboBoxItem item)
        {
            var theme = item.Tag?.ToString();
            if (theme != null)
            {
                ViewModel.ChangeThemeCommand.Execute(theme);
            }
        }
    }

    private void CurrencySelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ViewModel != null && sender is ComboBox combo && combo.SelectedItem is ComboBoxItem item)
        {
            var currency = item.Tag?.ToString();
            if (currency != null)
            {
                ViewModel.ChangeCurrencyCommand.Execute(currency);
            }
        }
    }

    private void DateFormatSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ViewModel != null && sender is ComboBox combo && combo.SelectedItem is ComboBoxItem item)
        {
            var format = item.Tag?.ToString();
            if (format != null)
            {
                ViewModel.ChangeDateFormatCommand.Execute(format);
            }
        }
    }

    private void LaunchOnStartup_Toggled(object sender, RoutedEventArgs e)
    {
        if (ViewModel != null && sender is ToggleSwitch toggle)
        {
            ViewModel.LaunchOnStartup = toggle.IsOn;
            ViewModel.ToggleLaunchOnStartupCommand.Execute(null);
        }
    }

    private async void AddExpenseType_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ExpenseTypeDialog
        {
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        
        if (result == ContentDialogResult.Primary && dialog.ExpenseType != null)
        {
            var expenseTypeService = App.Host!.Services.GetRequiredService<IExpenseTypeService>();
            var createResult = await expenseTypeService.CreateExpenseTypeAsync(dialog.ExpenseType);

            if (createResult.Success)
            {
                await ViewModel!.LoadExpenseTypesCommand.ExecuteAsync(null);
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

    private async void EditExpenseType_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is ExpenseType expenseType)
        {
            var dialog = new ExpenseTypeDialog(expenseType)
            {
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            
            if (result == ContentDialogResult.Primary && dialog.ExpenseType != null)
            {
                var expenseTypeService = App.Host!.Services.GetRequiredService<IExpenseTypeService>();
                var updateResult = await expenseTypeService.UpdateExpenseTypeAsync(dialog.ExpenseType);

                if (updateResult.Success)
                {
                    await ViewModel!.LoadExpenseTypesCommand.ExecuteAsync(null);
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

    private async void DeleteExpenseType_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is ExpenseType expenseType)
        {
            var confirmDialog = new ContentDialog
            {
                Title = "Delete Type",
                Content = $"Are you sure you want to delete '{expenseType.DisplayText}'?",
                PrimaryButtonText = "Delete",
                SecondaryButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Secondary,
                XamlRoot = this.XamlRoot
            };

            var result = await confirmDialog.ShowAsync();
            
            if (result == ContentDialogResult.Primary)
            {
                await ViewModel!.DeleteExpenseTypeCommand.ExecuteAsync(expenseType);
            }
        }
    }

    private async void ClearAllData_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel == null) return;

        var inputDialog = new ContentDialog
        {
            Title = "?? Clear All Data - Confirmation Required",
            PrimaryButtonText = "Clear All Data",
            SecondaryButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Secondary,
            XamlRoot = this.XamlRoot
        };

        var stackPanel = new StackPanel { Spacing = 12 };
        
        stackPanel.Children.Add(new TextBlock
        {
            Text = "This will permanently delete ALL data including expenses, projects, contacts, and settings.",
            TextWrapping = TextWrapping.Wrap,
            Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red),
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        });

        stackPanel.Children.Add(new TextBlock
        {
            Text = $"To confirm, please enter your Windows username: {ViewModel.CurrentUsername}",
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 8, 0, 0)
        });

        var usernameInput = new TextBox
        {
            PlaceholderText = "Enter username to confirm",
            Margin = new Thickness(0, 8, 0, 0)
        };
        stackPanel.Children.Add(usernameInput);

        inputDialog.Content = stackPanel;

        var result = await inputDialog.ShowAsync();
        
        if (result == ContentDialogResult.Primary)
        {
            await ViewModel.ClearAllDataCommand.ExecuteAsync(usernameInput.Text);
            
            // If successful, show restart message
            if (!ViewModel.HasErrors)
            {
                var restartDialog = new ContentDialog
                {
                    Title = "Data Cleared",
                    Content = "All data has been cleared successfully. The application will now restart.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await restartDialog.ShowAsync();
                
                // Restart app
                await Windows.ApplicationModel.Core.CoreApplication.RequestRestartAsync(string.Empty);
            }
        }
    }
}
