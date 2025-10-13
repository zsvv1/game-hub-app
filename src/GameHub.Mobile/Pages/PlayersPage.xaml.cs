using GameHub.Mobile.Services;

namespace GameHub.Mobile.Pages;

public partial class PlayersPage : ContentPage
{
    private readonly ApiClient _api = new();
    private ApiClient.Player? _editing;

    public PlayersPage()
    {
        InitializeComponent();

        SearchButton.Clicked += async (_, __) => await LoadAsync(SearchEntry.Text);
        AddButton.Clicked += async (_, __) => await AddOrUpdateAsync();

        _ = LoadAsync(null);
    }

    private async Task LoadAsync(string? search)
    {
        StatusLabel.Text = "";
        var items = await _api.GetPlayersAsync(search);
        PlayersList.ItemsSource = items;
    }

    private async Task AddOrUpdateAsync()
    {
        StatusLabel.Text = "";

        var name = NameEntry.Text?.Trim();
        var email = EmailEntry.Text?.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            StatusLabel.Text = "Name is required.";
            return;
        }

        if (_editing is null)
        {
            var created = await _api.AddPlayerAsync(name!, email);
            if (created is null) { StatusLabel.Text = "Create failed."; return; }
        }
        else
        {
            var updated = await _api.UpdatePlayerAsync(new ApiClient.Player(_editing.Id, name!, email));
            if (!updated) { StatusLabel.Text = "Update failed."; return; }
            _editing = null;
            AddButton.Text = "Add / Update";
        }

        NameEntry.Text = EmailEntry.Text = "";
        await LoadAsync(SearchEntry.Text);
    }

    // === Item button handlers wired in XAML ===
    private void OnEditClicked(object? sender, EventArgs e)
    {
        if (sender is Button b && b.CommandParameter is ApiClient.Player p)
        {
            _editing = p;
            NameEntry.Text = p.Name;
            EmailEntry.Text = p.Email ?? "";
            AddButton.Text = "Update";
        }
    }

    private async void OnDeleteClicked(object? sender, EventArgs e)
    {
        if (sender is Button b && b.CommandParameter is ApiClient.Player p)
        {
            var ok = await _api.DeletePlayerAsync(p.Id);
            if (!ok) { StatusLabel.Text = "Delete failed."; return; }
            await LoadAsync(SearchEntry.Text);
        }
    }
}
