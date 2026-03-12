using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
namespace MiniGames.Emulators.GameBoy3;

/// <summary>
/// Complete GameBoy Pixel Processing Unit (PPU) implementation.
/// Handles background, window, and sprite rendering according to hardware specs.
/// </summary>
public class PPU
{
    private readonly MMU _mmu;
    private readonly Interrupts _int;
    private readonly IJSRuntime _js;
    private readonly ElementReference _canvas;

    // Screen dimensions
    public const int Width = 160;
    public const int Height = 144;

    // VRAM and OAM references (from MMU)
    private readonly byte[] _vram;
    private readonly byte[] _oam;

    // PPU state
    private int _mode; // 0=HBlank, 1=VBlank, 2=OAM scan, 3=Drawing
    private int _clock;
    private int _line;
    private byte _lyCompare;

    // Registers
    private byte _lcdc;   // 0xFF40
    private byte _stat;   // 0xFF41
    private byte _scy;    // 0xFF42
    private byte _scx;    // 0xFF43
    private byte _ly;     // 0xFF44 (read‑only)
    private byte _lyc;    // 0xFF45
    private byte _bgp;    // 0xFF47
    private byte _obp0;   // 0xFF48
    private byte _obp1;   // 0xFF49
    private byte _wy;     // 0xFF4A
    private byte _wx;     // 0xFF4B

    // Palette data
    private byte[] _bgPalette = new byte[4];
    private byte[] _objPalette0 = new byte[4];
    private byte[] _objPalette1 = new byte[4];

    // Framebuffer (160x144 RGBA)
    private byte[] _framebuffer = new byte[Width * Height * 4];

    // Temporary line buffer for background+window
    private byte[] _bgLine = new byte[Width];
    // Sprite attributes per line (max 10)
    private struct SpriteInfo
    {
        public byte Y;
        public byte X;
        public byte Tile;
        public byte Flags;
        public int Index;
    }
    private SpriteInfo[] _sprites = new SpriteInfo[10];
    private int _spriteCount;

    public PPU(MMU mmu, Interrupts intHandler, IJSRuntime js, ElementReference canvas)
    {
        _mmu = mmu;
        _int = intHandler;
        _js = js;
        _canvas = canvas;
        _vram = mmu.GetVRAM();
        _oam = mmu.GetOAM();
        Reset();
    }

    private void Reset()
    {
        _lcdc = 0x91; // typical initial value (LCD on, BG on)
        _stat = 0x85;
        _scy = 0;
        _scx = 0;
        _ly = 0;
        _lyc = 0;
        _bgp = 0xFC;
        _obp0 = 0xFF;
        _obp1 = 0xFF;
        _wy = 0;
        _wx = 0;
        _mode = 2; // start with OAM scan
        _clock = 0;
        _line = 0;
        UpdatePalettes();
    }

    private void UpdatePalettes()
    {
        _bgPalette[0] = (byte)((_bgp >> 0) & 3);
        _bgPalette[1] = (byte)((_bgp >> 2) & 3);
        _bgPalette[2] = (byte)((_bgp >> 4) & 3);
        _bgPalette[3] = (byte)((_bgp >> 6) & 3);

        _objPalette0[0] = 0; // color 0 is transparent
        _objPalette0[1] = (byte)((_obp0 >> 2) & 3);
        _objPalette0[2] = (byte)((_obp0 >> 4) & 3);
        _objPalette0[3] = (byte)((_obp0 >> 6) & 3);

        _objPalette1[0] = 0;
        _objPalette1[1] = (byte)((_obp1 >> 2) & 3);
        _objPalette1[2] = (byte)((_obp1 >> 4) & 3);
        _objPalette1[3] = (byte)((_obp1 >> 6) & 3);
    }

    public void Update(int cycles)
    {
        if ((_lcdc & 0x80) == 0) // LCD off
        {
            // Reset PPU state
            _mode = 0;
            _clock = 0;
            _line = 0;
            _ly = 0;
            return;
        }

        _clock += cycles;
        switch (_mode)
        {
            case 2: // OAM scan (80 cycles)
                if (_clock >= 80)
                {
                    _clock -= 80;
                    _mode = 3;
                    // Find sprites for this line
                    FindSpritesForLine(_line);
                }
                break;
            case 3: // Drawing (172 cycles)
                if (_clock >= 172)
                {
                    _clock -= 172;
                    _mode = 0;
                    // Render the line
                    RenderLine(_line);
                }
                break;
            case 0: // HBlank (204 cycles)
                if (_clock >= 204)
                {
                    _clock -= 204;
                    _line++;
                    _ly = (byte)_line;
                    CompareLY();

                    if (_line == Height)
                    {
                        // Enter VBlank
                        _mode = 1;
                        Console.WriteLine($"VBlank triggered at line {_line}");
                        _int.RequestInterrupt(0); // VBlank interrupt
                        if ((_stat & 0x10) != 0) // STAT interrupt on mode 1
                            _int.RequestInterrupt(1);
                    }
                    else
                    {
                        _mode = 2; // next line OAM scan
                        if ((_stat & 0x20) != 0) // STAT interrupt on mode 2
                            _int.RequestInterrupt(1);
                    }
                }
                break;
            case 1: // VBlank (4560 cycles total for 10 lines)
                if (_clock >= 456)
                {
                    _clock -= 456;
                    _line++;
                    _ly = (byte)_line;
                    CompareLY();

                    if (_line > 153) // after last VBlank line
                    {
                        _line = 0;
                        _ly = 0;
                        _mode = 2;
                        CompareLY();
                        if ((_stat & 0x20) != 0)
                            _int.RequestInterrupt(1);
                    }
                }
                break;
        }
    }

    private void FindSpritesForLine(int line)
    {
        _spriteCount = 0;
        if ((_lcdc & 0x02) == 0) return; // sprites disabled

        int spriteHeight = (_lcdc & 0x04) != 0 ? 16 : 8;
        for (int i = 0; i < 40 && _spriteCount < 10; i++)
        {
            int baseAddr = i * 4;
            byte y = _oam[baseAddr];
            byte x = _oam[baseAddr + 1];
            byte tile = _oam[baseAddr + 2];
            byte flags = _oam[baseAddr + 3];

            int yPos = y - 16;
            int xPos = x - 8;

            if (line >= yPos && line < yPos + spriteHeight)
            {
                _sprites[_spriteCount].Y = y;
                _sprites[_spriteCount].X = x;
                _sprites[_spriteCount].Tile = tile;
                _sprites[_spriteCount].Flags = flags;
                _sprites[_spriteCount].Index = i;
                _spriteCount++;
            }
        }

        // Sort by X (lower X first) for priority
        for (int i = 0; i < _spriteCount - 1; i++)
        {
            for (int j = i + 1; j < _spriteCount; j++)
            {
                if (_sprites[j].X < _sprites[i].X)
                {
                    var temp = _sprites[i];
                    _sprites[i] = _sprites[j];
                    _sprites[j] = temp;
                }
            }
        }
    }

    private void RenderLine(int line)
    {
        if ((_lcdc & 0x80) == 0)
        {
            // LCD off – fill line with white
            int offset = line * Width * 4;
            for (int x = 0; x < Width; x++)
            {
                _framebuffer[offset + x * 4 + 0] = 0xFF;
                _framebuffer[offset + x * 4 + 1] = 0xFF;
                _framebuffer[offset + x * 4 + 2] = 0xFF;
                _framebuffer[offset + x * 4 + 3] = 0xFF;
            }
            return;
        }

        // Render background and window
        RenderBackgroundLine(line);

        // Render sprites
        if ((_lcdc & 0x02) != 0)
            RenderSpritesLine(line);

// At the end of RenderLine, after filling _bgLine and before copying to framebuffer:
if (line == 0)
    Console.WriteLine($"PPU line 0 first pixel color: {_bgLine[0]}");
        // Copy to framebuffer
        int fbOffset = line * Width * 4;
        for (int x = 0; x < Width; x++)
        {
            byte color = _bgLine[x];
            // Convert color (0-3) to RGBA (0 = white, 3 = black)
            byte r, g, b;
            switch (color)
            {
                case 0: r = g = b = 0xFF; break; // white
                case 1: r = g = b = 0xAA; break; // light gray
                case 2: r = g = b = 0x55; break; // dark gray
                case 3: r = g = b = 0x00; break; // black
                default: r = g = b = 0; break;
            }
            _framebuffer[fbOffset + x * 4 + 0] = r;
            _framebuffer[fbOffset + x * 4 + 1] = g;
            _framebuffer[fbOffset + x * 4 + 2] = b;
            _framebuffer[fbOffset + x * 4 + 3] = 0xFF;
        }
    }

    private void RenderBackgroundLine(int line)
    {
        // Background and window share the same tile data
        bool bgEnabled = (_lcdc & 0x01) != 0;
        bool winEnabled = (_lcdc & 0x20) != 0;

        int bgTileMapBase = ((_lcdc & 0x08) != 0) ? 0x1C00 : 0x1800; // 0x9800 or 0x9C00 relative to VRAM start
        int winTileMapBase = ((_lcdc & 0x40) != 0) ? 0x1C00 : 0x1800;
        bool tileDataSigned = ((_lcdc & 0x10) != 0); // 0x8000 method if true, else 0x8800 signed

        int y = line + _scy;
        for (int x = 0; x < Width; x++)
        {
            int bgX = (x + _scx) & 255;
            int tileY = (y / 8) & 31;
            int tileX = bgX / 8;
            int tilePixelX = bgX % 8;
            int tilePixelY = y % 8;

            int tileMapAddr = bgTileMapBase + tileY * 32 + tileX;
            byte tileNumber = _vram[tileMapAddr];

            int tileDataAddr;
            if (tileDataSigned)
            {
                tileDataAddr = (tileNumber * 16) + (tilePixelY * 2);
            }
            else
            {
                // signed mode: tile numbers 0-127 are at 0x0800, 128-255 at 0x0000
                if (tileNumber < 128)
                    tileDataAddr = 0x1000 + tileNumber * 16 + tilePixelY * 2;
                else
                    tileDataAddr = 0x0000 + (tileNumber - 128) * 16 + tilePixelY * 2;
            }

            byte low = _vram[tileDataAddr];
            byte high = _vram[tileDataAddr + 1];
            int bit = 7 - tilePixelX;
            int color = ((high >> bit) & 1) << 1 | ((low >> bit) & 1);
            _bgLine[x] = _bgPalette[color];
        }

        // Window overlay (if enabled and line within window)
        if (winEnabled && line >= _wy)
        {
            int winY = line - _wy;
            if (winY < 144)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (x + 7 < _wx) continue; // window starts at X=wx-7
                    int winX = x - (_wx - 7);
                    if (winX < 0) continue;
                    int tileY = (winY / 8) & 31;
                    int tileX = winX / 8;
                    int tilePixelX = winX % 8;
                    int tilePixelY = winY % 8;

                    int tileMapAddr = winTileMapBase + tileY * 32 + tileX;
                    byte tileNumber = _vram[tileMapAddr];

                    int tileDataAddr;
                    if (tileDataSigned)
                    {
                        tileDataAddr = tileNumber * 16 + tilePixelY * 2;
                    }
                    else
                    {
                        if (tileNumber < 128)
                            tileDataAddr = 0x1000 + tileNumber * 16 + tilePixelY * 2;
                        else
                            tileDataAddr = 0x0000 + (tileNumber - 128) * 16 + tilePixelY * 2;
                    }

                    byte low = _vram[tileDataAddr];
                    byte high = _vram[tileDataAddr + 1];
                    int bit = 7 - tilePixelX;
                    int color = ((high >> bit) & 1) << 1 | ((low >> bit) & 1);
                    _bgLine[x] = _bgPalette[color];
                }
            }
        }
    }

    private void RenderSpritesLine(int line)
    {
        bool spriteHeight16 = (_lcdc & 0x04) != 0;
        for (int i = 0; i < _spriteCount; i++)
        {
            var spr = _sprites[i];
            int yPos = spr.Y - 16;
            int xPos = spr.X - 8;
            int tileIndex = spr.Tile;
            byte flags = spr.Flags;

            // Determine tile line
            int tileLine = line - yPos;
            if (spriteHeight16)
            {
                tileIndex &= 0xFE; // LSB of tile number is ignored for 8x16
                if ((flags & 0x40) != 0) // Y flip
                    tileLine = 15 - tileLine;
            }
            else
            {
                if ((flags & 0x40) != 0)
                    tileLine = 7 - tileLine;
            }

            int tileDataAddr;
            if (spriteHeight16)
            {
                tileDataAddr = (tileIndex + (tileLine / 8)) * 16 + (tileLine % 8) * 2;
            }
            else
            {
                tileDataAddr = tileIndex * 16 + tileLine * 2;
            }

            byte low = _vram[tileDataAddr];
            byte high = _vram[tileDataAddr + 1];

            for (int p = 0; p < 8; p++)
            {
                int bit = (flags & 0x20) != 0 ? p : 7 - p; // X flip
                int color = ((high >> bit) & 1) << 1 | ((low >> bit) & 1);
                if (color == 0) continue; // transparent

                int screenX = xPos + p;
                if (screenX < 0 || screenX >= Width) continue;

                // Priority: sprite above background if bit 7 of flags is clear
                bool bgPriority = (flags & 0x80) != 0;
                if (bgPriority && _bgLine[screenX] != 0) continue;

                // Apply palette
                byte paletteColor;
                if ((flags & 0x10) != 0)
                    paletteColor = _objPalette1[color];
                else
                    paletteColor = _objPalette0[color];

                _bgLine[screenX] = paletteColor;
            }
        }
    }

    private void CompareLY()
    {
        if (_ly == _lyc)
        {
            _stat |= 0x04;
            if ((_stat & 0x40) != 0)
                _int.RequestInterrupt(1);
        }
        else
        {
            _stat &= 0xFB;
        }
    }

    public async Task RenderFrame()
    {
        await _js.InvokeVoidAsync("gameboy3.render", _canvas, _framebuffer);
    }

    // MMU access methods
    public byte ReadRegister(ushort addr)
    {
        switch (addr)
        {
            case 0xFF40: return _lcdc;
            case 0xFF41: return (byte)((_stat & 0x78) | 0x80);
            case 0xFF42: return _scy;
            case 0xFF43: return _scx;
            case 0xFF44: return _ly;
            case 0xFF45: return _lyc;
            case 0xFF47: return _bgp;
            case 0xFF48: return _obp0;
            case 0xFF49: return _obp1;
            case 0xFF4A: return _wy;
            case 0xFF4B: return _wx;
            default: return 0xFF;
        }
    }

    public void WriteRegister(ushort addr, byte value)
{
    switch (addr)
    {
        case 0xFF40:
            Console.WriteLine($"LCDC set to 0x{value:X2}");
            _lcdc = value;
            break;
        case 0xFF41:
            _stat = (byte)((value & 0x78) | (_stat & 0x07));
            break;
        case 0xFF42:
            _scy = value;
            break;
        case 0xFF43:
            _scx = value;
            break;
        case 0xFF44:
            /* read‑only */
            break;
        case 0xFF45:
            _lyc = value;
            break;
        case 0xFF47:
            _bgp = value;
            UpdatePalettes();
            break;
        case 0xFF48:
            _obp0 = value;
            UpdatePalettes();
            break;
        case 0xFF49:
            _obp1 = value;
            UpdatePalettes();
            break;
        case 0xFF4A:
            _wy = value;
            break;
        case 0xFF4B:
            _wx = value;
            break;
    }
}
}