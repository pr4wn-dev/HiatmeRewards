using HiatMeApp.ViewModels;
using Microsoft.Maui.Controls;
using System.IO;

namespace HiatMeApp;

public partial class HomePage : ContentPage
{
    public HomePage(HomeViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}