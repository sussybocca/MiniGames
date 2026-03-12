using MiniGames.Models;  // For GameInfo
using MiniGames.Services; // For GameService

namespace MiniGames.Emulators.TV;

public class TVService
{
    private readonly GameService _gameService;
    private List<GameInfo> _channels = new();
    private int _currentChannelIndex = 0;
    private bool _isOn = true;
    private int _volume = 50;
    private bool _isMuted = false;

    public event Action? OnStateChanged;

    public TVService(GameService gameService)
    {
        _gameService = gameService;
    }

    public async Task InitializeAsync()
    {
        _channels = await _gameService.GetGamesAsync();
        _currentChannelIndex = _channels.Count > 0 ? 0 : -1;
    }

    public IReadOnlyList<GameInfo> Channels => _channels.AsReadOnly();
    public GameInfo? CurrentChannel => _channels.Count > 0 ? _channels[_currentChannelIndex] : null;
    public bool IsOn => _isOn;
    public int Volume => _isMuted ? 0 : _volume;
    public int RawVolume => _volume;
    public bool IsMuted => _isMuted;
    public int ChannelCount => _channels.Count;

    public void TogglePower()
    {
        _isOn = !_isOn;
        NotifyStateChanged();
    }

    public void NextChannel()
    {
        if (!_isOn || _channels.Count == 0) return;
        _currentChannelIndex = (_currentChannelIndex + 1) % _channels.Count;
        NotifyStateChanged();
    }

    public void PrevChannel()
    {
        if (!_isOn || _channels.Count == 0) return;
        _currentChannelIndex = (_currentChannelIndex - 1 + _channels.Count) % _channels.Count;
        NotifyStateChanged();
    }

    public void SetChannel(int index)
    {
        if (!_isOn || index < 0 || index >= _channels.Count) return;
        _currentChannelIndex = index;
        NotifyStateChanged();
    }

    public void VolumeUp()
    {
        if (!_isOn) return;
        _volume = Math.Min(100, _volume + 5);
        if (_isMuted) _isMuted = false;
        NotifyStateChanged();
    }

    public void VolumeDown()
    {
        if (!_isOn) return;
        _volume = Math.Max(0, _volume - 5);
        if (_isMuted) _isMuted = false;
        NotifyStateChanged();
    }

    public void ToggleMute()
    {
        if (!_isOn) return;
        _isMuted = !_isMuted;
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnStateChanged?.Invoke();
}