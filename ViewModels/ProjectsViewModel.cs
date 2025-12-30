using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Expense_Flow.Models;
using Expense_Flow.Services;

namespace Expense_Flow.ViewModels;

public partial class ProjectsViewModel : ViewModelBase
{
    private readonly IProjectService _projectService;
    private readonly IProjectGroupService _projectGroupService;

    [ObservableProperty]
    private ObservableCollection<Project> _projects = new();

    [ObservableProperty]
    private ObservableCollection<ProjectGroup> _projectGroups = new();

    [ObservableProperty]
    private Project? _selectedProject;

    [ObservableProperty]
    private ProjectGroup? _selectedProjectGroup;

    [ObservableProperty]
    private bool _showArchived = false;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private int _selectedTabIndex = 0;

    public ProjectsViewModel(IProjectService projectService, IProjectGroupService projectGroupService)
    {
        _projectService = projectService;
        _projectGroupService = projectGroupService;
    }

    [RelayCommand]
    private async Task LoadProjectsAsync()
    {
        await ExecuteAsync(async () =>
        {
            var result = await _projectService.GetAllProjectsAsync(ShowArchived);
            if (result.Success && result.Data != null)
            {
                Projects = new ObservableCollection<Project>(result.Data);
            }
            else
            {
                SetError(result.GetErrorMessage());
            }
        }, "Loading projects...");
    }

    [RelayCommand]
    private async Task LoadProjectGroupsAsync()
    {
        await ExecuteAsync(async () =>
        {
            var result = await _projectGroupService.GetAllProjectGroupsAsync();
            if (result.Success && result.Data != null)
            {
                ProjectGroups = new ObservableCollection<ProjectGroup>(result.Data);
            }
            else
            {
                SetError(result.GetErrorMessage());
            }
        }, "Loading project groups...");
    }

    [RelayCommand]
    private async Task ArchiveProjectAsync(Project project)
    {
        if (project == null) return;

        await ExecuteAsync(async () =>
        {
            var result = await _projectService.ArchiveProjectAsync(project.Id);
            if (result.Success)
            {
                await LoadProjectsAsync();
            }
            else
            {
                SetError(result.GetErrorMessage());
            }
        }, "Archiving project...");
    }

    [RelayCommand]
    private async Task UnarchiveProjectAsync(Project project)
    {
        if (project == null) return;

        await ExecuteAsync(async () =>
        {
            var result = await _projectService.UnarchiveProjectAsync(project.Id);
            if (result.Success)
            {
                await LoadProjectsAsync();
            }
            else
            {
                SetError(result.GetErrorMessage());
            }
        }, "Unarchiving project...");
    }

    [RelayCommand]
    private async Task DeleteProjectAsync(Project project)
    {
        if (project == null) return;

        await ExecuteAsync(async () =>
        {
            var result = await _projectService.DeleteProjectAsync(project.Id);
            if (result.Success)
            {
                Projects.Remove(project);
            }
            else
            {
                SetError(result.GetErrorMessage());
            }
        }, "Deleting project...");
    }

    [RelayCommand]
    private async Task DeleteProjectGroupAsync(ProjectGroup projectGroup)
    {
        if (projectGroup == null) return;

        await ExecuteAsync(async () =>
        {
            var result = await _projectGroupService.DeleteProjectGroupAsync(projectGroup.Id);
            if (result.Success)
            {
                ProjectGroups.Remove(projectGroup);
            }
            else
            {
                SetError(result.GetErrorMessage());
            }
        }, "Deleting project group...");
    }

    partial void OnShowArchivedChanged(bool value)
    {
        _ = LoadProjectsAsync();
    }
}
