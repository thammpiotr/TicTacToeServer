namespace TicTacToeServer.Models
{
    public static class TicTacToeGame
    {
        public static bool IsValidMove(GameRoom room, int row, int col, char player)
        {
            if (room.Board[row][col] != ' ') Console.WriteLine($"Field at Row: {row}, Col: {col} is already occupied by {room.Board[row][col]}");
            if (room.CurrentTurn != player) return false;
            if (row < 0 || row > 2 || col < 0 || col > 2) return false;
            if (room.Board[row][col] != ' ') return false;
            return true;
        }

        public static void MakeMove(GameRoom room, int row, int col, char player)
        {
            room.Board[row][col] = player;
            
            if (CheckWin(room.Board, player))
            {
                room.IsGameOver = true;
                return;
            }
            
            if (IsBoardFull(room.Board))
            {
                room.IsGameOver = true;
                return;
            }
            
            room.CurrentTurn = (player == 'X') ? 'O' : 'X';
        }

        private static bool CheckWin(char[][] board, char player)
        {
            for (int i = 0; i < 3; i++)
            {
                if (board[i][0] == player && board[i][1] == player && board[i][2] == player)
                    return true;
            }
            for (int j = 0; j < 3; j++)
            {
                if (board[0][j] == player && board[1][j] == player && board[2][j] == player)
                    return true;
            }
            if (board[0][0] == player && board[1][1] == player && board[2][2] == player)
                return true;
            if (board[0][2] == player && board[1][1] == player && board[2][0] == player)
                return true;

            return false;
        }

        private static bool IsBoardFull(char[][] board)
        {
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (board[i][j] == ' ')
                        return false;
                }
            }
            return true;
        }
    }
}
