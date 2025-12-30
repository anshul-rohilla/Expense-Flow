using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using Expense_Flow.ViewModels;
using Expense_Flow.Models;
using System;

namespace Expense_Flow.Views.Contacts;

public sealed partial class ContactsPage : Page
{
    public ContactsViewModel ViewModel { get; }

    public ContactsPage()
    {
        InitializeComponent();
        ViewModel = App.Host!.Services.GetRequiredService<ContactsViewModel>();
        DataContext = ViewModel;
        Loaded += ContactsPage_Loaded;
    }

    private async void ContactsPage_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await ViewModel.LoadContactsCommand.ExecuteAsync(null);
    }

    private async void AddContact_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var dialog = new ContactDialog();
        dialog.XamlRoot = this.XamlRoot;

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary && dialog.Contact != null)
        {
            var contactService = App.Host!.Services.GetRequiredService<Services.IContactService>();
            var createResult = await contactService.CreateContactAsync(dialog.Contact);

            if (createResult.Success)
            {
                await ViewModel.LoadContactsCommand.ExecuteAsync(null);
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

    private async void EditContact_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is Contact contact)
        {
            var dialog = new ContactDialog(contact);
            dialog.XamlRoot = this.XamlRoot;

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary && dialog.Contact != null)
            {
                var contactService = App.Host!.Services.GetRequiredService<Services.IContactService>();
                var updateResult = await contactService.UpdateContactAsync(dialog.Contact);

                if (updateResult.Success)
                {
                    await ViewModel.LoadContactsCommand.ExecuteAsync(null);
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

    private async void DeleteContact_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is Contact contact)
        {
            var confirmDialog = new ContentDialog
            {
                Title = "Delete Contact",
                Content = $"Are you sure you want to delete '{contact.Name}'? This action cannot be undone.",
                PrimaryButtonText = "Delete",
                SecondaryButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Secondary,
                XamlRoot = this.XamlRoot
            };

            var result = await confirmDialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await ViewModel.DeleteContactCommand.ExecuteAsync(contact);
            }
        }
    }
}
