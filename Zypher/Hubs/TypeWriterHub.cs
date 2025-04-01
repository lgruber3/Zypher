using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace Zypher
{
    public enum GameStateEnum
    {
        Waiting,
        CountingDown,
        Racing,
        Finished
    }

    public class PlayerState
    {
        public required string ConnectionId { get; set; }
        public required string Name { get; set; }
        public int Position { get; set; }
    }

    public class GameSession : IDisposable
    {
        public required string GameCode { get; set; }
        public string? SampleText { get; set; } 
        public ConcurrentDictionary<string, PlayerState> Players { get; }
        public DateTime CreationTime { get; }
        public string? HostConnectionId { get; set; } 
        public GameStateEnum CurrentState { get; set; }

        private System.Timers.Timer? _countdownTimer;
        private int _countdownValue = 3;
        private IHubContext<TypingHub> _hubContext;


        public GameSession(string code, IHubContext<TypingHub> hubContext)
        {
            GameCode = code;
            SampleText = null; // Initial state
            Players = new ConcurrentDictionary<string, PlayerState>();
            CreationTime = DateTime.UtcNow;
            HostConnectionId = null; // Initial state
            CurrentState = GameStateEnum.Waiting; // Treat pending as Waiting initially
            _hubContext = hubContext;
        }

        public bool IsReadyToStart() => !string.IsNullOrEmpty(SampleText) && HostConnectionId != null;

        public void StartCountdownSequence()
        {
            if (CurrentState != GameStateEnum.Waiting || !IsReadyToStart()) return;

            CurrentState = GameStateEnum.CountingDown;
            _countdownValue = 3;

            Console.WriteLine($"Game {GameCode}: Countdown initiated by host.");
            _hubContext.Clients.Group(GameCode).SendAsync("ReceiveCountdownState", _countdownValue);

            _countdownTimer?.Dispose();
            _countdownTimer = new System.Timers.Timer(1000);
            _countdownTimer.Elapsed += OnCountdownTick;
            _countdownTimer.AutoReset = true;
            _countdownTimer.Enabled = true;
        }

        private async void OnCountdownTick(object? sender, ElapsedEventArgs e)
        {
            _countdownValue--;

            if (_countdownValue > 0)
            {
                await _hubContext.Clients.Group(GameCode).SendAsync("ReceiveCountdownState", _countdownValue);
            }
            else
            {
                _countdownTimer?.Stop();
                _countdownTimer?.Dispose();
                _countdownTimer = null;

                CurrentState = GameStateEnum.Racing;
                Console.WriteLine($"Game {GameCode}: Countdown finished. Game starting!");
                await _hubContext.Clients.Group(GameCode).SendAsync("GameStarted");
            }
        }

        public void Dispose()
        {
            _countdownTimer?.Stop();
            _countdownTimer?.Dispose();
            _countdownTimer = null;
            GC.SuppressFinalize(this);
        }
    }

    public class HubPlayerState
    {
        public string ConnectionId { get; set; } = "";
        public string Name { get; set; } = "";
        public int Position { get; set; }
    }

    public class HubGameStateDto
    {
        public string GameCode { get; set; } = "";
        public string SampleText { get; set; } = "";
        public List<HubPlayerState> Players { get; set; } = new();
        public string? HostConnectionId { get; set; }
        public GameStateEnum CurrentState { get; set; }
    }


    public class TypingHub : Hub
    {
        private static readonly ConcurrentDictionary<string, GameSession> _activeGames = new ConcurrentDictionary<string, GameSession>(StringComparer.OrdinalIgnoreCase);
        private static readonly Random _random = new Random();
        private readonly IHubContext<TypingHub> _hubContext;
        private readonly HttpClient Http;
        private readonly IConfiguration Configuration;

        public TypingHub(IHubContext<TypingHub> hubContext, HttpClient http, IConfiguration configuration)
        {
            _hubContext = hubContext;
            Http = http;
            Configuration = configuration;
        }

        public async Task<string?> CreateGame() 
        {
            var connectionId = Context.ConnectionId;
            Console.WriteLine($"Game creation requested by {connectionId}");
            string gameCode;

            const string chars = "ABCDEFGHIJKLMNPQRSTUVWXYZ123456789";
            const int codeLength = 6;
            int maxAttempts = 10;
            int attempts = 0;
            do
            {
                if (attempts++ > maxAttempts)
                {
                    Console.WriteLine($"Error: Could not generate unique game code after {maxAttempts} attempts.");
                    await Clients.Caller.SendAsync("GameCreationError", "Failed to generate a unique game code.");
                    return null; 
                }
                gameCode = new string(Enumerable.Repeat(chars, codeLength)
                    .Select(s => s[_random.Next(s.Length)]).ToArray());
            } while (_activeGames.ContainsKey(gameCode));


            var gameSession = new GameSession(gameCode, _hubContext)
            {
                HostConnectionId = connectionId,
                CurrentState = GameStateEnum.Waiting,
                GameCode = gameCode
            };

            if (_activeGames.TryAdd(gameCode, gameSession))
            {
                Console.WriteLine($"Pending game created: {gameCode} by {connectionId}. Waiting for first player.");
                return gameCode; 
            }
            else
            {
                Console.WriteLine($"Error: Failed to add pending game {gameCode} to dictionary.");
                await Clients.Caller.SendAsync("GameCreationError", "Failed to register the new game session.");
                return null; 
            }
        }

        public async Task JoinGame(string gameCode, string playerName)
        {
            var connectionId = Context.ConnectionId;

            if (string.IsNullOrWhiteSpace(gameCode))
            {
                await Clients.Caller.SendAsync("JoinError", "Game code cannot be empty.");
                return;
            }

            gameCode = gameCode.ToUpperInvariant();

            if (_activeGames.TryGetValue(gameCode, out GameSession? game))
            {
                if (game.CurrentState != GameStateEnum.Waiting && game.CurrentState != GameStateEnum.CountingDown) 
                {
                    await Clients.Caller.SendAsync("JoinError", $"Game '{gameCode}' has already started or finished.");
                    return;
                }

                bool isFirstPlayer = false;
                if (!game.IsReadyToStart()) 
                {
                     if (!game.IsReadyToStart()) 
                     {
                         Console.WriteLine($"Game {gameCode}: First player {connectionId} joining. Initializing game...");
                         string? initialText = await FetchTextFromServerLogicInternal(); 

                         if (string.IsNullOrEmpty(initialText))
                         {
                             Console.WriteLine($"Error: Failed to fetch sample text for game {gameCode} during join.");
                             await Clients.Caller.SendAsync("JoinError", "Failed to load sample text for the game.");
                             return;
                         }
                         game.SampleText = initialText;
                         game.HostConnectionId = connectionId;
                         game.CurrentState = GameStateEnum.Waiting; 
                         isFirstPlayer = true;
                          Console.WriteLine($"Game {gameCode}: Initialized. Host: {connectionId}, Text set.");
                     }
                }
                
                if (string.IsNullOrEmpty(game.SampleText)) 
                { 
                     await Clients.Caller.SendAsync("JoinError", "Game data is missing. Cannot join.");
                     return;
                }

                await JoinGameInternal(gameCode, playerName, connectionId, game, isFirstPlayer);
            }
            else
            {
                Console.WriteLine($"Join Failed: Player {connectionId} ({playerName}) tried to join non-existent game {gameCode}.");
                await Clients.Caller.SendAsync("JoinError", $"Game code '{gameCode}' not found or has expired.");
            }
        }


        public async Task RequestStartGame()
        {
            var connectionId = Context.ConnectionId;
            if (Context.Items.TryGetValue("GameCode", out object? gameCodeObj) && gameCodeObj is string gameCode)
            {
                if (_activeGames.TryGetValue(gameCode, out GameSession? game))
                {
                    if (!game.IsReadyToStart())
                    {
                        await Clients.Caller.SendAsync("StartGameError", "Game is not ready yet.");
                        return;
                    }

                    if (game.HostConnectionId == connectionId && game.CurrentState == GameStateEnum.Waiting)
                    {
                        if (game.Players.Count < 1) 
                        {
                            await Clients.Caller.SendAsync("StartGameError", "Need at least 1 player to start."); 
                            return;
                        }
                        Console.WriteLine($"Game {gameCode}: Start request received from host {connectionId}.");
                        game.StartCountdownSequence();
                    }
                    else if (game.HostConnectionId != connectionId)
                    {
                        Console.WriteLine($"Game {gameCode}: Start request denied. Caller {connectionId} is not the host ({game.HostConnectionId}).");
                        await Clients.Caller.SendAsync("StartGameError", "Only the host can start the game.");
                    }
                    else
                    {
                        Console.WriteLine($"Game {gameCode}: Start request denied. Game state is {game.CurrentState}.");
                        await Clients.Caller.SendAsync("StartGameError", $"Cannot start game, current state is: {game.CurrentState}.");
                    }
                }
            } else {
                 Console.WriteLine($"Warning: Player {connectionId} requested start game but has no game code associated.");
            }
        }


        public async Task UpdateProgress(int position)
        {
            var connectionId = Context.ConnectionId;

            if (Context.Items.TryGetValue("GameCode", out object? gameCodeObj) && gameCodeObj is string gameCode)
            {
                if (_activeGames.TryGetValue(gameCode, out GameSession? game))
                {
                    if (game.CurrentState != GameStateEnum.Racing || !game.IsReadyToStart()) return;

                    if (game.Players.TryGetValue(connectionId, out PlayerState? player))
                    {
                        position = Math.Clamp(position, 0, game.SampleText!.Length); 
                        player.Position = position;
                        await Clients.Group(gameCode).SendAsync("PlayerProgressUpdated", connectionId, position);
                    }
                    else
                    {
                        Console.WriteLine($"Warning: Player {connectionId} sent progress for game {gameCode} but is not listed in players dictionary.");
                    }
                }
            }
        }

        private async Task JoinGameInternal(string gameCode, string playerName, string connectionId, GameSession game, bool wasFirstPlayer)
        {
             if (!game.IsReadyToStart() || string.IsNullOrEmpty(game.SampleText)) {
                 Console.WriteLine($"Error: JoinGameInternal called for game {gameCode} but it's not ready.");
                 await Clients.Caller.SendAsync("JoinError", "Game initialization failed.");
                 return;
             }

            string validatedPlayerName = string.IsNullOrWhiteSpace(playerName) ? $"Player_{connectionId.Substring(0, 4)}" : playerName.Trim();

            var player = new PlayerState
            {
                ConnectionId = connectionId,
                Name = validatedPlayerName,
                Position = 0,
            };

            bool added = game.Players.TryAdd(connectionId, player);

            if (!added && game.Players.ContainsKey(connectionId))
            {
                 Console.WriteLine($"Player {player.Name} ({connectionId}) re-joining game {gameCode}.");
            }
            else if (!added)
            {
                Console.WriteLine($"Error: Failed to add player {player.Name} ({connectionId}) to game {gameCode} dictionary.");
                await Clients.Caller.SendAsync("JoinError", "A server error occurred adding player.");
                return;
            }

             if (added) {
                Console.WriteLine($"Player {player.Name} ({connectionId}) added to game {gameCode}. Players: {game.Players.Count}");
             }

            await Groups.AddToGroupAsync(connectionId, gameCode);
            Context.Items["GameCode"] = gameCode;

            var gameStateDto = new HubGameStateDto
            {
                GameCode = game.GameCode,
                SampleText = game.SampleText, 
                Players = game.Players.Values.Select(p => new HubPlayerState { ConnectionId = p.ConnectionId, Name = p.Name, Position = p.Position }).ToList(),
                HostConnectionId = game.HostConnectionId,
                CurrentState = game.CurrentState
            };

            await Clients.Caller.SendAsync("ReceiveFullGameState", gameStateDto);

            if (added && !wasFirstPlayer) 
            {
                var newPlayerState = new HubPlayerState { ConnectionId = player.ConnectionId, Name = player.Name, Position = player.Position };
                await Clients.OthersInGroup(gameCode).SendAsync("PlayerJoined", newPlayerState);
            } else if (wasFirstPlayer && game.Players.Count > 1) {
                var firstPlayerState = new HubPlayerState { ConnectionId = player.ConnectionId, Name = player.Name, Position = player.Position };
                await Clients.OthersInGroup(gameCode).SendAsync("PlayerJoined", firstPlayerState);
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var connectionId = Context.ConnectionId;

            if (Context.Items.TryGetValue("GameCode", out object? gameCodeObj) && gameCodeObj is string gameCode)
            {
                if (_activeGames.TryGetValue(gameCode, out GameSession? game))
                {
                    bool wasHost = (game.HostConnectionId == connectionId);

                    if (game.Players.TryRemove(connectionId, out PlayerState? player))
                    {
                        Console.WriteLine($"Player {player.Name} ({connectionId}) disconnected from game {gameCode}. Players Left: {game.Players.Count}");
                        await Clients.Group(gameCode).SendAsync("PlayerLeft", connectionId);

                        if (wasHost && !game.Players.IsEmpty)
                        {
                            var newHost = game.Players.Values.OrderBy(p => p.ConnectionId).FirstOrDefault();
                            if (newHost != null)
                            {
                                game.HostConnectionId = newHost.ConnectionId;
                                Console.WriteLine($"Game {gameCode}: Host disconnected. New host assigned: {newHost.Name} ({newHost.ConnectionId})");
                                await Clients.Group(gameCode).SendAsync("NewHostAssigned", game.HostConnectionId);
                            }
                        }

                        if (game.Players.IsEmpty)
                        {
                            Console.WriteLine($"Game {gameCode} is now empty. Removing session.");
                            game.Dispose();
                            if (_activeGames.TryRemove(gameCode, out _)) {
                                Console.WriteLine($"Game {gameCode} successfully removed.");
                            } else {
                                Console.WriteLine($"Warning: Failed to remove empty game {gameCode}.");
                            }
                        }
                    }
                    else {
                         Console.WriteLine($"Warning: Disconnected player {connectionId} not found in game {gameCode}.");
                    }
                }
                else {
                     Console.WriteLine($"Warning: Disconnected player {connectionId} had game code {gameCode}, but game not found.");
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

        private async Task<string> FetchTextFromServerLogicInternal()
        {
            string text = string.Empty;
            string apiUrl = Configuration["TextApis:RandomBookQuote"];

            try
            {
                if (!string.IsNullOrEmpty(apiUrl))
                {
                    var fetchedText = await Http.GetStringAsync(apiUrl);

                        using var jsonDoc = System.Text.Json.JsonDocument.Parse(fetchedText);
                        text = jsonDoc.RootElement.TryGetProperty("quote", out var quoteElement)
                            ? quoteElement.GetString() ?? "Error loading quote."
                            : "Error: Quote format invalid.";
              
                }
                else {
                    text = "API URL not configured for selected mode.";
                }

                text = text.Replace("\n", " ").Replace("\r", " ").Replace("  ", " ").Trim();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading sample text from {apiUrl}: {ex}");
                text = $"Error loading text. Check console. {ex.Message}"; 
            }
            
            return text;
        }
    }
}