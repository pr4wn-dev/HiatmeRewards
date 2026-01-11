using HiatMeApp.Converters;
using HiatMeApp.Controls;
using HiatMeApp.Services;
using HiatMeApp.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
using System;

namespace HiatMeApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp(provider => new App(builder.Build()))
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("MaterialIcons-Regular.ttf", "MaterialIcons");
            });

        builder.Services.AddHttpClient("HiatmeApi", client =>
        {
            client.BaseAddress = new Uri("https://hiatme.com");
            client.DefaultRequestHeaders.Add("User-Agent", "HiatMeApp/1.0");
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            UseCookies = true,
            CookieContainer = new System.Net.CookieContainer()
        });

        builder.Services.AddSingleton<AuthService>(serviceProvider =>
        {
            var httpClient = serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient("HiatmeApi");
            return new AuthService(httpClient);
        });

        builder.Services.AddSingleton<LocationService>(serviceProvider =>
        {
            var httpClient = serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient("HiatmeApi");
            return new LocationService(httpClient);
        });

        builder.Services.AddSingleton<IValueConverter, BoolToEyeIconConverter>();
        builder.Services.AddSingleton<IValueConverter, NotNullConverter>();
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<RegisterViewModel>();
        builder.Services.AddTransient<ForgotPasswordViewModel>();
        builder.Services.AddTransient<HomeViewModel>();
        builder.Services.AddTransient<VehicleViewModel>();
        builder.Services.AddTransient<VehicleIssuesViewModel>();
        builder.Services.AddTransient<FinishDayViewModel>();
        builder.Services.AddTransient<ProfileViewModel>();
        builder.Services.AddTransient<RequestDayOffViewModel>();
        builder.Services.AddTransient<ViewLogViewModel>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<RegisterPage>();
        builder.Services.AddTransient<ForgotPasswordPage>();
        builder.Services.AddTransient<HomePage>();
        builder.Services.AddTransient<VehiclePage>();
        builder.Services.AddTransient<ClientPage>();
        builder.Services.AddTransient<DriverPage>();
        builder.Services.AddTransient<ManagerPage>();
        builder.Services.AddTransient<OwnerPage>();
        builder.Services.AddTransient<VehicleIssuesPage>();
        builder.Services.AddTransient<FinishDayPage>();
        builder.Services.AddTransient<ProfilePage>();
        builder.Services.AddTransient<RequestDayOffPage>();
        builder.Services.AddTransient<NavigationBar>();

        return builder.Build();
    }
}