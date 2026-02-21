using System;
using Microsoft.UI.Xaml.Controls;

namespace Expense_Flow.Services;

public interface INavigationService
{
    Frame? Frame { get; set; }
    bool NavigateTo(Type pageType, object? parameter = null);
    bool NavigateTo(string pageKey, object? parameter = null);
    bool CanGoBack { get; }
    void GoBack();
}

public class NavigationService : INavigationService
{
    private Frame? _frame;

    public Frame? Frame
    {
        get => _frame;
        set => _frame = value;
    }

    public bool NavigateTo(Type pageType, object? parameter = null)
    {
        if (_frame == null || pageType == null)
            return false;

        return _frame.Navigate(pageType, parameter);
    }

    public bool NavigateTo(string pageKey, object? parameter = null)
    {
        var pageType = GetPageType(pageKey);
        return pageType != null && NavigateTo(pageType, parameter);
    }

    public bool CanGoBack => _frame?.CanGoBack ?? false;

    public void GoBack()
    {
        if (CanGoBack)
            _frame?.GoBack();
    }

    private Type? GetPageType(string pageKey)
    {
        return pageKey switch
        {
            "Dashboard" => typeof(Views.Dashboard.DashboardPage),
            "Expenses" => typeof(Views.Expenses.ExpensesPage),
            "Projects" => typeof(Views.Projects.ProjectsPage),
            "Contacts" => typeof(Views.Contacts.ContactsPage),
            "PaymentModes" => typeof(Views.PaymentModes.PaymentModesPage),
            "Subscriptions" => typeof(Views.Subscriptions.SubscriptionsPage),
            "Vendors" => typeof(Views.Vendors.VendorsPage),
            "Settlements" => typeof(Views.Settlements.SettlementsPage),
            "Reports" => typeof(Views.Reports.ReportsPage),
            "Settings" => typeof(Views.Settings.SettingsPage),
            _ => null
        };
    }
}
