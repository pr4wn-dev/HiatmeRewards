using HiatMeApp.ViewModels;

namespace HiatMeApp.Pages;

public partial class VehicleIssuesPage : ContentPage
{
    public VehicleIssuesPage(VehicleIssuesViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
