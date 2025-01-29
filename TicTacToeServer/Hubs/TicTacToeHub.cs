using Microsoft.AspNetCore.SignalR;
using TicTacToeServer.Models;

namespace TicTacToeServer.Hubs
{
    public class TicTacToeHub : Hub
    {
        private static Dictionary<string, string> _connectionToRoom = new Dictionary<string, string>();
        
        private static Dictionary<string, GameRoom> _rooms = new Dictionary<string, GameRoom>();
        
        public Task<List<string>> GetRooms()
        {
            return Task.FromResult(_rooms.Keys.ToList());
        }
        
        public async Task<string?> CreateRoom(string playerName, string playerId)
        {
            var connectionId = Context.ConnectionId;

            if (_connectionToRoom.ContainsKey(connectionId))
            {
                return null;
            }

            var roomId = Guid.NewGuid().ToString();
            var room = new GameRoom(roomId)
            {
                PlayerXName = playerName,
                PlayerXConnectionId = connectionId,
                PlayerXId = playerId,
                Board = new []
                {
                    new [] { ' ', ' ', ' ' },
                    new [] { ' ', ' ', ' ' },
                    new [] { ' ', ' ', ' ' }
                },
                CurrentTurn = 'X',
                IsGameOver = false
            };
            _rooms.Add(roomId, room);
            _connectionToRoom[connectionId] = roomId;



            await Groups.AddToGroupAsync(connectionId, roomId);
            await Clients.All.SendAsync("RoomsUpdated", await GetRoomsWithPlayerCounts());

            return roomId;
        }

                
public async Task<bool> JoinRoom(string roomId, string playerName, string playerId)
{
    var connectionId = Context.ConnectionId;

    if (!_rooms.TryGetValue(roomId, out var room))
    {

        return false;
    }
    
    if (room.PlayerXConnectionId != null && room.PlayerOConnectionId != null &&
        room.PlayerXId != playerId && room.PlayerOId != playerId)
    {
        return false;
    }


    bool isPlayerX = (room.PlayerXId == playerId);
    bool isPlayerO = (room.PlayerOId == playerId);
    bool isGameOver = room.IsGameOver;


    if (isPlayerX || isPlayerO)
    {
        room.IsPendingRemoval = false;


        if (isPlayerX)
        {
            room.PlayerXPendingRemoval = false;
            room.PlayerXConnectionId = connectionId;
            room.PlayerXName = playerName;
        }
        else
        {
            room.PlayerOPendingRemoval = false;
            room.PlayerOConnectionId = connectionId;
            room.PlayerOName = playerName;
        }

        _connectionToRoom[connectionId] = roomId;
        await Groups.AddToGroupAsync(connectionId, roomId);


        if (isGameOver)
        {
            ResetGameState(room);
            await Clients.Group(roomId).SendAsync("GameRestarted", room.Board, room.CurrentTurn);

            await Clients.Caller.SendAsync(
                "SyncGameState",
                room.Board,
                room.CurrentTurn,
                room.IsGameOver,
                room.Winner ?? string.Empty
            );
            return true; 
        }


        await Clients.Caller.SendAsync(
            "SyncGameState",
            room.Board,
            room.CurrentTurn,
            room.IsGameOver,
            room.Winner ?? string.Empty
        );
        return true;
    }


    if (!isGameOver && room.IsPendingRemoval)
    {
        return false;
    }


    if (isGameOver)
    {
        ResetGameState(room);
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


    await Clients.Caller.SendAsync(
        "SyncGameState",
        room.Board,
        room.CurrentTurn,
        room.IsGameOver,
        room.Winner ?? string.Empty
    );
    await Clients.All.SendAsync("RoomsUpdated", await GetRoomsWithPlayerCounts());

    return true;
}




private void ResetGameState(GameRoom room)
{

    room.Board = new []
    {
        new [] { ' ', ' ', ' ' },
        new [] { ' ', ' ', ' ' },
        new [] { ' ', ' ', ' ' }
    };
    room.CurrentTurn = 'X';
    room.IsGameOver = false;
    room.Winner = null;
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
                await Clients.Group(roomId).SendAsync("BoardUpdated", room.Board, room.CurrentTurn);
                if (room.IsGameOver)
                {
                    if (!string.IsNullOrEmpty(room.Winner))
                    {
                        await Clients.Group(roomId).SendAsync("GameOver", room.Winner);
                    }
                    else
                    {
                        await Clients.Group(roomId).SendAsync("GameOver", "Draw");
                    }
                }

                return true;
            }

            return false;
        }
        public Task<object?> GetGameState(string roomId)
        {
            if (_rooms.TryGetValue(roomId, out var room))
            {
                return Task.FromResult<object?>(new
                {
                    room.Board,
                    room.CurrentTurn,
                    room.IsGameOver,
                    room.Winner,
                    room.PlayerXName,
                    room.PlayerOName
                });
            }

            return Task.FromResult<object?>(null);
        }


        public async Task<bool> RestartGame(string roomId)
        {
            if (_rooms.TryGetValue(roomId, out var room))
            {
                room.Board = new []
                {
                    new [] { ' ', ' ', ' ' },
                    new [] { ' ', ' ', ' ' },
                    new [] { ' ', ' ', ' ' }
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

            if (!_rooms.TryGetValue(roomId, out var room))
            {
                return true;
            }
            
            if (room.IsGameOver)
            {
                if (room.PlayerXConnectionId == connectionId)
                {
                    room.PlayerXConnectionId = null;
                    room.PlayerXName = null;
                }
                else if (room.PlayerOConnectionId == connectionId)
                {
                    room.PlayerOConnectionId = null;
                    room.PlayerOName = null;
                }
                
                if (room.PlayerXConnectionId == null && room.PlayerOConnectionId == null)
                {
                    _rooms.Remove(roomId);
                    await Clients.All.SendAsync("RoomsUpdated", await GetRoomsWithPlayerCounts());
                }
                else
                {
                    await Clients.Group(roomId).SendAsync("PlayerLeft", connectionId);
                    await Clients.All.SendAsync("RoomsUpdated", await GetRoomsWithPlayerCounts());
                }
                return true;
            }
            
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
                await Clients.All.SendAsync("RoomsUpdated", await GetRoomsWithPlayerCounts());
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
                    if (room.PlayerXConnectionId == connectionId)
                    {
                        room.PlayerXPendingRemoval = true;
                    }
                    else if (room.PlayerOConnectionId == connectionId)
                    {
                        room.PlayerOPendingRemoval = true;
                    }

                    room.IsPendingRemoval = true;

                    await Task.Delay(5000);
                    
                    if (room.IsPendingRemoval)
                    {
                        if (room.PlayerXPendingRemoval)
                        {
                            room.PlayerXConnectionId = null;
                            room.PlayerXName = null;
                            room.PlayerXId = null;
                            room.PlayerXPendingRemoval = false; 
                        }
                        if (room.PlayerOPendingRemoval)
                        {
                            room.PlayerOConnectionId = null;
                            room.PlayerOName = null;
                            room.PlayerOId = null;
                            room.PlayerOPendingRemoval = false;
                        }
                        
                        if (!room.IsGameOver)
                        {
                            string? winner = null;
                            if (room.PlayerXConnectionId == null && room.PlayerOConnectionId != null)
                            {
                                winner = room.PlayerOName;
                            }
                            else if (room.PlayerOConnectionId == null && room.PlayerXConnectionId != null)
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
                        
                        if (room.PlayerXConnectionId == null && room.PlayerOConnectionId == null)
                        {
                            _rooms.Remove(roomId);
                            _connectionToRoom.Remove(connectionId);
                            await Clients.All.SendAsync("RoomsUpdated", await GetRoomsWithPlayerCounts());
                        }
                        else
                        {
                            await Clients.All.SendAsync("RoomsUpdated", await GetRoomsWithPlayerCounts());
                        }
                    }
                }

                _connectionToRoom.Remove(connectionId);
            }

            await base.OnDisconnectedAsync(exception);
        }

        public Task<List<object>> GetRoomsWithPlayerCounts()
        {
            return Task.FromResult(_rooms
                .Select(room => new 
                {
                    roomId = room.Key,
                    playerCount = 
                        (room.Value.PlayerXConnectionId != null ? 1 : 0) +
                        (room.Value.PlayerOConnectionId != null ? 1 : 0)
                })
                .Cast<object>() 
                .ToList());
        }

        
    }
}
