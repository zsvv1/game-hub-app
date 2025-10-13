using GameHub.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace GameHub.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Game> Games => Set<Game>();
        public DbSet<Player> Players => Set<Player>();
    }
}
