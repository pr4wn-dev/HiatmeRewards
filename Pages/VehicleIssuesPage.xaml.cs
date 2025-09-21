using HiatMeApp.ViewModels;
using Microsoft.Maui.Controls;

namespace HiatMeApp;
public partial class VehicleIssuesPage : ContentPage
{
    public VehicleIssuesPage(VehicleIssuesViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
