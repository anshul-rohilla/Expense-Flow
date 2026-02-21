using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using Expense_Flow.ViewModels;
using Expense_Flow.Models;
using System;

namespace Expense_Flow.Views.Vendors;

public sealed partial class VendorsPage : Page
{
    public VendorsViewModel ViewModel { get; }

    public VendorsPage()
    {
        InitializeComponent();
        ViewModel = App.Host!.Services.GetRequiredService<VendorsViewModel>();
        DataContext = ViewModel;
        Loaded += VendorsPage_Loaded;
    }

    private async void VendorsPage_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await ViewModel.LoadVendorsCommand.ExecuteAsync(null);
    }

    private async void AddVendor_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var dialog = new VendorDialog();
        dialog.XamlRoot = this.XamlRoot;

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary && dialog.Vendor != null)
        {
            var vendorService = App.Host!.Services.GetRequiredService<Services.IVendorService>();
            var createResult = await vendorService.CreateVendorAsync(dialog.Vendor);

            if (createResult.Success)
            {
                await ViewModel.LoadVendorsCommand.ExecuteAsync(null);
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

    private async void EditVendor_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is Vendor vendor)
        {
            var dialog = new VendorDialog(vendor);
            dialog.XamlRoot = this.XamlRoot;

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary && dialog.Vendor != null)
            {
                var vendorService = App.Host!.Services.GetRequiredService<Services.IVendorService>();
                var updateResult = await vendorService.UpdateVendorAsync(dialog.Vendor);

                if (updateResult.Success)
                {
                    await ViewModel.LoadVendorsCommand.ExecuteAsync(null);
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

    private async void ArchiveVendor_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is Vendor vendor)
        {
            if (vendor.IsArchived)
            {
                await ViewModel.UnarchiveVendorCommand.ExecuteAsync(vendor);
            }
            else
            {
                await ViewModel.ArchiveVendorCommand.ExecuteAsync(vendor);
            }
        }
    }

    private async void DeleteVendor_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is Vendor vendor)
        {
            var confirmDialog = new ContentDialog
            {
                Title = "Delete Vendor",
                Content = $"Are you sure you want to delete '{vendor.Name}'? This action cannot be undone.",
                PrimaryButtonText = "Delete",
                SecondaryButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Secondary,
                XamlRoot = this.XamlRoot
            };

            var result = await confirmDialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await ViewModel.DeleteVendorCommand.ExecuteAsync(vendor);
            }
        }
    }
}
