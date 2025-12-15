using HiatMeApp.ViewModels;
using Microsoft.Maui.Controls;

namespace HiatMeApp;

public partial class RequestDayOffPage : ContentPage
{
    public RequestDayOffPage(RequestDayOffViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}


