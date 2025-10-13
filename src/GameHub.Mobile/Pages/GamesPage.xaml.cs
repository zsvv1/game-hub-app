using GameHub.Mobile.Services;

namespace GameHub.Mobile.Pages;

public partial class GamesPage : ContentPage
{
    private readonly ApiClient _api = new();
    private ApiClient.Game? _editing;

    public GamesPage()
    {
        InitializeComponent();

        SearchButton.Clicked += async (_, __) => await LoadAsync(SearchEntry.Text);
        AddButton.Clicked += async (_, __) => await AddOrUpdateAsync();

        _ = LoadAsync(null);
    }

    private async Task LoadAsync(string? search)
    {
        StatusLabel.Text = "";
        var items = await _api.GetGamesAsync(search);
        GamesList.ItemsSource = items;
    }

    private async Task AddOrUpdateAsync()
    {
        StatusLabel.Text = "";

        var name = NameEntry.Text?.Trim();
        var genre = GenreEntry.Text?.Trim();
        int? year = null;
        if (int.TryParse(YearEntry.Text, out var y)) year = y;

        if (string.IsNullOrWhiteSpace(name))
        {
            StatusLabel.Text = "Name is required.";
            return;
        }

        if (_editing is null)
        {
            var created = await _api.AddGameAsync(name!, genre, year);
            if (created is null) { StatusLabel.Text = "Create failed."; return; }
        }
        else
        {
            var updated = await _api.UpdateGameAsync(new ApiClient.Game(_editing.Id, name!, genre, year));
            if (!updated) { StatusLabel.Text = "Update failed."; return; }
            _editing = null;
            AddButton.Text = "Add / Update";
        }

        NameEntry.Text = GenreEntry.Text = YearEntry.Text = "";
        await LoadAsync(SearchEntry.Text);
    }

    // === Item button handlers wired in XAML ===
    private void OnEditClicked(object? sender, EventArgs e)
    {
        if (sender is Button b && b.CommandParameter is ApiClient.Game g)
        {
            _editing = g;
            NameEntry.Text = g.Name;
            GenreEntry.Text = g.Genre;
            YearEntry.Text = g.ReleaseYear?.ToString() ?? "";
            AddButton.Text = "Update";
        }
    }

    private async void OnDeleteClicked(object? sender, EventArgs e)
    {
        if (sender is Button b && b.CommandParameter is ApiClient.Game g)
        {
            var ok = await _api.DeleteGameAsync(g.Id);
            if (!ok) { StatusLabel.Text = "Delete failed."; return; }
            await LoadAsync(SearchEntry.Text);
        }
    }
}
