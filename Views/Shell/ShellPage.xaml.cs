using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using Expense_Flow.Services;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Expense_Flow.Views.Shell;

public sealed partial class ShellPage : Page
{
    public UIElement TitleBarElement => AppTitleBar;
    private readonly IUserProfileService _userProfileService;
    private readonly INavigationService _navigationService;

    public ShellPage()
    {
        InitializeComponent();
        _userProfileService = App.Host!.Services.GetRequiredService<IUserProfileService>();
        _navigationService = App.Host!.Services.GetRequiredService<INavigationService>();
        
        // Register the ContentFrame with NavigationService
        _navigationService.Frame = ContentFrame;
        
        Loaded += ShellPage_Loaded;
    }

    // Public method for programmatic navigation
    public void SelectNavigationItemByTag(string tag)
    {
        // Find the navigation item and select it
        NavigationViewItem? itemToSelect = null;
        
        // Search in MenuItems
        foreach (NavigationViewItemBase item in MainNavigationView.MenuItems)
        {
            if (item is NavigationViewItem navItem && navItem.Tag?.ToString() == tag)
            {
                itemToSelect = navItem;
                break;
            }
        }
        
        // Search in FooterMenuItems if not found
        if (itemToSelect == null)
        {
            foreach (NavigationViewItemBase item in MainNavigationView.FooterMenuItems)
            {
                if (item is NavigationViewItem navItem && navItem.Tag?.ToString() == tag)
                {
                    itemToSelect = navItem;
                    break;
                }
            }
        }
        
        // Select the item - this will trigger SelectionChanged and handle everything
        if (itemToSelect != null)
        {
            MainNavigationView.SelectedItem = itemToSelect;
        }
    }

    // Navigate to project details page
    public void NavigateToProjectDetails(int projectId)
    {
        ContentFrame.Navigate(typeof(Projects.ProjectDetailsPage), projectId);
    }

    private async void ShellPage_Loaded(object sender, RoutedEventArgs e)
    {
        // Load organization name
        try
        {
            var orgService = App.Host!.Services.GetRequiredService<IOrganizationService>();
            var orgResult = await orgService.GetDefaultOrganizationAsync();
            if (orgResult.Success && orgResult.Data != null)
            {
                OrgNameText.Text = orgResult.Data.Name;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading organization: {ex.Message}");
        }

        // Load user profile information
        try
        {
            var displayName = await _userProfileService.GetUserDisplayNameAsync();
            var email = await _userProfileService.GetUserEmailAsync();
            var userName = _userProfileService.GetWindowsUserName();

            // Update UI
            UserDisplayNameText.Text = displayName;
            UserEmailText.Text = !string.IsNullOrEmpty(email) ? email : userName;
            
            // Set PersonPicture properties
            ProfilePictureSmall.DisplayName = displayName;
            ProfilePictureSmall.Initials = GetInitials(displayName);
            
            ProfilePictureLarge.DisplayName = displayName;
            ProfilePictureLarge.Initials = GetInitials(displayName);

            // Try to load actual profile picture
            var pictureStream = await _userProfileService.GetUserPictureAsync();
            if (pictureStream != null)
            {
                try
                {
                    var bitmap = new BitmapImage();
                    var stream = await pictureStream.OpenReadAsync();
                    await bitmap.SetSourceAsync(stream);
                    ProfilePictureSmall.ProfilePicture = bitmap;
                    ProfilePictureLarge.ProfilePicture = bitmap;
                }
                catch
                {
                    // Use initials if picture loading fails
                }
            }
        }
        catch (Exception ex)
        {
            // Fallback to basic info
            var userName = Environment.UserName;
            UserDisplayNameText.Text = userName;
            UserEmailText.Text = userName;
            ProfilePictureSmall.DisplayName = userName;
            ProfilePictureSmall.Initials = GetInitials(userName);
            ProfilePictureLarge.DisplayName = userName;
            ProfilePictureLarge.Initials = GetInitials(userName);
        }
    }

    private string GetInitials(string name)
    {
        if (string.IsNullOrEmpty(name))
            return "U";

        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return "U";
        
        if (parts.Length == 1)
            return parts[0][0].ToString().ToUpper();
        
        return (parts[0][0].ToString() + parts[^1][0].ToString()).ToUpper();
    }

    private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        // Ignore if this is triggered by programmatic selection
        if (args.IsSettingsSelected)
            return;
            
        // Reset all navigation icon colors to default
        ResetNavigationIconColors();
        
        if (args.SelectedItemContainer is NavigationViewItem selectedNavItem)
        {
            var tag = selectedNavItem.Tag?.ToString();
            if (!string.IsNullOrEmpty(tag))
            {
                // Change selected icon color to blue
                SetNavigationIconColor(selectedNavItem, true);
                
                // Animate Settings icon when selected
                if (tag == "Settings")
                {
                    AnimatedIcon.SetState(SettingsAnimatedIcon, "Pressed");
                }
                
                NavigateToPage(tag);
            }
        }
    }

    private void ResetNavigationIconColors()
    {
        var primaryBrush = Application.Current.Resources["PrimaryBrush"] as Microsoft.UI.Xaml.Media.Brush;
        var secondaryBrush = Application.Current.Resources["TextFillColorSecondaryBrush"] as Microsoft.UI.Xaml.Media.Brush;
        
        // Reset menu items
        foreach (NavigationViewItemBase item in MainNavigationView.MenuItems)
        {
            if (item is NavigationViewItem navItem)
            {
                SetNavigationIconColor(navItem, false);
            }
        }
        
        // Reset footer items
        foreach (NavigationViewItemBase item in MainNavigationView.FooterMenuItems)
        {
            if (item is NavigationViewItem navItem)
            {
                if (navItem.Tag?.ToString() == "Settings")
                {
                    // Settings uses AnimatedIcon, change its foreground
                    if (SettingsAnimatedIcon != null)
                    {
                        SettingsAnimatedIcon.Foreground = secondaryBrush;
                    }
                }
                else
                {
                    SetNavigationIconColor(navItem, false);
                }
            }
        }
    }

    private void SetNavigationIconColor(NavigationViewItem navItem, bool isSelected)
    {
        var primaryBrush = Application.Current.Resources["PrimaryBrush"] as Microsoft.UI.Xaml.Media.Brush;
        var secondaryBrush = Application.Current.Resources["TextFillColorSecondaryBrush"] as Microsoft.UI.Xaml.Media.Brush;
        var brush = isSelected ? primaryBrush : secondaryBrush;
        
        // Find the StackPanel in Content
        if (navItem.Content is StackPanel stackPanel)
        {
            // Find FontIcon in StackPanel
            foreach (var child in stackPanel.Children)
            {
                if (child is FontIcon fontIcon)
                {
                    fontIcon.Foreground = brush;
                    break;
                }
                else if (child is AnimatedIcon animatedIcon && navItem.Tag?.ToString() == "Settings")
                {
                    animatedIcon.Foreground = brush;
                    break;
                }
            }
        }
    }

    private void NavigateToPage(string pageTag)
    {
        Type? pageType = pageTag switch
        {
            "Dashboard" => typeof(Dashboard.DashboardPage),
            "Expenses" => typeof(Expenses.ExpensesPage),
            "Projects" => typeof(Projects.ProjectsPage),
            "Contacts" => typeof(Contacts.ContactsPage),
            "PaymentModes" => typeof(PaymentModes.PaymentModesPage),
            "Subscriptions" => typeof(Subscriptions.SubscriptionsPage),
            "Vendors" => typeof(Vendors.VendorsPage),
            "Settlements" => typeof(Settlements.SettlementsPage),
            "Reports" => typeof(Reports.ReportsPage),
            "WhatsNew" => null, // Handle with dialog
            "Settings" => typeof(Settings.SettingsPage),
            _ => null
        };

        if (pageTag == "WhatsNew")
        {
            ShowWhatsNewDialog();
            return;
        }

        if (pageType != null && ContentFrame.CurrentSourcePageType != pageType)
        {
            ContentFrame.Navigate(pageType);
        }
    }

    private void SelectNavigationItem(string pageTag)
    {
        // Use DispatcherQueue to ensure UI updates happen after navigation
        DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, () =>
        {
            // Reset all colors first
            ResetNavigationIconColors();
            
            // Find and select in menu items
            foreach (NavigationViewItemBase item in MainNavigationView.MenuItems)
            {
                if (item is NavigationViewItem navItem && navItem.Tag?.ToString() == pageTag)
                {
                    // Temporarily disconnect SelectionChanged to avoid recursion
                    MainNavigationView.SelectionChanged -= NavigationView_SelectionChanged;
                    MainNavigationView.SelectedItem = navItem;
                    MainNavigationView.SelectionChanged += NavigationView_SelectionChanged;
                    
                    // Manually set the icon color to blue
                    SetNavigationIconColor(navItem, true);
                    return;
                }
            }
            
            // Find and select in footer items
            foreach (NavigationViewItemBase item in MainNavigationView.FooterMenuItems)
            {
                if (item is NavigationViewItem navItem && navItem.Tag?.ToString() == pageTag)
                {
                    // Temporarily disconnect SelectionChanged to avoid recursion
                    MainNavigationView.SelectionChanged -= NavigationView_SelectionChanged;
                    MainNavigationView.SelectedItem = navItem;
                    MainNavigationView.SelectionChanged += NavigationView_SelectionChanged;
                    
                    // Manually set the icon color to blue
                    SetNavigationIconColor(navItem, true);
                    
                    // Animate Settings icon if it's settings
                    if (pageTag == "Settings")
                    {
                        AnimatedIcon.SetState(SettingsAnimatedIcon, "Pressed");
                    }
                    return;
                }
            }
        });
    }

    private void NavigationView_Loaded(object sender, RoutedEventArgs e)
    {
        ContentFrame.Navigate(typeof(Dashboard.DashboardPage));
        
        foreach (NavigationViewItemBase item in MainNavigationView.MenuItems)
        {
            if (item is NavigationViewItem navItem && navItem.Tag?.ToString() == "Dashboard")
            {
                MainNavigationView.SelectedItem = navItem;
                // Set Dashboard icon to blue initially
                SetNavigationIconColor(navItem, true);
                break;
            }
        }

        // Add pointer events to Settings item for animation
        foreach (NavigationViewItemBase item in MainNavigationView.FooterMenuItems)
        {
            if (item is NavigationViewItem navItem && navItem.Tag?.ToString() == "Settings")
            {
                navItem.PointerEntered += SettingsItem_PointerEntered;
                navItem.PointerExited += SettingsItem_PointerExited;
                break;
            }
        }
    }

    private void SettingsItem_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        AnimatedIcon.SetState(SettingsAnimatedIcon, "PointerOver");
    }

    private void SettingsItem_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        AnimatedIcon.SetState(SettingsAnimatedIcon, "Normal");
    }

    private void ViewProfile_Click(object sender, RoutedEventArgs e)
    {
        ShowProfileDialog();
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        // Navigate to settings - find settings in footer items
        foreach (NavigationViewItemBase item in MainNavigationView.FooterMenuItems)
        {
            if (item is NavigationViewItem navItem && navItem.Tag?.ToString() == "Settings")
            {
                MainNavigationView.SelectedItem = navItem;
                break;
            }
        }
        NavigateToPage("Settings");
    }

    private void Help_Click(object sender, RoutedEventArgs e)
    {
        ShowHelpDialog();
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        ShowAboutDialog();
    }

    private async void ShowWhatsNewDialog()
    {
        var content = new StackPanel { Spacing = 16, MaxWidth = 450 };
        
        // Header with party icon
        var header = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
        header.Children.Add(new FontIcon 
        { 
            Glyph = "\uEA8F", 
            FontSize = 32,
            Foreground = Application.Current.Resources["PrimaryBrush"] as Microsoft.UI.Xaml.Media.Brush
        });
        header.Children.Add(new TextBlock 
        { 
            Text = "What's New in Expense Flow",
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            FontSize = 18,
            VerticalAlignment = VerticalAlignment.Center
        });
        content.Children.Add(header);

        // Version info
        content.Children.Add(new TextBlock 
        { 
            Text = "Version 1.0.0 - Latest Updates",
            Foreground = Application.Current.Resources["TextFillColorSecondaryBrush"] as Microsoft.UI.Xaml.Media.Brush,
            FontSize = 13
        });

        content.Children.Add(new MenuFlyoutSeparator());

        // Features list
        var features = new StackPanel { Spacing = 12 };
        
        AddFeature(features, "??", "Account renamed to Subscriptions", "Better terminology for tracking Netflix, GitHub, Azure, and more!");
        AddFeature(features, "??", "Windows Profile Integration", "Your Windows account info and picture now appears in the title bar");
        AddFeature(features, "??", "Modern UI Updates", "Sleek navigation and beautiful gradient cards");
        AddFeature(features, "?", "Performance Improvements", "Faster loading and smoother animations");
        AddFeature(features, "??", "Bug Fixes", "Various stability improvements and fixes");

        content.Children.Add(features);

        var dialog = new ContentDialog
        {
            Title = string.Empty,
            Content = content,
            CloseButtonText = "Got it!",
            XamlRoot = this.XamlRoot,
            Style = Application.Current.Resources["ModernContentDialogStyle"] as Style
        };
        await dialog.ShowAsync();
    }

    private void AddFeature(StackPanel parent, string emoji, string title, string description)
    {
        var featurePanel = new StackPanel { Spacing = 4 };
        
        var titlePanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        titlePanel.Children.Add(new TextBlock { Text = emoji, FontSize = 16 });
        titlePanel.Children.Add(new TextBlock 
        { 
            Text = title,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold 
        });
        featurePanel.Children.Add(titlePanel);
        
        featurePanel.Children.Add(new TextBlock 
        { 
            Text = description,
            TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap,
            Foreground = Application.Current.Resources["TextFillColorSecondaryBrush"] as Microsoft.UI.Xaml.Media.Brush,
            Margin = new Thickness(24, 0, 0, 0)
        });

        parent.Children.Add(featurePanel);
    }

    private async void ShowProfileDialog()
    {
        var displayName = UserDisplayNameText.Text;
        var email = UserEmailText.Text;
        
        var content = new StackPanel { Spacing = 12 };
        content.Children.Add(new TextBlock 
        { 
            Text = "Profile Information",
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            FontSize = 16
        });
        content.Children.Add(new TextBlock { Text = $"Name: {displayName}" });
        content.Children.Add(new TextBlock { Text = $"Account: {email}" });
        content.Children.Add(new TextBlock 
        { 
            Text = "\nThis information is retrieved from your Windows account.",
            Foreground = Application.Current.Resources["TextFillColorSecondaryBrush"] as Microsoft.UI.Xaml.Media.Brush,
            TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap,
            FontSize = 12
        });

        var dialog = new ContentDialog
        {
            Title = "User Profile",
            Content = content,
            CloseButtonText = "Close",
            XamlRoot = this.XamlRoot,
            Style = Application.Current.Resources["ModernContentDialogStyle"] as Style
        };
        await dialog.ShowAsync();
    }

    private async void ShowHelpDialog()
    {
        var content = new StackPanel { Spacing = 12 };
        content.Children.Add(new TextBlock 
        { 
            Text = "Need Help?",
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            FontSize = 16
        });
        content.Children.Add(new TextBlock 
        { 
            Text = "� Navigate using the sidebar menu\n� Track expenses by project and payment method\n� Manage subscriptions and contacts\n� View reports and analytics",
            TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap
        });

        var dialog = new ContentDialog
        {
            Title = "Help & Feedback",
            Content = content,
            PrimaryButtonText = "Send Feedback",
            CloseButtonText = "Close",
            XamlRoot = this.XamlRoot,
            Style = Application.Current.Resources["ModernContentDialogStyle"] as Style
        };
        await dialog.ShowAsync();
    }

    private async void ShowAboutDialog()
    {
        var content = new StackPanel { Spacing = 12 };
        content.Children.Add(new TextBlock 
        { 
            Text = "Expense Flow",
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            FontSize = 20
        });
        content.Children.Add(new TextBlock 
        { 
            Text = "Version 1.0.0",
            Foreground = Application.Current.Resources["TextFillColorSecondaryBrush"] as Microsoft.UI.Xaml.Media.Brush
        });
        content.Children.Add(new TextBlock 
        { 
            Text = "\nA modern expense tracking application built with WinUI 3 and .NET 10.",
            TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap,
            Margin = new Thickness(0, 8, 0, 0)
        });
        content.Children.Add(new TextBlock 
        { 
            Text = "� 2024 Expense Flow. All rights reserved.",
            FontSize = 12,
            Foreground = Application.Current.Resources["TextFillColorSecondaryBrush"] as Microsoft.UI.Xaml.Media.Brush,
            Margin = new Thickness(0, 8, 0, 0)
        });

        var dialog = new ContentDialog
        {
            Title = "About",
            Content = content,
            CloseButtonText = "Close",
            XamlRoot = this.XamlRoot,
            Style = Application.Current.Resources["ModernContentDialogStyle"] as Style
        };
        await dialog.ShowAsync();
    }
}
