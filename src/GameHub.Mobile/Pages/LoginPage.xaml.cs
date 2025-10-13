using GameHub.Mobile.Services;

namespace GameHub.Mobile.Pages;

public partial class LoginPage : ContentPage
{
    private readonly ApiClient _api = new();

    public LoginPage()
    {
        InitializeComponent();

        LoginButton.Clicked += OnLoginClicked;
        RegisterButton.Clicked += OnRegisterClicked;
    }

    private async void OnRegisterClicked(object? sender, EventArgs e)
    {
        StatusLabel.Text = "";
        var email = EmailEntry.Text?.Trim() ?? "";
        var pwd = PasswordEntry.Text ?? "";

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(pwd))
        {
            StatusLabel.Text = "Email and password are required.";
            return;
        }

        var token = await _api.RegisterAsync(email, pwd);
        if (token is null)
        {
            StatusLabel.Text = "Registration failed.";
            return;
        }

        await Shell.Current.GoToAsync("//GamesPage");
    }

    private async void OnLoginClicked(object? sender, EventArgs e)
    {
        StatusLabel.Text = "";
        var email = EmailEntry.Text?.Trim() ?? "";
        var pwd = PasswordEntry.Text ?? "";

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(pwd))
        {
            StatusLabel.Text = "Email and password are required.";
            return;
        }

        var token = await _api.LoginAsync(email, pwd);
        if (token is null)
        {
            StatusLabel.Text = "Invalid credentials.";
            return;
        }

        await Shell.Current.GoToAsync("//GamesPage");
    }
}
