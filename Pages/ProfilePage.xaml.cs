using HiatMeApp.ViewModels;
using Microsoft.Maui.Controls;

namespace HiatMeApp;

public partial class ProfilePage : ContentPage
{
    public ProfilePage(ProfileViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        
        // Subscribe to property changes to update image
        viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(ProfileViewModel.ProfilePicture))
            {
                UpdateProfileImage(viewModel.ProfilePicture);
            }
        };
        
        // Set initial image
        if (!string.IsNullOrEmpty(viewModel.ProfilePicture))
        {
            UpdateProfileImage(viewModel.ProfilePicture);
        }
    }
    
    private void UpdateProfileImage(string? imageSource)
    {
        if (ProfileImage != null)
        {
            if (string.IsNullOrEmpty(imageSource))
            {
                ProfileImage.Source = null;
            }
            else if (imageSource.StartsWith("http://") || imageSource.StartsWith("https://"))
            {
                // URL image
                ProfileImage.Source = ImageSource.FromUri(new Uri(imageSource));
            }
            else
            {
                // Local file path
                ProfileImage.Source = ImageSource.FromFile(imageSource);
            }
        }
    }
}

