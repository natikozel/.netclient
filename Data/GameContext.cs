using Microsoft.EntityFrameworkCore;
using Connect4Client.Models;
using System.Text.Json;

namespace Connect4Client.Data
{
    public class GameContext : DbContext
    {
        public DbSet<SavedGame> SavedGames { get; set; } = default!;

        public GameContext(DbContextOptions<GameContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Ensure GameId is properly configured
            modelBuilder.Entity<SavedGame>()
                .Property(e => e.GameId)
                .IsRequired();

            // Add unique constraint for PlayerId + GameId combination
            modelBuilder.Entity<SavedGame>()
                .HasIndex(e => new { e.PlayerId, e.GameId })
                .IsUnique();

            base.OnModelCreating(modelBuilder);
        }
    }

    public class SavedGame
    {
        public int Id { get; set; }
        public int PlayerId { get; set; }
        public int GameId { get; set; }
        public string BoardStateJson { get; set; } = "";
        public bool IsPlayerTurn { get; set; }
        public DateTime SavedAt { get; set; }
        public string GameStatus { get; set; } = "InProgress";
    }
} 