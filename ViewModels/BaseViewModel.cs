using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace HiatMeApp.ViewModels;

public partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _title;
}