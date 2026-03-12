using System;

namespace MiniGames.Emulators.GameBoy3;

public class MMU
{
    private readonly byte[] _rom;          // ROM data (up to 2MB with MBC1)
    private readonly byte[] _ram;          // Cartridge RAM (up to 32KB)
    private readonly byte[] _vram;         // Video RAM (8KB)
    private readonly byte[] _wram;         // Work RAM (8KB, 2 banks)
    private readonly byte[] _oam;          // Object Attribute Memory (160 bytes)
    private readonly byte[] _hram;         // High RAM (127 bytes)

    private readonly Interrupts _int;
    private readonly Timer _timer;
    private readonly Input _input;
    private PPU? _ppu;                      // Attached after construction

    // Cartridge type – full list from pandocs
    private enum MapperType
    {
        None,
        MBC1,
        MBC2,
        MBC3,
        MBC5,
        MMM01,
        MBC6,
        MBC7,
        PocketCamera,
        BandaiTAMA5,
        HuC3,
        HuC1,
        Unknown
    }
    private MapperType _mapper;
    private bool _hasRam;
    private bool _hasBattery;
    private bool _hasRtc; // for MBC3

    // Bank state
    private int _romBank;                   // current ROM bank (1‑based for most mappers)
    private int _ramBank;                    // current RAM bank (0‑3 for MBC1/3, up to 0xF for MBC5)
    private bool _ramEnable;
    private int _romBankMask;                // mask for valid ROM banks

    // MBC3 RTC registers
    private byte _rtcSeconds;
    private byte _rtcMinutes;
    private byte _rtcHours;
    private byte _rtcDaysLsb;
    private byte _rtcDaysMsb;                // bit0 = day carry, bits1-7 unused? We'll just store full 9-bit days in two bytes.
    private bool _rtcLatched;                 // whether RTC is latched
    private byte[] _rtcLatchedRegs = new byte[5]; // latched copy (seconds, minutes, hours, days low, days high)

    // For RTC update (called from emulation loop)
    private long _rtcLastUpdate;
    private const long RTC_UPDATE_CYCLES = 262144; // approx 1 second at 4.19 MHz

    public MMU(byte[] romData, Interrupts intHandler, Input input, Timer timer)
    {
        _int = intHandler;
        _input = input;
        _timer = timer;

        // Determine cartridge type from header byte 0x0147
        byte cartridgeType = romData.Length > 0x147 ? romData[0x147] : (byte)0;
        _mapper = cartridgeType switch
        {
            0x00 => MapperType.None,
            0x01 => MapperType.MBC1,
            0x02 => MapperType.MBC1,
            0x03 => MapperType.MBC1,
            0x05 => MapperType.MBC2,
            0x06 => MapperType.MBC2,
            0x08 => MapperType.None, // ROM+RAM
            0x09 => MapperType.None, // ROM+RAM+BATTERY
            0x0B => MapperType.MMM01,
            0x0C => MapperType.MMM01,
            0x0D => MapperType.MMM01,
            0x0F => MapperType.MBC3,
            0x10 => MapperType.MBC3,
            0x11 => MapperType.MBC3,
            0x12 => MapperType.MBC3,
            0x13 => MapperType.MBC3,
            0x19 => MapperType.MBC5,
            0x1A => MapperType.MBC5,
            0x1B => MapperType.MBC5,
            0x1C => MapperType.MBC5,
            0x1D => MapperType.MBC5,
            0x1E => MapperType.MBC5,
            0x20 => MapperType.MBC6,
            0x22 => MapperType.MBC7,
            0xFC => MapperType.PocketCamera,
            0xFD => MapperType.BandaiTAMA5,
            0xFE => MapperType.HuC3,
            0xFF => MapperType.HuC1,
            _ => MapperType.Unknown
        };

        // RAM size from header byte 0x0149
        int ramSize = romData.Length > 0x149 ? romData[0x149] : 0;
        ramSize = ramSize switch
        {
            0 => 0,
            1 => 2 * 1024,
            2 => 8 * 1024,
            3 => 32 * 1024,
            4 => 128 * 1024, // for MBC2? Actually MBC2 has built‑in 512×4 bits, but we treat as 512 bytes
            5 => 64 * 1024,
            _ => 0
        };
        _ram = new byte[ramSize];

        // ROM size – allocate full ROM
        _rom = new byte[romData.Length];
        Array.Copy(romData, _rom, romData.Length);

        // Initialize memory arrays
        _vram = new byte[0x2000];
        _wram = new byte[0x2000];
        _oam = new byte[0xA0];
        _hram = new byte[0x7F];

        // Initial bank state
        _romBank = 1;
        _ramBank = 0;
        _ramEnable = false;

        // ROM bank mask based on ROM size (number of 16KB banks - 1)
        _romBankMask = (romData.Length >> 14) - 1;
        if (_romBankMask < 0) _romBankMask = 0;

        // Initialize RTC (start at some reasonable values, e.g., 0)
        _rtcSeconds = 0;
        _rtcMinutes = 0;
        _rtcHours = 0;
        _rtcDaysLsb = 0;
        _rtcDaysMsb = 0;
        _rtcLastUpdate = 0;
    }

    public void AttachPPU(PPU ppu) => _ppu = ppu;

    public byte ReadByte(ushort addr)
    {
        if (addr <= 0x3FFF)
        {
            // Bank 0 always fixed (except for some MBC1 modes, but we ignore)
            return _rom[addr];
        }
        if (addr <= 0x7FFF)
        {
            // Switchable ROM bank
            int bank = _romBank;
            // For MBC1 in mode 0, bank 0 is accessible, but we treat bank 1 as bank 1
            // Ensure bank is within range
            bank = Math.Max(1, bank) & _romBankMask;
            return _rom[(bank * 0x4000) + (addr - 0x4000)];
        }
        if (addr <= 0x9FFF)
            return _vram[addr - 0x8000];
        if (addr <= 0xBFFF)
        {
            // If RAM is enabled, check if we're in RTC mode or RAM mode
            if (_ramEnable)
            {
                if (_ramBank >= 0x08 && _ramBank <= 0x0C)
                {
                    // RTC register selected
                    if (_rtcLatched)
                    {
                        // Return latched value
                        return _ramBank switch
                        {
                            0x08 => _rtcLatchedRegs[0],
                            0x09 => _rtcLatchedRegs[1],
                            0x0A => _rtcLatchedRegs[2],
                            0x0B => _rtcLatchedRegs[3],
                            0x0C => _rtcLatchedRegs[4],
                            _ => 0xFF
                        };
                    }
                    else
                    {
                        // Return current RTC value
                        return _ramBank switch
                        {
                            0x08 => _rtcSeconds,
                            0x09 => _rtcMinutes,
                            0x0A => _rtcHours,
                            0x0B => _rtcDaysLsb,
                            0x0C => _rtcDaysMsb,
                            _ => 0xFF
                        };
                    }
                }
                else if (_ram.Length > 0 && _ramBank < 0x08)
                {
                    // RAM bank
                    int bank = _ramBank;
                    if (bank < 0) bank = 0;
                    if (bank * 0x2000 < _ram.Length)
                        return _ram[(bank * 0x2000) + (addr - 0xA000)];
                }
            }
            return 0xFF;
        }
        if (addr <= 0xDFFF)
            return _wram[addr - 0xC000];
        if (addr <= 0xFDFF)
            return _wram[addr - 0xE000]; // echo RAM
        if (addr <= 0xFE9F)
            return _oam[addr - 0xFE00];
        if (addr >= 0xFEA0 && addr <= 0xFEFF)
            return 0xFF; // unusable area
        if (addr == 0xFF00)
            return _input.ReadJoypad();
        if (addr == 0xFF01 || addr == 0xFF02)
            return 0xFF; // serial – not emulated
        if (addr >= 0xFF04 && addr <= 0xFF07)
            return _timer.ReadByte(addr);
        if (addr == 0xFF0F)
            return _int.ReadIF();
        if (addr >= 0xFF40 && addr <= 0xFF4B)
        {
            if (_ppu != null)
                return _ppu.ReadRegister(addr);
            return 0xFF;
        }
        if (addr >= 0xFF80)
            return _hram[addr - 0xFF80];
        if (addr == 0xFFFF)
            return _int.ReadIE();
        return 0xFF;
    }

    public void WriteByte(ushort addr, byte value)
    {
        // Handle banking registers based on mapper
        switch (_mapper)
        {
            case MapperType.MBC1:
                HandleMBC1Write(addr, value);
                break;
            case MapperType.MBC3:
                HandleMBC3Write(addr, value);
                break;
            case MapperType.MBC5:
                HandleMBC5Write(addr, value);
                break;
            case MapperType.None:
                // No mapper – ignore writes to ROM area
                break;
            default:
                // Unsupported mapper – ignore or log
                break;
        }

        // Handle writes to other memory regions (these are independent of mapper)
        if (addr <= 0x9FFF)
        {
            _vram[addr - 0x8000] = value;
            return;
        }
        if (addr <= 0xBFFF)
        {
            if (_ramEnable)
            {
                if (_ramBank >= 0x08 && _ramBank <= 0x0C)
                {
                    // Write to RTC register
                    switch (_ramBank)
                    {
                        case 0x08: _rtcSeconds = value; break;
                        case 0x09: _rtcMinutes = value; break;
                        case 0x0A: _rtcHours = value; break;
                        case 0x0B: _rtcDaysLsb = value; break;
                        case 0x0C: _rtcDaysMsb = value; break;
                    }
                    return;
                }
                else if (_ram.Length > 0 && _ramBank < 0x08)
                {
                    int bank = _ramBank;
                    if (bank < 0) bank = 0;
                    if (bank * 0x2000 < _ram.Length)
                        _ram[(bank * 0x2000) + (addr - 0xA000)] = value;
                }
            }
            return;
        }
        if (addr <= 0xDFFF)
        {
            _wram[addr - 0xC000] = value;
            return;
        }
        if (addr <= 0xFDFF)
        {
            _wram[addr - 0xE000] = value; // echo
            return;
        }
        if (addr <= 0xFE9F)
        {
            _oam[addr - 0xFE00] = value;
            return;
        }
        if (addr == 0xFF00)
        {
            _input.WriteJoypad(value);
            return;
        }
        if (addr == 0xFF01 || addr == 0xFF02)
        {
            // serial – ignore
            return;
        }
        if (addr >= 0xFF04 && addr <= 0xFF07)
        {
            _timer.WriteByte(addr, value);
            return;
        }
        if (addr == 0xFF0F)
        {
            _int.WriteIF(value);
            return;
        }
        if (addr >= 0xFF40 && addr <= 0xFF4B)
        {
            _ppu?.WriteRegister(addr, value);
            return;
        }
        if (addr >= 0xFF80)
        {
            _hram[addr - 0xFF80] = value;
            return;
        }
        if (addr == 0xFFFF)
        {
            _int.WriteIE(value);
            return;
        }
    }

    private void HandleMBC1Write(ushort addr, byte value)
    {
        if (addr <= 0x1FFF)
        {
            _ramEnable = (value & 0x0F) == 0x0A;
        }
        else if (addr <= 0x3FFF)
        {
            int bank = value & 0x1F;
            if (bank == 0) bank = 1;
            _romBank = (_romBank & 0x60) | bank;
            _romBank &= _romBankMask;
        }
        else if (addr <= 0x5FFF)
        {
            if ((_romBankMask & 0x60) != 0)
                _romBank = (_romBank & 0x1F) | ((value & 0x03) << 5);
            _ramBank = value & 0x03;
        }
        else if (addr <= 0x7FFF)
        {
            // Mode select – ignored for simplicity
        }
    }

    private void HandleMBC3Write(ushort addr, byte value)
    {
        if (addr <= 0x1FFF)
        {
            _ramEnable = (value & 0x0F) == 0x0A;
        }
        else if (addr <= 0x3FFF)
        {
            int bank = value & 0x7F;
            if (bank == 0) bank = 1;
            _romBank = bank & _romBankMask;
        }
        else if (addr <= 0x5FFF)
        {
            if (value >= 0x08 && value <= 0x0C)
            {
                // Select RTC register
                _ramBank = value;
            }
            else
            {
                _ramBank = value & 0x03;
            }
        }
        else if (addr <= 0x7FFF)
        {
            // Latch RTC
            // Writing any value latches the current RTC time into the latched registers
            _rtcLatched = true;
            _rtcLatchedRegs[0] = _rtcSeconds;
            _rtcLatchedRegs[1] = _rtcMinutes;
            _rtcLatchedRegs[2] = _rtcHours;
            _rtcLatchedRegs[3] = _rtcDaysLsb;
            _rtcLatchedRegs[4] = _rtcDaysMsb;
        }
    }

    private void HandleMBC5Write(ushort addr, byte value)
    {
        if (addr <= 0x1FFF)
        {
            _ramEnable = (value & 0x0F) == 0x0A;
        }
        else if (addr <= 0x2FFF)
        {
            _romBank = (_romBank & 0x100) | value;
            if ((_romBank & _romBankMask) == 0) _romBank = 1;
        }
        else if (addr <= 0x3FFF)
        {
            _romBank = (_romBank & 0xFF) | ((value & 0x01) << 8);
            if ((_romBank & _romBankMask) == 0) _romBank = 1;
        }
        else if (addr <= 0x5FFF)
        {
            _ramBank = value & 0x0F;
        }
    }

    public ushort ReadWord(ushort addr)
    {
        return (ushort)(ReadByte(addr) | (ReadByte((ushort)(addr + 1)) << 8));
    }

    public void WriteWord(ushort addr, ushort value)
    {
        WriteByte(addr, (byte)(value & 0xFF));
        WriteByte((ushort)(addr + 1), (byte)(value >> 8));
    }

    public byte[] GetVRAM() => _vram;
    public byte[] GetOAM() => _oam;

    // Call this periodically from the emulation loop (e.g., every frame) to advance RTC
    public void UpdateRTC(int cycles)
    {
        if (!_hasRtc) return;
        // Simple RTC update – increment seconds every ~1 second (262144 cycles at 4.19 MHz)
        _rtcLastUpdate += cycles;
        while (_rtcLastUpdate >= RTC_UPDATE_CYCLES)
        {
            _rtcLastUpdate -= RTC_UPDATE_CYCLES;
            _rtcSeconds++;
            if (_rtcSeconds >= 60)
            {
                _rtcSeconds = 0;
                _rtcMinutes++;
                if (_rtcMinutes >= 60)
                {
                    _rtcMinutes = 0;
                    _rtcHours++;
                    if (_rtcHours >= 24)
                    {
                        _rtcHours = 0;
                        // Increment days (9-bit)
                        ushort days = (ushort)((_rtcDaysMsb << 8) | _rtcDaysLsb);
                        days++;
                        if (days >= 512)
                        {
                            days = 0;
                            // Set carry flag? MBC3 days overflow bit in bit 6 of days high? We'll ignore for now.
                        }
                        _rtcDaysLsb = (byte)(days & 0xFF);
                        _rtcDaysMsb = (byte)((days >> 8) & 0x01);
                    }
                }
            }
        }
    }
}