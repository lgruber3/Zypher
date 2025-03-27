using Microsoft.JSInterop;

namespace Zypher.Services;

public class UserService
{
    private readonly IJSRuntime _jsRuntime;

    public UserService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<string> GetUsernameAsync()
    {
        return await _jsRuntime.InvokeAsync<string>("sessionStorage.getItem", "username");
    }

    public async Task SetUsernameAsync(string username)
    {
        await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", "username", username);
    }
}
