using Microsoft.UI.Xaml.Controls;
using Expense_Flow.Models;

namespace Expense_Flow.Views.Projects;

public sealed partial class ProjectGroupDialog : ContentDialog
{
    public ProjectGroup ProjectGroup { get; private set; }
    private bool _isEditMode;

    public ProjectGroupDialog(ProjectGroup? projectGroup = null)
    {
        InitializeComponent();
        _isEditMode = projectGroup != null;
        Title = _isEditMode ? "Edit Project Group" : "Add Project Group";
        PrimaryButtonText = _isEditMode ? "Update" : "Create";
        SecondaryButtonText = "Cancel";

        ProjectGroup = projectGroup ?? new ProjectGroup();

        if (_isEditMode && projectGroup != null)
        {
            NameTextBox.Text = projectGroup.Name ?? string.Empty;
            DescriptionTextBox.Text = projectGroup.Description ?? string.Empty;
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

        ProjectGroup.Name = NameTextBox.Text.Trim();
        ProjectGroup.Description = string.IsNullOrWhiteSpace(DescriptionTextBox.Text) 
            ? null 
            : DescriptionTextBox.Text.Trim();
    }

    private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        ProjectGroup = null!;
    }
}
