using Microsoft.Maui.Storage;
using Microsoft.JSInterop;

namespace JournalApp.Services;

public class ThemeService
{
    private const string ThemeKey = "APP_THEME";

    public string CurrentTheme { get; private set; } = "light";

    public async Task InitializeAsync(IJSRuntime js)
    {
        var saved = Preferences.Get(ThemeKey, "light");
        CurrentTheme = saved;
        await ApplyThemeAsync(js, saved);
    }

    public async Task SetThemeAsync(string theme, IJSRuntime js)
    {
        var normalized = theme == "dark" ? "dark" : "light";
        CurrentTheme = normalized;
        Preferences.Set(ThemeKey, normalized);
        await ApplyThemeAsync(js, normalized);
    }

    private static Task ApplyThemeAsync(IJSRuntime js, string theme)
    {
        return js.InvokeVoidAsync("theme.setTheme", theme).AsTask();
    }
}
