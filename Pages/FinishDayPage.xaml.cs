using HiatMeApp.ViewModels;
using Microsoft.Maui.Controls;

namespace HiatMeApp; // Changed from HiatMeApp.Pages to HiatMeApp

public partial class FinishDayPage : ContentPage
{
    private readonly FinishDayViewModel _viewModel;

    public FinishDayPage(FinishDayViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }
}