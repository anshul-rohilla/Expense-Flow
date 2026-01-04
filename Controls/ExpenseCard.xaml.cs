using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Expense_Flow.Models;

namespace Expense_Flow.Controls;

public sealed partial class ExpenseCard : UserControl
{
    public static readonly DependencyProperty ExpenseProperty =
        DependencyProperty.Register(
            nameof(Expense),
            typeof(Expense),
            typeof(ExpenseCard),
            new PropertyMetadata(null, OnExpenseChanged));

    public static readonly DependencyProperty IsCompactProperty =
        DependencyProperty.Register(
            nameof(IsCompact),
            typeof(bool),
            typeof(ExpenseCard),
            new PropertyMetadata(false, OnIsCompactChanged));

    public Expense? Expense
    {
        get => (Expense?)GetValue(ExpenseProperty);
        set => SetValue(ExpenseProperty, value);
    }

    public bool IsCompact
    {
        get => (bool)GetValue(IsCompactProperty);
        set => SetValue(IsCompactProperty, value);
    }

    public event RoutedEventHandler? Click;

    public ExpenseCard()
    {
        InitializeComponent();
    }

    private static void OnExpenseChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ExpenseCard card)
        {
            card.UpdateUI();
        }
    }

    private static void OnIsCompactChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ExpenseCard card)
        {
            card.UpdateCompactState();
        }
    }

    private void UpdateUI()
    {
        // UI updates handled by bindings
    }

    private void UpdateCompactState()
    {
        if (IsCompact)
        {
            DetailsRow.Visibility = Visibility.Collapsed;
            RootButton.Padding = new Thickness(0);
            RootBorder.Padding = new Thickness(12);
        }
        else
        {
            DetailsRow.Visibility = Visibility.Visible;
            RootButton.Padding = new Thickness(0);
            RootBorder.Padding = new Thickness(16);
        }
    }

    private void RootButton_Click(object sender, RoutedEventArgs e)
    {
        Click?.Invoke(this, e);
    }
}
