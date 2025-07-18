using Connect4Client.Data;
using Connect4Client.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Connect4Client.Services
{
    public class GameService
    {
        private readonly IServiceProvider serviceProvider;

        public GameService()
        {
            // Get service provider from Application
            serviceProvider = ((App)App.Current).Services ?? throw new InvalidOperationException("Service provider not initialized");
        }

        public async Task SaveGameState(int playerId, int[,] boardState, bool isPlayerTurn, int gameId)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<GameContext>();
                
                System.Diagnostics.Debug.WriteLine($"GameService: Saving game state for PlayerId: {playerId}, GameId: {gameId}");
                
                // Check if saved game for this game ID already exists
                var existingGame = await context.SavedGames
                    .FirstOrDefaultAsync(g => g.PlayerId == playerId && g.GameId == gameId);
                
                if (existingGame != null)
                {
                    // Update existing saved game
                    existingGame.BoardStateJson = boardState;
                    existingGame.IsPlayerTurn = isPlayerTurn;
                    existingGame.SavedAt = DateTime.Now;
                    existingGame.GameStatus = "InProgress";
                    System.Diagnostics.Debug.WriteLine($"GameService: Updated existing saved game");
                }
                else
                {
                    // Create new saved game
                    var savedGame = new SavedGame
                    {
                        PlayerId = playerId,
                        GameId = gameId,
                        BoardStateJson = boardState,
                        IsPlayerTurn = isPlayerTurn,
                        SavedAt = DateTime.Now,
                        GameStatus = "InProgress"
                    };
                    
                    context.SavedGames.Add(savedGame);
                    System.Diagnostics.Debug.WriteLine($"GameService: Created new saved game");
                }
                
                await context.SaveChangesAsync();
                System.Diagnostics.Debug.WriteLine($"GameService: Game state saved successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GameService: Error saving game state: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"GameService: Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task<GameState?> LoadGameState(int playerId)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<GameContext>();
            
            var savedGame = await context.SavedGames
                .FirstOrDefaultAsync(g => g.PlayerId == playerId);
            
            if (savedGame == null)
                return null;
            
            return new GameState
            {
                Id = savedGame.Id,
                PlayerId = savedGame.PlayerId,
                BoardState = savedGame.BoardStateJson,
                IsPlayerTurn = savedGame.IsPlayerTurn,
                SavedAt = savedGame.SavedAt,
                GameStatus = savedGame.GameStatus
            };
        }

        public async Task DeleteSavedGame(int playerId)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<GameContext>();
            
            var savedGame = await context.SavedGames
                .FirstOrDefaultAsync(g => g.PlayerId == playerId);
            
            if (savedGame != null)
            {
                context.SavedGames.Remove(savedGame);
                await context.SaveChangesAsync();
            }
        }

        public async Task<List<SavedGame>> GetAllSavedGames()
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<GameContext>();
            return await context.SavedGames.ToListAsync();
        }

        public async Task<List<SavedGame>> GetSavedGamesForPlayer(int playerId)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<GameContext>();
                
                System.Diagnostics.Debug.WriteLine($"GameService: Getting saved games for PlayerId: {playerId}");
                
                var savedGames = await context.SavedGames
                    .Where(g => g.PlayerId == playerId)
                    .OrderByDescending(g => g.SavedAt)
                    .ToListAsync();
                
                System.Diagnostics.Debug.WriteLine($"GameService: Found {savedGames.Count} saved games for player {playerId}");
                return savedGames;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GameService: Error getting saved games: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"GameService: Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task<SavedGame?> GetSavedGameById(int savedGameId)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<GameContext>();
            return await context.SavedGames
                .FirstOrDefaultAsync(g => g.Id == savedGameId);
        }

        public async Task UpdateGameStatus(int playerId, int gameId, string status)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<GameContext>();
                
                var savedGame = await context.SavedGames
                    .FirstOrDefaultAsync(g => g.PlayerId == playerId && g.GameId == gameId);
                
                if (savedGame != null)
                {
                    savedGame.GameStatus = status;
                    await context.SaveChangesAsync();
                    System.Diagnostics.Debug.WriteLine($"GameService: Updated game status to {status}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"GameService: No saved game found to update status");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GameService: Error updating game status: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> TestDatabaseConnection()
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<GameContext>();
                
                // Try to access the database
                var count = await context.SavedGames.CountAsync();
                System.Diagnostics.Debug.WriteLine($"GameService: Database connection test successful. Found {count} saved games.");
                
                // Test saving a dummy record
                var testGame = new SavedGame
                {
                    PlayerId = 999,
                    GameId = 999,
                    BoardStateJson = new int[6, 7],
                    IsPlayerTurn = true,
                    SavedAt = DateTime.Now,
                    GameStatus = "Test"
                };
                
                context.SavedGames.Add(testGame);
                await context.SaveChangesAsync();
                
                // Remove the test record
                context.SavedGames.Remove(testGame);
                await context.SaveChangesAsync();
                
                System.Diagnostics.Debug.WriteLine($"GameService: Database write test successful.");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GameService: Database connection test failed: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"GameService: Stack trace: {ex.StackTrace}");
                return false;
            }
        }
    }
} 