using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GameHub.Api.Data;
using GameHub.Core.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using GameHub.Api;


var builder = WebApplication.CreateBuilder(args);

// ===== Services =====
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlite("Data Source=gamehub.db"));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS (dev-friendly)
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("AllowAll", p => p
        .AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod());
});

// JWT/Auth
var jwtKey = "super_secret_dev_key_12345_change_me";
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Ensure DB exists
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Health check
app.MapGet("/", () => Results.Ok("Game Hub API running"));

// Helper: DataAnnotations validation
static (bool ok, Dictionary<string, string[]>? errors) Validate<T>(T model)
{
    var ctx = new ValidationContext(model!);
    var results = new List<ValidationResult>();
    var valid = Validator.TryValidateObject(model!, ctx, results, validateAllProperties: true);
    if (valid) return (true, null);

    var dict = results
        .GroupBy(r => r.MemberNames.FirstOrDefault() ?? "General")
        .ToDictionary(g => g.Key, g => g.Select(r => r.ErrorMessage ?? "Invalid value").ToArray());

    return (false, dict);
}

// ===== JWT helper =====
string GenerateJwt(User user)
{
    var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
    var claims = new[]
    {
        new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
        new Claim(JwtRegisteredClaimNames.Email, user.Email),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    };
    var token = new JwtSecurityToken(
        claims: claims,
        notBefore: DateTime.UtcNow,
        expires: DateTime.UtcNow.AddHours(8),
        signingCredentials: creds);
    return new JwtSecurityTokenHandler().WriteToken(token);
}

// ===== AUTH =====
app.MapPost("/api/auth/register", async (RegisterRequest req, AppDbContext db) =>
{
    var (ok, errors) = Validate(req);
    if (!ok) return Results.ValidationProblem(errors!);

    var exists = await db.Users.AnyAsync(u => u.Email == req.Email);
    if (exists) return Results.BadRequest(new { message = "Email already registered." });

    var user = new User
    {
        Email = req.Email,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password)
    };

    db.Users.Add(user);
    await db.SaveChangesAsync();

    var token = GenerateJwt(user);
    return Results.Created($"/api/users/{user.Id}", new { user.Id, user.Email, token });
});

app.MapPost("/api/auth/login", async (LoginRequest req, AppDbContext db) =>
{
    var (ok, errors) = Validate(req);
    if (!ok) return Results.ValidationProblem(errors!);

    var user = await db.Users.SingleOrDefaultAsync(u => u.Email == req.Email);
    if (user is null) return Results.BadRequest(new { message = "Invalid credentials." });

    var valid = BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash);
    if (!valid) return Results.BadRequest(new { message = "Invalid credentials." });

    var token = GenerateJwt(user);
    return Results.Ok(new { user.Id, user.Email, token });
});

app.MapGet("/api/me", async (ClaimsPrincipal me, AppDbContext db) =>
{
    var idClaim = me.FindFirstValue(JwtRegisteredClaimNames.Sub);
    if (string.IsNullOrEmpty(idClaim)) return Results.Unauthorized();
    if (!int.TryParse(idClaim, out var id)) return Results.Unauthorized();

    var user = await db.Users.FindAsync(id);
    if (user is null) return Results.Unauthorized();

    return Results.Ok(new { user.Id, user.Email, user.CreatedAt });
}).RequireAuthorization();

// ===== GAMES =====
app.MapGet("/api/games", async (string? search, AppDbContext db) =>
{
    var q = db.Games.AsQueryable();
    if (!string.IsNullOrWhiteSpace(search))
        q = q.Where(g => g.Name.Contains(search));
    var list = await q.OrderBy(g => g.Name).ToListAsync();
    return Results.Ok(list);
});

app.MapGet("/api/games/{id:int}", async (int id, AppDbContext db) =>
{
    var game = await db.Games.FindAsync(id);
    return game is null ? Results.NotFound() : Results.Ok(game);
});

app.MapPost("/api/games", async (Game input, AppDbContext db) =>
{
    var v = Validate(input);
    if (!v.ok) return Results.ValidationProblem(v.errors!);

    db.Games.Add(input);
    await db.SaveChangesAsync();
    return Results.Created($"/api/games/{input.Id}", input);
});

app.MapPut("/api/games/{id:int}", async (int id, Game input, AppDbContext db) =>
{
    if (id != input.Id) return Results.BadRequest(new { message = "Id mismatch" });

    var v = Validate(input);
    if (!v.ok) return Results.ValidationProblem(v.errors!);

    var existing = await db.Games.FindAsync(id);
    if (existing is null) return Results.NotFound();

    existing.Name = input.Name;
    existing.Genre = input.Genre;
    existing.ReleaseYear = input.ReleaseYear;

    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.MapDelete("/api/games/{id:int}", async (int id, AppDbContext db) =>
{
    var existing = await db.Games.FindAsync(id);
    if (existing is null) return Results.NotFound();

    db.Games.Remove(existing);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

// ===== PLAYERS =====
app.MapGet("/api/players", async (string? search, AppDbContext db) =>
{
    var q = db.Players.AsQueryable();
    if (!string.IsNullOrWhiteSpace(search))
        q = q.Where(p => p.Name.Contains(search));
    var list = await q.OrderBy(p => p.Name).ToListAsync();
    return Results.Ok(list);
});

app.MapGet("/api/players/{id:int}", async (int id, AppDbContext db) =>
{
    var player = await db.Players.FindAsync(id);
    return player is null ? Results.NotFound() : Results.Ok(player);
});

app.MapPost("/api/players", async (Player input, AppDbContext db) =>
{
    var v = Validate(input);
    if (!v.ok) return Results.ValidationProblem(v.errors!);

    db.Players.Add(input);
    await db.SaveChangesAsync();
    return Results.Created($"/api/players/{input.Id}", input);
});

app.MapPut("/api/players/{id:int}", async (int id, Player input, AppDbContext db) =>
{
    if (id != input.Id) return Results.BadRequest(new { message = "Id mismatch" });

    var v = Validate(input);
    if (!v.ok) return Results.ValidationProblem(v.errors!);

    var existing = await db.Players.FindAsync(id);
    if (existing is null) return Results.NotFound();

    existing.Name = input.Name;
    existing.Email = input.Email;

    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.MapDelete("/api/players/{id:int}", async (int id, AppDbContext db) =>
{
    var existing = await db.Players.FindAsync(id);
    if (existing is null) return Results.NotFound();

    db.Players.Remove(existing);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.Run();
