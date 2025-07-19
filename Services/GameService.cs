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
                System.Windows.MessageBox.Show($"=== GAMESERVICE SAVEGAMESTATE - PlayerId: {playerId}, GameId: {gameId} ===", "DEBUG", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                
                if (serviceProvider == null)
                {
                    System.Windows.MessageBox.Show("GameService: Service provider is null!", "DEBUG", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    throw new InvalidOperationException("Service provider is null");
                }
                
                System.Windows.MessageBox.Show("GameService: Service provider is available", "DEBUG", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                
                using var scope = serviceProvider.CreateScope();
                System.Windows.MessageBox.Show("GameService: Created service scope", "DEBUG", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                
                var context = scope.ServiceProvider.GetRequiredService<GameContext>();
                System.Windows.MessageBox.Show("GameService: Got GameContext from service provider", "DEBUG", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                
                System.Windows.MessageBox.Show($"GameService: Saving game state for PlayerId: {playerId}, GameId: {gameId}", "DEBUG", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                
                // Check if saved game for this game ID already exists
                System.Windows.MessageBox.Show("GameService: Checking for existing game...", "DEBUG", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                var existingGame = await context.SavedGames
                    .FirstOrDefaultAsync(g => g.PlayerId == playerId && g.GameId == gameId);
                
                System.Windows.MessageBox.Show($"GameService: Existing game found: {existingGame != null}", "DEBUG", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                
                if (existingGame != null)
                {
                    System.Windows.MessageBox.Show("GameService: Updating existing saved game", "DEBUG", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                    // Update existing saved game
                    existingGame.BoardStateJson = boardState;
                    existingGame.IsPlayerTurn = isPlayerTurn;
                    existingGame.SavedAt = DateTime.Now;
                    existingGame.GameStatus = "InProgress";
                    System.Windows.MessageBox.Show($"GameService: Updated existing saved game - Id: {existingGame.Id}", "DEBUG", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
                else
                {
                    System.Windows.MessageBox.Show("GameService: Creating new saved game", "DEBUG", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
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
                    System.Windows.MessageBox.Show($"GameService: Created new saved game object", "DEBUG", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
                
                System.Windows.MessageBox.Show("GameService: About to save changes to database", "DEBUG", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                var result = await context.SaveChangesAsync();
                System.Windows.MessageBox.Show($"GameService: SaveChangesAsync completed - affected rows: {result}", "DEBUG", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                System.Windows.MessageBox.Show($"GameService: Game state saved successfully", "DEBUG", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"GameService: ERROR saving game state: {ex.Message}", "DEBUG", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                System.Windows.MessageBox.Show($"GameService: Stack trace: {ex.StackTrace}", "DEBUG", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
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
                System.Windows.MessageBox.Show($"=== GAMESERVICE GETSAVEDGAMESFORPLAYER - PlayerId: {playerId} ===", "DEBUG", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                
                if (serviceProvider == null)
                {
                    System.Windows.MessageBox.Show("GameService: Service provider is null!", "DEBUG", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    throw new InvalidOperationException("Service provider is null");
                }
                
                using var scope = serviceProvider.CreateScope();
                System.Windows.MessageBox.Show("GameService: Created service scope for getting saved games", "DEBUG", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                
                var context = scope.ServiceProvider.GetRequiredService<GameContext>();
                System.Windows.MessageBox.Show("GameService: Got GameContext for getting saved games", "DEBUG", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                
                System.Windows.MessageBox.Show($"GameService: Getting saved games for PlayerId: {playerId}", "DEBUG", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                
                var savedGames = await context.SavedGames
                    .Where(g => g.PlayerId == playerId)
                    .OrderByDescending(g => g.SavedAt)
                    .ToListAsync();
                
                System.Windows.MessageBox.Show($"GameService: Found {savedGames.Count} saved games for player {playerId}", "DEBUG", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                
                // Log details of each saved game
                foreach (var game in savedGames)
                {
                    System.Windows.MessageBox.Show($"GameService: Saved game - Id: {game.Id}, GameId: {game.GameId}, Status: {game.GameStatus}, SavedAt: {game.SavedAt}", "DEBUG", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
                
                return savedGames;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"GameService: ERROR getting saved games: {ex.Message}", "DEBUG", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                System.Windows.MessageBox.Show($"GameService: Stack trace: {ex.StackTrace}", "DEBUG", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
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