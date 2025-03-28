using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Zypher;

public class TypingHub : Hub
{
    private static ConcurrentDictionary<string, int> _sessions = new();

    public async Task SendKeystroke(string user, string key, int position)
    {
        await Clients.Others.SendAsync("ReceiveKeystroke", user, key, position);
    }
    
   public async Task Join(string user)
   {
       _sessions.AddOrUpdate(user, 0, (key, value) => value);
       await Clients.All.SendAsync("UserJoined", user);
   }
   
   public async Task Leave(string user)
   {
       _sessions.TryRemove(user, out _);
       await Clients.All.SendAsync("UserLeft", user);
   }
}