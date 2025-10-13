using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GameHub.Mobile.Services;

public sealed class ApiClient
{
    // CHANGE PORT IF YOUR API SHOWS A DIFFERENT ONE IN THE TERMINAL
    private const string BaseUrl = "http://localhost:5280";

    private readonly HttpClient _http = new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(15)
    };

    private string? _jwt;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    // ===== Auth =====
    public async Task<string?> RegisterAsync(string email, string password)
    {
        var res = await _http.PostAsync($"{BaseUrl}/api/auth/register",
            JsonContent.Create(new { email, password }, options: JsonOpts));
        if (!res.IsSuccessStatusCode) return null;
        var obj = await res.Content.ReadFromJsonAsync<AuthResponse>(JsonOpts);
        _jwt = obj?.Token;
        SetBearer();
        return _jwt;
    }

    public async Task<string?> LoginAsync(string email, string password)
    {
        var res = await _http.PostAsync($"{BaseUrl}/api/auth/login",
            JsonContent.Create(new { email, password }, options: JsonOpts));
        if (!res.IsSuccessStatusCode) return null;
        var obj = await res.Content.ReadFromJsonAsync<AuthResponse>(JsonOpts);
        _jwt = obj?.Token;
        SetBearer();
        return _jwt;
    }

    private void SetBearer()
    {
        if (!string.IsNullOrWhiteSpace(_jwt))
        {
            _http.DefaultRequestHeaders.Remove("Authorization");
            _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {_jwt}");
        }
    }

    // ===== Models =====
    public record Game(int Id, string Name, string? Genre, int? ReleaseYear);
    public record Player(int Id, string Name, string? Email);

    private record AuthResponse(int Id, string Email, string Token);

    // ===== Games CRUD =====
    public async Task<List<Game>> GetGamesAsync(string? search = null)
    {
        var url = string.IsNullOrWhiteSpace(search)
            ? $"{BaseUrl}/api/games"
            : $"{BaseUrl}/api/games?search={Uri.EscapeDataString(search)}";
        var data = await _http.GetFromJsonAsync<List<Game>>(url, JsonOpts);
        return data ?? new List<Game>();
    }

    public async Task<Game?> AddGameAsync(string name, string? genre, int? releaseYear)
    {
        var res = await _http.PostAsync($"{BaseUrl}/api/games",
            JsonContent.Create(new { name, genre, releaseYear }, options: JsonOpts));
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<Game>(JsonOpts);
    }

    public async Task<bool> UpdateGameAsync(Game g)
    {
        var res = await _http.PutAsync($"{BaseUrl}/api/games/{g.Id}",
            JsonContent.Create(g, options: JsonOpts));
        return res.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteGameAsync(int id)
    {
        var res = await _http.DeleteAsync($"{BaseUrl}/api/games/{id}");
        return res.IsSuccessStatusCode;
    }

    // ===== Players CRUD =====
    public async Task<List<Player>> GetPlayersAsync(string? search = null)
    {
        var url = string.IsNullOrWhiteSpace(search)
            ? $"{BaseUrl}/api/players"
            : $"{BaseUrl}/api/players?search={Uri.EscapeDataString(search)}";
        var data = await _http.GetFromJsonAsync<List<Player>>(url, JsonOpts);
        return data ?? new List<Player>();
    }

    public async Task<Player?> AddPlayerAsync(string name, string? email)
    {
        var res = await _http.PostAsync($"{BaseUrl}/api/players",
            JsonContent.Create(new { name, email }, options: JsonOpts));
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<Player>(JsonOpts);
    }

    public async Task<bool> UpdatePlayerAsync(Player p)
    {
        var res = await _http.PutAsync($"{BaseUrl}/api/players/{p.Id}",
            JsonContent.Create(p, options: JsonOpts));
        return res.IsSuccessStatusCode;
    }

    public async Task<bool> DeletePlayerAsync(int id)
    {
        var res = await _http.DeleteAsync($"{BaseUrl}/api/players/{id}");
        return res.IsSuccessStatusCode;
    }
}
