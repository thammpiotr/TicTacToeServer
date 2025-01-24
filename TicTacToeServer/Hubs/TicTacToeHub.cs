using Microsoft.AspNetCore.SignalR;
using TicTacToeServer.Models;

namespace TicTacToeServer.Hubs
{
    public class TicTacToeHub : Hub
    {
        private static Dictionary<string, string> _connectionToRoom = new Dictionary<string, string>();
        
        private static Dictionary<string, GameRoom> _rooms = new Dictionary<string, GameRoom>();
        
        public async Task<List<string>> GetRooms()
        {
            return _rooms.Keys.ToList();
        }
        
        public async Task<string?> CreateRoom(string playerName, string playerId)
        {
            var connectionId = Context.ConnectionId;
            Console.WriteLine($"CreateRoom called by {connectionId} for player: {playerName}");

            if (_connectionToRoom.ContainsKey(connectionId))
            {
                Console.WriteLine($"Connection {connectionId} is already in a room.");
                return null;
            }

            var roomId = Guid.NewGuid().ToString();
            var room = new GameRoom(roomId)
            {
                PlayerXName = playerName,
                PlayerXConnectionId = connectionId,
                PlayerXId = playerId,
                Board = new char[3][]
                {
                    new char[3] { ' ', ' ', ' ' },
                    new char[3] { ' ', ' ', ' ' },
                    new char[3] { ' ', ' ', ' ' }
                },
                CurrentTurn = 'X',
                IsGameOver = false
            };
            _rooms.Add(roomId, room);
            _connectionToRoom[connectionId] = roomId;

            Console.WriteLine($"Room created with ID: {roomId}, PlayerXConnectionId: {room.PlayerXConnectionId}");

            await Groups.AddToGroupAsync(connectionId, roomId);
            await Clients.All.SendAsync("RoomsUpdated", await GetRoomsWithPlayerCounts());

            return roomId;
        }

                
        public async Task<bool> JoinRoom(string roomId, string playerName, string playerId)
        {
            var connectionId = Context.ConnectionId;

            if (_rooms.TryGetValue(roomId, out var room))
            {
                if (room.PlayerXId == playerId || room.PlayerOId == playerId)
                {
                    room.IsPendingRemoval = false;
                    if (room.PlayerXId == playerId)
                    {
                        room.PlayerXConnectionId = connectionId;
                    }
                    else if (room.PlayerOId == playerId)
                    {
                        room.PlayerOConnectionId = connectionId;
                    }
                    
                    _connectionToRoom[connectionId] = roomId;
                    await Groups.AddToGroupAsync(connectionId, roomId);
                    await Clients.Caller.SendAsync("SyncGameState", room.Board, room.CurrentTurn, room.IsGameOver);
                    return true;
                }
                if (!room.IsGameOver && room.IsPendingRemoval)
                {
                    return false;
                }
                if (room.IsGameOver)
                {
                    room.Board = new char[3][]
                    {
                        new char[3] { ' ', ' ', ' ' },
                        new char[3] { ' ', ' ', ' ' },
                        new char[3] { ' ', ' ', ' ' }
                    };
                    room.CurrentTurn = 'X';
                    room.IsGameOver = false;
                    room.Winner = null;

                    await Clients.Group(roomId).SendAsync("GameRestarted", room.Board, room.CurrentTurn);
                }


                if (room.PlayerXConnectionId == null)
                {
                    room.PlayerXConnectionId = connectionId;
                    room.PlayerXId = playerId;
                    room.PlayerXName = playerName;
                }
                else if (room.PlayerOConnectionId == null)
                {
                    room.PlayerOConnectionId = connectionId;
                    room.PlayerOId = playerId;
                    room.PlayerOName = playerName;
                }
                else
                {
                    return false;
                }

                _connectionToRoom[connectionId] = roomId;
                await Groups.AddToGroupAsync(connectionId, roomId);
                
                await Clients.Caller.SendAsync("SyncGameState", room.Board, room.CurrentTurn, room.IsGameOver);
                await Clients.Group(roomId).SendAsync("BoardUpdated", room.Board, room.CurrentTurn, room.IsGameOver);

                return true;
            }

            return false;
        }       

        
        public async Task<bool> EnterRoomGroup(string roomId)
        {
            if (_rooms.ContainsKey(roomId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
                return true;
            }
            return false;
        }


        public async Task<bool> MakeMove(string roomId, int row, int col, string playerId)
        {
            if (_rooms.TryGetValue(roomId, out var room))
            {
                char playerSymbol;

                if (room.PlayerXId == playerId)
                {
                    playerSymbol = 'X';
                }
                else if (room.PlayerOId == playerId)
                {
                    playerSymbol = 'O';
                }
                else
                {
                    return false;
                }

                if (!TicTacToeGame.IsValidMove(room, row, col, playerSymbol))
                {
                    return false;
                }

                TicTacToeGame.MakeMove(room, row, col, playerSymbol);

                await Clients.Group(roomId).SendAsync("BoardUpdated", room.Board, room.CurrentTurn, room.IsGameOver);
                return true;
            }

            return false;
        }
        public async Task<object?> GetGameState(string roomId)
        {
            if (_rooms.TryGetValue(roomId, out var room))
            {
                return new
                {
                    Board = room.Board,
                    CurrentTurn = room.CurrentTurn,
                    IsGameOver = room.IsGameOver,
                    PlayerXName = room.PlayerXName,
                    PlayerOName = room.PlayerOName
                };
            }

            return null; 
        }

        public async Task<bool> RestartGame(string roomId)
        {
            if (_rooms.TryGetValue(roomId, out var room))
            {
                room.Board = new char[3][]
                {
                    new char[3] { ' ', ' ', ' ' },
                    new char[3] { ' ', ' ', ' ' },
                    new char[3] { ' ', ' ', ' ' }
                };
                
                room.CurrentTurn = 'X';
                room.IsGameOver = false;
                room.Winner = null;

                await Clients.Group(roomId).SendAsync("GameRestarted", room.Board, room.CurrentTurn);
                return true; 
            }
            return false;
        }
        
        public async Task<bool> LeaveRoom()
        {
            var connectionId = Context.ConnectionId;
            
            if (!_connectionToRoom.TryGetValue(connectionId, out var roomId))
            {
                return false; 
            }

            _connectionToRoom.Remove(connectionId);
            await Groups.RemoveFromGroupAsync(connectionId, roomId);

            if (_rooms.TryGetValue(roomId, out var room))
            {
                string? winner = null;
                
                if (room.PlayerXConnectionId == connectionId)
                {
                    room.PlayerXConnectionId = null;
                    room.PlayerXName = null;
                    if (room.PlayerOConnectionId != null)
                    {
                        winner = room.PlayerOName;
                    }
                }
                else if (room.PlayerOConnectionId == connectionId)
                {
                    room.PlayerOConnectionId = null;
                    room.PlayerOName = null;
                    if (room.PlayerXConnectionId != null)
                    {
                        winner = room.PlayerXName; 
                    }
                }

                if (winner != null)
                {
                    room.IsGameOver = true;
                    room.Winner = winner; 
                    await Clients.Group(roomId).SendAsync("GameOver", winner);
                }
                
                if (room.PlayerXConnectionId == null && room.PlayerOConnectionId == null)
                {
                    _rooms.Remove(roomId);
                    await Clients.All.SendAsync("RoomsUpdated", await GetRoomsWithPlayerCounts());
                }
                else
                {
                    await Clients.Group(roomId).SendAsync("PlayerLeft", connectionId);
                }
            }

            return true;
        }


        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var connectionId = Context.ConnectionId;

            if (_connectionToRoom.TryGetValue(connectionId, out var roomId))
            {
                if (_rooms.TryGetValue(roomId, out var room))
                {
                    room.IsPendingRemoval = true;
                    
                    if (room.PlayerXConnectionId == connectionId)
                    {
                        room.PlayerXConnectionId = null;
                    }
                    else if (room.PlayerOConnectionId == connectionId)
                    {
                        room.PlayerOConnectionId = null;
                    }
                    
                    await Task.Delay(5000);
                    
                    if (room.IsPendingRemoval)
                    {
                        if (string.IsNullOrEmpty(room.PlayerXConnectionId) && string.IsNullOrEmpty(room.PlayerOConnectionId))
                        {
                            _rooms.Remove(roomId);
                            _connectionToRoom.Remove(connectionId);
                            await Clients.All.SendAsync("RoomsUpdated", await GetRoomsWithPlayerCounts());
                        }
                        else
                        {
                            string? winner = null;
                            if (room.PlayerXConnectionId == null)
                            {
                                winner = room.PlayerOName;
                            }
                            else if (room.PlayerOConnectionId == null)
                            {
                                winner = room.PlayerXName;
                            }

                            if (winner != null)
                            {
                                room.IsGameOver = true;
                                room.Winner = winner;
                                await Clients.Group(roomId).SendAsync("GameOver", winner);
                            }
                        }
                    }
                    else
                    {
                        room.IsPendingRemoval = false;
                    }
                }
                _connectionToRoom.Remove(connectionId);
            }
    await base.OnDisconnectedAsync(exception);
}

        
        
        private string GetPlayerNameIfNeeded(string connectionId, string existingName)
        {
            return existingName;
        }
        public async Task<List<object>> GetRoomsWithPlayerCounts()
        {
            return _rooms
                .Select(room => new 
                {
                    roomId = room.Key,
                    playerCount = 
                        (room.Value.PlayerXConnectionId != null ? 1 : 0) +
                        (room.Value.PlayerOConnectionId != null ? 1 : 0)
                })
                .Cast<object>() 
                .ToList();
        }
        
    }
}
