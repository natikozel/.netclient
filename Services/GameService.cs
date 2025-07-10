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
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<GameContext>();
            
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
            }
            
            await context.SaveChangesAsync();
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
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<GameContext>();
            return await context.SavedGames
                .Where(g => g.PlayerId == playerId)
                .OrderByDescending(g => g.SavedAt)
                .ToListAsync();
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
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<GameContext>();
            
            var savedGame = await context.SavedGames
                .FirstOrDefaultAsync(g => g.PlayerId == playerId && g.GameId == gameId);
            
            if (savedGame != null)
            {
                savedGame.GameStatus = status;
                await context.SaveChangesAsync();
            }
        }
    }
} 