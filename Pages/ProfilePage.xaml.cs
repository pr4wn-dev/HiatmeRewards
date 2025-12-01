using HiatMeApp.ViewModels;
using Microsoft.Maui.Controls;

namespace HiatMeApp;

public partial class ProfilePage : ContentPage
{
    private ProfileViewModel? _viewModel;
    
    public ProfilePage(ProfileViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
        
        // Subscribe to property changes to update image
        viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(ProfileViewModel.ProfilePicture))
            {
                UpdateProfileImage(viewModel.ProfilePicture);
            }
        };
    }
    
    protected override void OnAppearing()
    {
        base.OnAppearing();
        
        if (_viewModel != null)
        {
            // Reload user data to ensure we have the latest
            _viewModel.LoadUserData();
            
            // Update image if we have a profile picture
            if (!string.IsNullOrWhiteSpace(_viewModel.ProfilePicture))
            {
                Console.WriteLine($"ProfilePage.OnAppearing: Setting profile image: {_viewModel.ProfilePicture}");
                UpdateProfileImage(_viewModel.ProfilePicture);
            }
            else
            {
                Console.WriteLine($"ProfilePage.OnAppearing: ProfilePicture is empty, setting to null");
                if (ProfileImage != null)
                {
                    ProfileImage.Source = null;
                }
            }
        }
    }
    
    private void UpdateProfileImage(string? imageSource)
    {
        if (ProfileImage != null)
        {
            if (string.IsNullOrEmpty(imageSource))
            {
                Console.WriteLine("UpdateProfileImage: Image source is empty, setting to null");
                ProfileImage.Source = null;
            }
            else if (imageSource.StartsWith("http://") || imageSource.StartsWith("https://"))
            {
                // URL image
                Console.WriteLine($"UpdateProfileImage: Loading URL image: {imageSource}");
                ProfileImage.Source = ImageSource.FromUri(new Uri(imageSource));
            }
            else
            {
                // Local file path
                Console.WriteLine($"UpdateProfileImage: Loading local file: {imageSource}");
                ProfileImage.Source = ImageSource.FromFile(imageSource);
            }
        }
        else
        {
            Console.WriteLine("UpdateProfileImage: ProfileImage control is null");
        }
    }
}

