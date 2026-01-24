using System.Security.Cryptography;
using System.Text;
using JournalApp.Data;
using JournalApp.Models;

namespace JournalApp.Services;

public class AuthService
{
    private User? _currentUser;

    public bool IsLoggedIn { get; private set; }
    public bool HasAccount { get; private set; }
    public string? Username => _currentUser?.Username;
    public string? UserId => _currentUser?.Id;

    public async Task InitializeAsync()
    {
        _currentUser = await JournalDatabase.GetUserAsync();
        
        if (_currentUser != null && !string.IsNullOrEmpty(_currentUser.PinHash))
        {
            HasAccount = true;
        }
        else
        {
            HasAccount = false;
        }

        // If not logged in, IsLoggedIn remains false (default)
        if (!HasAccount) IsLoggedIn = false;
    }

    public event Action? OnAuthStateChanged;

    public async Task SetPinAsync(string pin)
    {
        if (string.IsNullOrWhiteSpace(pin))
            throw new ArgumentException("PIN cannot be empty.", nameof(pin));

        if (_currentUser == null)
        {
             _currentUser = new User();
        }

        _currentUser.PinHash = HashPin(pin);
        
        await JournalDatabase.SaveUserAsync(_currentUser);

        HasAccount = true;
        IsLoggedIn = true; 
        OnAuthStateChanged?.Invoke();
    }

    public async Task SetUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username cannot be empty.", nameof(username));

        if (_currentUser == null)
        {
            _currentUser = new User { Username = username };
        }
        else
        {
            _currentUser.Username = username;
        }

        await JournalDatabase.SaveUserAsync(_currentUser);
    }

    public async Task RegisterAsync(string username, string pin)
    {
        // Ensure user object exists or verify uniqueness if we supported multiple users
        if (_currentUser == null) _currentUser = new User();
        
        _currentUser.Username = username;
        _currentUser.PinHash = HashPin(pin);

        await JournalDatabase.SaveUserAsync(_currentUser);

        HasAccount = true;
        IsLoggedIn = true;
        OnAuthStateChanged?.Invoke();
    }

    public async Task<bool> LoginAsync(string pin)
    {
        if (_currentUser == null) await InitializeAsync();
        
        if (_currentUser == null || string.IsNullOrEmpty(_currentUser.PinHash))
        {
            HasAccount = false;
            IsLoggedIn = false;
            OnAuthStateChanged?.Invoke();
            return false;
        }

        HasAccount = true;

        if (VerifyHash(pin, _currentUser.PinHash))
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
        if (_currentUser == null) await InitializeAsync();
        if (_currentUser == null || string.IsNullOrEmpty(_currentUser.PinHash)) return false;
        
        return VerifyHash(pin, _currentUser.PinHash);
    }

    public void Logout()
    {
        IsLoggedIn = false;
        OnAuthStateChanged?.Invoke();
    }

    public async Task ClearUsername()
    {
        if (_currentUser != null)
        {
            _currentUser.Username = "Default User";
            await JournalDatabase.SaveUserAsync(_currentUser);
        }
    }

    public async Task ClearPin()
    {
        if (_currentUser != null)
        {
            _currentUser.PinHash = null;
            await JournalDatabase.SaveUserAsync(_currentUser);
            HasAccount = false;
            IsLoggedIn = false;
            OnAuthStateChanged?.Invoke();
        }
    }

    private static string HashPin(string pin)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(pin);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    private static bool VerifyHash(string pin, string storedHash)
    {
        var hash = HashPin(pin);
        return hash == storedHash;
    }
}
