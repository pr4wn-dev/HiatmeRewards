using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace HiatMeApp.Helpers;

public static class PageDialogService
{
    private static Page? GetActivePage()
    {
        return Application.Current?.Windows.FirstOrDefault()?.Page;
    }

    public static async Task DisplayAlertAsync(string title, string message, string cancel = "OK")
    {
        var page = GetActivePage();
        if (page != null)
        {
            await page.DisplayAlert(title, message, cancel);
        }
    }

    public static async Task<bool> DisplayAlertAsync(string title, string message, string accept, string cancel)
    {
        var page = GetActivePage();
        if (page == null)
        {
            return false;
        }

        return await page.DisplayAlert(title, message, accept, cancel);
    }

    public static async Task<string?> DisplayPromptAsync(
        string title,
        string message,
        string accept = "OK",
        string cancel = "Cancel",
        string? placeholder = null,
        int maxLength = -1,
        Keyboard? keyboard = null,
        string? initialValue = null)
    {
        var page = GetActivePage();
        if (page == null)
        {
            return null;
        }

        return await page.DisplayPromptAsync(title, message, accept, cancel, placeholder, maxLength, keyboard ?? Keyboard.Default, initialValue);
    }

    public static async Task<string?> DisplayActionSheetAsync(string title, string cancel, string? destruction, params string[] buttons)
    {
        var page = GetActivePage();
        if (page == null)
        {
            return null;
        }

        return await page.DisplayActionSheet(title, cancel, destruction, buttons);
    }
}

