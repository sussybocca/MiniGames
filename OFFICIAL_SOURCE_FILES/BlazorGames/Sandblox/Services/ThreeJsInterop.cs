using Microsoft.JSInterop;

namespace Sandblox.Services;

public class ThreeJsInterop : IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private IJSObjectReference? _module;
    private DotNetObjectReference<Sandblox>? _dotNetRef;

    public ThreeJsInterop(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task InitializeAsync(ElementReference canvas, DotNetObjectReference<Sandblox> dotNetRef)
    {
        _dotNetRef = dotNetRef;
        _module = await _jsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/sandblox.js");
        await _module.InvokeVoidAsync("initialize", canvas, dotNetRef);
    }

    public async Task StartGameLoop() => await _module!.InvokeVoidAsync("startGameLoop");

    public async Task HandleKeyDownAsync(string key, bool shiftKey, bool ctrlKey) =>
        await _module!.InvokeVoidAsync("handleKeyDown", key, shiftKey, ctrlKey);

    public async Task HandleKeyUpAsync(string key) =>
        await _module!.InvokeVoidAsync("handleKeyUp", key);

    public async Task HandleMouseMoveAsync(int deltaX, int deltaY) =>
        await _module!.InvokeVoidAsync("handleMouseMove", deltaX, deltaY);

    public async Task HandleClickAsync(long button) =>
        await _module!.InvokeVoidAsync("handleClick", (int)button);

    public async Task SetSelectedSlot(int slot) =>
        await _module!.InvokeVoidAsync("setSelectedSlot", slot);

    public async Task ToggleDebug() =>
        await _module!.InvokeVoidAsync("toggleDebug");

    public async Task UpdateChunk(string chunkKey, object[] blocks) =>
        await _module!.InvokeVoidAsync("updateChunk", chunkKey, blocks);

    public async Task RemoveChunk(string chunkKey) =>
        await _module!.InvokeVoidAsync("removeChunk", chunkKey);

    public async ValueTask DisposeAsync()
    {
        if (_module != null)
            await _module.DisposeAsync();
        _dotNetRef?.Dispose();
    }
}