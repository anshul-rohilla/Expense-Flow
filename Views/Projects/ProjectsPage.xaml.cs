using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using Expense_Flow.ViewModels;
using Expense_Flow.Models;
using System;

namespace Expense_Flow.Views.Projects;

public sealed partial class ProjectsPage : Page
{
    public ProjectsViewModel ViewModel { get; }

    public ProjectsPage()
    {
        InitializeComponent();
        ViewModel = App.Host!.Services.GetRequiredService<ProjectsViewModel>();
        DataContext = ViewModel;
        Loaded += ProjectsPage_Loaded;
    }

    private async void ProjectsPage_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await ViewModel.LoadProjectsCommand.ExecuteAsync(null);
        await ViewModel.LoadProjectGroupsCommand.ExecuteAsync(null);
    }

    private void ShowArchived_Toggled(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is ToggleSwitch toggle)
        {
            ViewModel.ShowArchived = toggle.IsOn;
        }
    }

    private async void AddProject_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var dialog = new ProjectDialog
        {
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        
        if (result == ContentDialogResult.Primary && dialog.Project != null)
        {
            var projectService = App.Host!.Services.GetRequiredService<Services.IProjectService>();
            var createResult = await projectService.CreateProjectAsync(dialog.Project);

            if (createResult.Success)
            {
                await ViewModel.LoadProjectsCommand.ExecuteAsync(null);
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

    private async void EditProject_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is Project project)
        {
            var dialog = new ProjectDialog(project)
            {
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            
            if (result == ContentDialogResult.Primary && dialog.Project != null)
            {
                var projectService = App.Host!.Services.GetRequiredService<Services.IProjectService>();
                var updateResult = await projectService.UpdateProjectAsync(dialog.Project);

                if (updateResult.Success)
                {
                    await ViewModel.LoadProjectsCommand.ExecuteAsync(null);
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

    private async void ArchiveProject_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is Project project)
        {
            await ViewModel.ArchiveProjectCommand.ExecuteAsync(project);
        }
    }

    private void ViewProjectDetails_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is Project project)
        {
            // Navigate to project details page
            var shellPage = FindShellPage();
            if (shellPage != null)
            {
                shellPage.NavigateToProjectDetails(project.Id);
            }
        }
    }

    private Shell.ShellPage? FindShellPage()
    {
        var parent = this.Parent;
        while (parent != null)
        {
            if (parent is Frame frame && frame.Parent is NavigationView navView && navView.Parent is Grid grid && grid.Parent is Shell.ShellPage shellPage)
            {
                return shellPage;
            }
            parent = (parent as Microsoft.UI.Xaml.FrameworkElement)?.Parent;
        }
        return null;
    }

    private async void AddProjectGroup_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var dialog = new ProjectGroupDialog
        {
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        
        if (result == ContentDialogResult.Primary && dialog.ProjectGroup != null)
        {
            var projectGroupService = App.Host!.Services.GetRequiredService<Services.IProjectGroupService>();
            var createResult = await projectGroupService.CreateProjectGroupAsync(dialog.ProjectGroup);

            if (createResult.Success)
            {
                await ViewModel.LoadProjectGroupsCommand.ExecuteAsync(null);
                
                var successDialog = new ContentDialog
                {
                    Title = "Success",
                    Content = "Project group created successfully!",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await successDialog.ShowAsync();
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
}
