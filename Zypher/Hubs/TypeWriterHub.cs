using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Zypher;

public class TypingHub : Hub
{
    private static ConcurrentDictionary<string, int> _playerProgress = new();

    public async Task UpdateProgress(string playerId, int progress)
    {
        _playerProgress[playerId] = progress;
        await Clients.All.SendAsync("ProgressUpdated", _playerProgress);
    }
}