namespace TicTacToeServer.Models
{
    public class GameRoom
    {
        public string RoomId { get; set; }
        public string? PlayerXName { get; set; }
        public string? PlayerOName { get; set; }
        public string? PlayerXId { get; set; }
        public string? PlayerOId { get; set; }
        public string? PlayerXConnectionId { get; set; }
        public string? PlayerOConnectionId { get; set; }
        public char[][] Board { get; set; }
        public char CurrentTurn { get; set; }
        public bool IsGameOver { get; set; }
        public bool IsPendingRemoval { get; set; }
        public string? Winner { get; set; }
        public bool PlayerXPendingRemoval { get; set; }
        public bool PlayerOPendingRemoval { get; set; }
        public HashSet<string> PlayersHistory { get; set; } = new HashSet<string>();
        public GameRoom(string roomId)
        {
            RoomId = roomId;
            Board = new []
            {
                new [] { ' ', ' ', ' ' },
                new [] { ' ', ' ', ' ' },
                new [] { ' ', ' ', ' ' }
            };
            CurrentTurn = 'X';
            IsGameOver = false;
        }
    }
}