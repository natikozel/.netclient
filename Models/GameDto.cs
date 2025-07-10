namespace Connect4Client.Models
{
    public class GameDto
    {
        public int Id { get; set; }
        public int PlayerId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public int[,] Board { get; set; } = new int[6, 7];
        public string CurrentPlayer { get; set; } = string.Empty;
        public string? Winner { get; set; }
        public PlayerDto? Player { get; set; }
    }

    public class PlayerDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public int PlayerId { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public int GamesPlayed { get; set; }
        public int GamesWon { get; set; }
        public int GamesLost { get; set; }
    }

    public class MakeMoveRequest
    {
        public int GameId { get; set; }
        public int Column { get; set; }
    }

    public class MakeMoveResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public GameDto? Game { get; set; }
        public int? CpuMove { get; set; }
    }

    public class StartGameRequest
    {
        public int PlayerId { get; set; }
    }

    public class StartGameResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public GameDto? Game { get; set; }
    }
} 