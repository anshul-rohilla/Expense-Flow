using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using Expense_Flow.ViewModels;
using Expense_Flow.Models;
using System;
using System.Linq;
using Microsoft.UI.Xaml;

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
        await ViewModel.LoadAllCommand.ExecuteAsync(null);
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

            if (createResult.Success && createResult.Data != null)
            {
                // Handle group assignment
                if (dialog.SelectedProjectGroupId.HasValue)
                {
                    var groupService = App.Host!.Services.GetRequiredService<Services.IProjectGroupService>();
                    await groupService.AddProjectToGroupAsync(dialog.SelectedProjectGroupId.Value, createResult.Data.Id);
                }
                await ViewModel.LoadAllCommand.ExecuteAsync(null);
            }
            else if (!createResult.Success)
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
                    // Handle group assignment changes
                    var groupService = App.Host!.Services.GetRequiredService<Services.IProjectGroupService>();
                    
                    // Remove from all existing groups first
                    var allGroups = await groupService.GetAllProjectGroupsAsync();
                    if (allGroups.Success && allGroups.Data != null)
                    {
                        foreach (var group in allGroups.Data)
                        {
                            var projectsInGroup = await groupService.GetProjectsInGroupAsync(group.Id);
                            if (projectsInGroup.Success && projectsInGroup.Data != null &&
                                projectsInGroup.Data.Any(p => p.Id == project.Id))
                            {
                                await groupService.RemoveProjectFromGroupAsync(group.Id, project.Id);
                            }
                        }
                    }

                    // Add to new group if selected
                    if (dialog.SelectedProjectGroupId.HasValue)
                    {
                        await groupService.AddProjectToGroupAsync(dialog.SelectedProjectGroupId.Value, project.Id);
                    }

                    await ViewModel.LoadAllCommand.ExecuteAsync(null);
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

    private string _currentProjectFilter = "All";

    private void FilterProjects_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string filter)
        {
            _currentProjectFilter = filter;

            // Update button styles
            var primaryStyle = (Style)Application.Current.Resources["PrimaryButtonStyle"];
            FilterAllButton.Style = (filter == "All") ? primaryStyle : null;
            FilterActiveButton.Style = (filter == "Active") ? primaryStyle : null;
            FilterDefaultButton.Style = (filter == "Default") ? primaryStyle : null;

            // Apply filter
            ApplyProjectFilter();
        }
    }

    private void ApplyProjectFilter()
    {
        // Reload with filter applied via ViewModel
        _ = ViewModel.ApplyFilterAsync(_currentProjectFilter);
    }

    private async void AddProjectGroup_Click(object sender, RoutedEventArgs e)
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
                await ViewModel.LoadAllCommand.ExecuteAsync(null);
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

    private async void EditProjectGroup_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is ProjectGroup group)
        {
            var dialog = new ProjectGroupDialog(group)
            {
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary && dialog.ProjectGroup != null)
            {
                var projectGroupService = App.Host!.Services.GetRequiredService<Services.IProjectGroupService>();
                var updateResult = await projectGroupService.UpdateProjectGroupAsync(dialog.ProjectGroup);

                if (updateResult.Success)
                {
                    await ViewModel.LoadAllCommand.ExecuteAsync(null);
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

    private async void DeleteProjectGroup_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is ProjectGroup group)
        {
            var confirmDialog = new ContentDialog
            {
                Title = "Delete Group",
                Content = $"Are you sure you want to delete group '{group.Name}'?",
                PrimaryButtonText = "Delete",
                SecondaryButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Secondary,
                XamlRoot = this.XamlRoot
            };

            var result = await confirmDialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await ViewModel.DeleteProjectGroupCommand.ExecuteAsync(group);
            }
        }
    }
}
