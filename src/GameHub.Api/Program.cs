using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using GameHub.Api.Data;
using GameHub.Core.Models;

var builder = WebApplication.CreateBuilder(args);

// --- Services ---
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlite("Data Source=gamehub.db"));  // SQLite file in the API folder

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- CORS (allow local dev + emulator) ---
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("AllowAll",
        p => p
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

// --- Ensure database exists on startup ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.UseCors("AllowAll");

// --- Swagger for easy testing ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Simple health check
app.MapGet("/", () => Results.Ok("Game Hub API running"));

// Helper: validate using DataAnnotations
static (bool ok, Dictionary<string, string[]>? errors) Validate<T>(T model)
{
    var ctx = new ValidationContext(model!);
    var results = new List<ValidationResult>();
    var ok = Validator.TryValidateObject(model!, ctx, results, validateAllProperties: true);

    if (ok) return (true, null);

    var dict = results
        .GroupBy(r => r.MemberNames.FirstOrDefault() ?? string.Empty)
        .ToDictionary(
            g => string.IsNullOrWhiteSpace(g.Key) ? "General" : g.Key,
            g => g.Select(r => r.ErrorMessage ?? "Invalid value").ToArray()
        );

    return (false, dict);
}

// ====================== GAMES ======================
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

// ====================== PLAYERS ======================
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
