using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Expense_Flow.Models;
using Expense_Flow.Services;

namespace Expense_Flow.ViewModels;

/// <summary>
/// Represents a group section on the Projects page containing a group header and its projects.
/// </summary>
public class ProjectGroupSection
{
    public ProjectGroup? Group { get; set; }
    public string GroupName => Group?.Name ?? "Others";
    public string? GroupDescription => Group?.Description;
    public bool IsDefaultSection => Group == null;
    public ObservableCollection<Project> Projects { get; set; } = new();
}

public partial class ProjectsViewModel : ViewModelBase
{
    private readonly IProjectService _projectService;
    private readonly IProjectGroupService _projectGroupService;

    [ObservableProperty]
    private ObservableCollection<Project> _projects = new();

    [ObservableProperty]
    private ObservableCollection<Project> _filteredProjects = new();

    [ObservableProperty]
    private ObservableCollection<ProjectGroup> _projectGroups = new();

    [ObservableProperty]
    private ObservableCollection<ProjectGroupSection> _groupSections = new();

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

    private string _currentFilter = "All";

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

    /// <summary>
    /// Loads projects and groups, then builds grouped sections.
    /// </summary>
    [RelayCommand]
    private async Task LoadAllAsync()
    {
        await ExecuteAsync(async () =>
        {
            // Load projects
            var projectResult = await _projectService.GetAllProjectsAsync(ShowArchived);
            if (projectResult.Success && projectResult.Data != null)
            {
                Projects = new ObservableCollection<Project>(projectResult.Data);
            }

            // Load groups
            var groupResult = await _projectGroupService.GetAllProjectGroupsAsync();
            if (groupResult.Success && groupResult.Data != null)
            {
                ProjectGroups = new ObservableCollection<ProjectGroup>(groupResult.Data);
            }

            // Build grouped sections
            await BuildGroupSectionsAsync();
        }, "Loading projects...");
    }

    private async Task BuildGroupSectionsAsync()
    {
        var sections = new List<ProjectGroupSection>();
        var assignedProjectIds = new HashSet<int>();

        // Apply filter to projects
        var filtered = ApplyFilterToProjects(Projects);

        // Build a section for each group
        foreach (var group in ProjectGroups)
        {
            var projectsInGroupResult = await _projectGroupService.GetProjectsInGroupAsync(group.Id);
            var section = new ProjectGroupSection { Group = group };

            if (projectsInGroupResult.Success && projectsInGroupResult.Data != null)
            {
                var groupProjectIds = projectsInGroupResult.Data.Select(p => p.Id).ToHashSet();
                // Use the filtered projects list to get the actual project objects (with Expenses loaded)
                var matchingProjects = filtered.Where(p => groupProjectIds.Contains(p.Id)).ToList();
                section.Projects = new ObservableCollection<Project>(matchingProjects);

                foreach (var id in groupProjectIds)
                {
                    assignedProjectIds.Add(id);
                }
            }

            sections.Add(section);
        }

        // "Others" section for ungrouped projects
        var ungroupedProjects = filtered.Where(p => !assignedProjectIds.Contains(p.Id)).ToList();
        if (ungroupedProjects.Any() || !sections.Any())
        {
            sections.Add(new ProjectGroupSection
            {
                Group = null,
                Projects = new ObservableCollection<Project>(ungroupedProjects)
            });
        }

        GroupSections = new ObservableCollection<ProjectGroupSection>(sections);
        FilteredProjects = new ObservableCollection<Project>(filtered);
    }

    private IEnumerable<Project> ApplyFilterToProjects(IEnumerable<Project> projects)
    {
        var filtered = projects.AsEnumerable();

        switch (_currentFilter)
        {
            case "Active":
                filtered = filtered.Where(p => !p.IsArchived);
                break;
            case "Default":
                filtered = filtered.Where(p => p.IsDefault);
                break;
        }

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            filtered = filtered.Where(p =>
                p.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        }

        return filtered;
    }

    public async Task ApplyFilterAsync(string filter)
    {
        _currentFilter = filter;
        await BuildGroupSectionsAsync();
    }

    private void ApplyFilter()
    {
        _ = BuildGroupSectionsAsync();
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
                await LoadAllAsync();
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
                await LoadAllAsync();
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
                await BuildGroupSectionsAsync();
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
                await BuildGroupSectionsAsync();
            }
            else
            {
                SetError(result.GetErrorMessage());
            }
        }, "Deleting project group...");
    }

    partial void OnShowArchivedChanged(bool value)
    {
        _ = LoadAllAsync();
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
    }
}
