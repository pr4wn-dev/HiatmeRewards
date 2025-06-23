using HiatMeApp.Services;
using HiatMeApp.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls.Hosting;
using System;

namespace HiatMeApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Configure named HttpClient
        builder.Services.AddHttpClient("HiatmeApi", client =>
        {
            client.BaseAddress = new Uri("https://hiatme.com");
            client.DefaultRequestHeaders.Add("User-Agent", "HiatMeApp/1.0"); // Move User-Agent here
        });

        // Register AuthService with explicit HttpClient factory
        builder.Services.AddSingleton<AuthService>(serviceProvider =>
        {
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient("HiatmeApi");
            return new AuthService(httpClient);
        });

        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<RegisterViewModel>();
        builder.Services.AddTransient<ForgotPasswordViewModel>();
        builder.Services.AddTransient<HomeViewModel>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<RegisterPage>();
        builder.Services.AddTransient<ForgotPasswordPage>();
        builder.Services.AddTransient<HomePage>();

        return builder.Build();
    }
}