using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Windowing;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Expense_Flow
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
            // Extend content into title bar
            ExtendsContentIntoTitleBar = true;
            
            // Configure AppWindow title bar
            var appWindow = GetAppWindowForCurrentWindow();
            if (appWindow != null)
            {
                appWindow.TitleBar.IconShowOptions = IconShowOptions.HideIconAndSystemMenu;
            }
            
            // Set title bar after a short delay to ensure elements are loaded
            this.Activated += MainWindow_Activated;
        }

        private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            // Only set once
            this.Activated -= MainWindow_Activated;
            
            // Access the ShellPage and set its title bar
            if (this.Content is Views.Shell.ShellPage shellPage)
            {
                SetTitleBar(shellPage.TitleBarElement);
            }
        }
        
        private AppWindow? GetAppWindowForCurrentWindow()
        {
            try
            {
                IntPtr hWnd = WindowNative.GetWindowHandle(this);
                Microsoft.UI.WindowId wndId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
                return AppWindow.GetFromWindowId(wndId);
            }
            catch
            {
                return null;
            }
        }
    }
}
