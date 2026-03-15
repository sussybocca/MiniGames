using Microsoft.JSInterop;
using System.Threading.Tasks;

namespace Sandblox.Services;

public class WorldPersistence
{
    private readonly IJSRuntime _jsRuntime;
    private IJSObjectReference? _module;

    public WorldPersistence(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    private async Task EnsureModuleAsync()
    {
        if (_module == null)
            _module = await _jsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/storage.js");
    }

    public async Task SaveWorldAsync(byte[] data)
    {
        await EnsureModuleAsync();
        await _module!.InvokeVoidAsync("saveWorld", data);
    }

    public async Task<byte[]?> LoadWorldAsync()
    {
        await EnsureModuleAsync();
        return await _module!.InvokeAsync<byte[]?>("loadWorld");
    }
}