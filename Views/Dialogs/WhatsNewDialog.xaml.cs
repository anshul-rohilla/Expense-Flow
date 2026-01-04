using Microsoft.UI.Xaml.Controls;
using System;

namespace Expense_Flow.Views.Dialogs;

public sealed partial class WhatsNewDialog : ContentDialog
{
    public WhatsNewDialog()
    {
        InitializeComponent();
        
        // Get app version from package
        try
        {
            var package = Windows.ApplicationModel.Package.Current;
            var version = package.Id.Version;
            VersionText.Text = $"Version {version.Major}.{version.Minor}.{version.Build}";
        }
        catch
        {
            VersionText.Text = "Version 1.0.0";
        }
    }
}
