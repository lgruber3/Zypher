using Microsoft.JSInterop;

namespace Zypher.Services;

public class UserService
{
    private readonly IJSRuntime _jsRuntime;
    public event Action OnUserUpdated;

    public UserService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<string> GetUsernameAsync()
    {
        var username = await _jsRuntime.InvokeAsync<string>("sessionStorage.getItem", "username");
        return username ?? "Anonymous";
    }

    public async Task SetUsernameAsync(string username)
    {
        await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", "username", username);
        OnUserUpdated?.Invoke();
    }
}
