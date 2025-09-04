using Microsoft.Maui.Controls;
using HiatMeApp.ViewModels;
namespace HiatMeApp;

public partial class RegisterPage : ContentPage
{
public RegisterPage(RegisterViewModel viewModel)
{
    InitializeComponent();
    BindingContext = viewModel;
    Console.WriteLine("RegisterPage BindingContext set to RegisterViewModel.");
}
}