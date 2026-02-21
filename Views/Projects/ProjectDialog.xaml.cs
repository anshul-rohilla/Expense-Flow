using Microsoft.UI.Xaml.Controls;
using Expense_Flow.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Expense_Flow.Views.Projects;

public sealed partial class ProjectDialog : ContentDialog
{
    public Project Project { get; private set; }
    public int? SelectedProjectGroupId { get; private set; }
    private bool _isEditMode;
    public ObservableCollection<PaymentMode> PaymentModes { get; } = new();
    public ObservableCollection<ProjectGroup> ProjectGroups { get; } = new();

    public ProjectDialog(Project? project = null)
    {
        InitializeComponent();
        _isEditMode = project != null;
        Title = _isEditMode ? "Edit Project" : "Add Project";
        PrimaryButtonText = _isEditMode ? "Update" : "Create";
        SecondaryButtonText = "Cancel";

        Project = project ?? new Project();
        
        LoadDataAsync();

        NameTextBox.Text = Project.Name;
        DescriptionTextBox.Text = Project.Description;
        if (Project.MonthlyBudget.HasValue)
        {
            BudgetNumberBox.Value = (double)Project.MonthlyBudget.Value;
            UpdateAnnualBudgetHint(Project.MonthlyBudget.Value);
        }

        // Update hint when budget changes
        BudgetNumberBox.ValueChanged += (s, e) =>
        {
            if (!double.IsNaN(e.NewValue) && e.NewValue >= 0)
            {
                UpdateAnnualBudgetHint((decimal)e.NewValue);
            }
            else
            {
                AnnualBudgetHint.Text = "";
            }
        };
    }

    private void UpdateAnnualBudgetHint(decimal monthlyBudget)
    {
        var settingsService = App.Host?.Services?.GetService<Services.ISettingsService>();
        var currencySymbol = settingsService?.GetCurrencySymbol() ?? "$";
        var annualBudget = monthlyBudget * 12;
        AnnualBudgetHint.Text = $"Annual Budget: {currencySymbol}{annualBudget:N2}";
    }

    private async void LoadDataAsync()
    {
        var paymentModeService = App.Host!.Services.GetRequiredService<Services.IPaymentModeService>();
        var orgService = App.Host!.Services.GetRequiredService<Services.IOrganizationService>();
        var projectGroupService = App.Host!.Services.GetRequiredService<Services.IProjectGroupService>();
        var orgId = orgService.GetCurrentOrganizationId();
        
        // Load payment modes
        var paymentModesResult = await paymentModeService.GetAllPaymentModesAsync(orgId);
        
        if (paymentModesResult.Success && paymentModesResult.Data != null)
        {
            PaymentModes.Clear();
            PaymentModes.Add(new PaymentMode { Id = 0, Name = "(None)" });
            foreach (var pm in paymentModesResult.Data)
            {
                PaymentModes.Add(pm);
            }

            if (_isEditMode && Project.DefaultPaymentModeId.HasValue)
            {
                PaymentModeComboBox.SelectedItem = PaymentModes.FirstOrDefault(pm => pm.Id == Project.DefaultPaymentModeId.Value);
            }
            else
            {
                PaymentModeComboBox.SelectedIndex = 0;
            }
        }

        // Load project groups
        var groupsResult = await projectGroupService.GetAllProjectGroupsAsync();
        if (groupsResult.Success && groupsResult.Data != null)
        {
            ProjectGroups.Clear();
            ProjectGroups.Add(new ProjectGroup { Id = 0, Name = "(None)" });
            foreach (var g in groupsResult.Data)
            {
                ProjectGroups.Add(g);
            }

            // Select current group if editing - query mappings from service
            if (_isEditMode)
            {
                int? currentGroupId = null;
                
                // Check loaded navigation property first
                if (Project.ProjectGroupMappings?.Any() == true)
                {
                    currentGroupId = Project.ProjectGroupMappings.First().ProjectGroupId;
                }
                else
                {
                    // Query each group to find which one contains this project
                    foreach (var g in groupsResult.Data)
                    {
                        var projectsInGroup = await projectGroupService.GetProjectsInGroupAsync(g.Id);
                        if (projectsInGroup.Success && projectsInGroup.Data != null &&
                            projectsInGroup.Data.Any(p => p.Id == Project.Id))
                        {
                            currentGroupId = g.Id;
                            break;
                        }
                    }
                }

                if (currentGroupId.HasValue)
                {
                    ProjectGroupComboBox.SelectedItem = ProjectGroups.FirstOrDefault(g => g.Id == currentGroupId.Value);
                }
                else
                {
                    ProjectGroupComboBox.SelectedIndex = 0;
                }
            }
            else
            {
                ProjectGroupComboBox.SelectedIndex = 0;
            }
        }
    }

    private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        ErrorTextBlock.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;

        if (string.IsNullOrWhiteSpace(NameTextBox.Text))
        {
            args.Cancel = true;
            ErrorTextBlock.Text = "Name is required.";
            ErrorTextBlock.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            return;
        }

        if (string.IsNullOrWhiteSpace(DescriptionTextBox.Text))
        {
            args.Cancel = true;
            ErrorTextBlock.Text = "Description is required.";
            ErrorTextBlock.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
            return;
        }

        Project.Name = NameTextBox.Text.Trim();
        Project.Description = DescriptionTextBox.Text.Trim();

        if (!_isEditMode)
        {
            var orgSetService = App.Host!.Services.GetRequiredService<Services.IOrganizationService>();
            Project.OrganizationId = orgSetService.GetCurrentOrganizationId();
        }
        
        if (!double.IsNaN(BudgetNumberBox.Value) && BudgetNumberBox.Value >= 0)
        {
            Project.MonthlyBudget = (decimal)BudgetNumberBox.Value;
        }
        else
        {
            Project.MonthlyBudget = null;
        }

        if (PaymentModeComboBox.SelectedItem is PaymentMode pm && pm.Id > 0)
        {
            Project.DefaultPaymentModeId = pm.Id;
        }
        else
        {
            Project.DefaultPaymentModeId = null;
        }

        // Save selected group
        if (ProjectGroupComboBox.SelectedItem is ProjectGroup group && group.Id > 0)
        {
            SelectedProjectGroupId = group.Id;
        }
        else
        {
            SelectedProjectGroupId = null;
        }
    }

    private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        Project = null!;
    }
}
