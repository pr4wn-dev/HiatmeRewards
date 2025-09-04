using HiatMeApp.ViewModels;
using Microsoft.Maui.Controls;

namespace HiatMeApp;

public partial class VehiclePage : ContentPage
{
    public VehiclePage()
    {
        InitializeComponent();
        BindingContext = new VehicleViewModel();
        Console.WriteLine("VehiclePage: BindingContext set to VehicleViewModel");
    }
}