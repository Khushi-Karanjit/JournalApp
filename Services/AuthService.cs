using Microsoft.Maui.Storage;

namespace JournalApp.Services;

public class AuthService
{
    private const string PinKey = "APP_PIN";
    private const string UserKey = "APP_USERNAME";

    public bool IsLoggedIn { get; private set; }

    // Avoid SecureStorage.GetAsync(...).Result. Use this cached flag instead.
    public bool HasAccount { get; private set; }

    public string? Username { get; private set; }

    /// <summary>
    /// Call once at startup to populate HasAccount + Username.
    /// </summary>
    public async Task InitializeAsync()
    {
        Username = Preferences.Get(UserKey, null);
        var pin = await SecureStorage.GetAsync(PinKey);
        HasAccount = !string.IsNullOrWhiteSpace(pin);

        // If no PIN exists, user is effectively not logged in
        if (!HasAccount) IsLoggedIn = false;
    }

    public event Action? OnAuthStateChanged;

    // ✅ These two methods are what your UI is asking for
    public async Task SetPinAsync(string pin)
    {
        if (string.IsNullOrWhiteSpace(pin))
            throw new ArgumentException("PIN cannot be empty.", nameof(pin));

        await SecureStorage.SetAsync(PinKey, pin);

        HasAccount = true;
        IsLoggedIn = true; // IMPORTANT
        OnAuthStateChanged?.Invoke();
    }


    public void SetUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username cannot be empty.", nameof(username));

        Preferences.Set(UserKey, username);
        Username = username;
    }

    // Keep your existing Register flow but implement using the setters
    public async Task RegisterAsync(string username, string pin)
    {
        SetUsername(username);
        await SetPinAsync(pin);
        IsLoggedIn = true;
        OnAuthStateChanged?.Invoke();
    }

    public async Task<bool> LoginAsync(string pin)
    {
        var stored = await SecureStorage.GetAsync(PinKey);

        if (string.IsNullOrWhiteSpace(stored))
        {
            HasAccount = false;
            IsLoggedIn = false;
            OnAuthStateChanged?.Invoke();
            return false;
        }

        HasAccount = true;

        if (stored.Trim() == pin.Trim())
        {
            IsLoggedIn = true;
            OnAuthStateChanged?.Invoke();
            return true;
        }

        IsLoggedIn = false;
        OnAuthStateChanged?.Invoke();
        return false;
    }

    public async Task<bool> VerifyPinAsync(string pin)
    {
        var stored = await SecureStorage.GetAsync(PinKey);
        if (string.IsNullOrWhiteSpace(stored)) return false;
        return stored.Trim() == pin.Trim();
    }


    public void Logout()
    {
        IsLoggedIn = false;
        OnAuthStateChanged?.Invoke();
    }

    // Optional helper (useful during testing)
    public void ClearUsername()
    {
        Preferences.Remove(UserKey);
        Username = null;
    }

    public void ClearPin()
    {
        SecureStorage.Remove(PinKey);
        HasAccount = false;
        IsLoggedIn = false;
        OnAuthStateChanged?.Invoke();
    }
}
