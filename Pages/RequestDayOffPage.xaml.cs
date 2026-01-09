using HiatMeApp.ViewModels;
using Microsoft.Maui.Controls;

namespace HiatMeApp;

public partial class RequestDayOffPage : ContentPage
{
    private readonly RequestDayOffViewModel _viewModel;

    public RequestDayOffPage(RequestDayOffViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        // Load requests when page appears
        await _viewModel.LoadMyRequestsAsync();
    }
}


