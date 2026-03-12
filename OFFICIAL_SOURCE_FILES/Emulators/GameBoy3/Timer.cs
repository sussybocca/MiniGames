namespace MiniGames.Emulators.GameBoy3;

public class Timer
{
    private readonly Interrupts _int;

    // Timer registers
    private ushort _div;      // DIV (16‑bit internal, only high byte readable)
    private byte _tima;       // TIMA (timer counter)
    private byte _tma;        // TMA (timer modulo)
    private byte _tac;        // TAC (timer control)

    // Internal counter for TIMA increment
    private int _timaCounter;

    public Timer(Interrupts intHandler)
    {
        _int = intHandler;
        Reset();
    }

    public void Reset()
    {
        _div = 0xABCC; // typical initial value after boot ROM
        _tima = 0;
        _tma = 0;
        _tac = 0;
        _timaCounter = 0;
    }

    public void Update(int cycles)
    {
        // Update DIV (always increments at 16384 Hz)
        for (int i = 0; i < cycles; i++)
        {
            _div++; // 16‑bit counter

            // Check TIMA increment based on TAC and selected frequency
            bool timerEnabled = (_tac & 0x04) != 0;
            if (!timerEnabled) continue;

            int freqBit = _tac & 0x03;
            int bitPos = freqBit switch
            {
                0 => 9,  // 4096 Hz (bit 9 of DIV)
                1 => 3,  // 262144 Hz (bit 3)
                2 => 5,  // 65536 Hz (bit 5)
                3 => 7,  // 16384 Hz (bit 7)
                _ => 9
            };

            // Check rising edge on the selected bit
            if (((_div >> bitPos) & 1) != 0 && ((_div - 1) >> bitPos & 1) == 0)
            {
                _timaCounter++;
                if (_timaCounter > 0) // actually increment TIMA after each detected edge
                {
                    _tima++;
                    if (_tima == 0)
                    {
                        _tima = _tma; // reload
                        _int.RequestInterrupt(2); // timer interrupt
                    }
                }
            }
        }
    }

    public byte ReadByte(ushort addr)
    {
        return addr switch
        {
            0xFF04 => (byte)(_div >> 8), // DIV high byte
            0xFF05 => _tima,
            0xFF06 => _tma,
            0xFF07 => (byte)(_tac | 0xF8), // low 3 bits only
            _ => 0xFF
        };
    }

    public void WriteByte(ushort addr, byte value)
    {
        switch (addr)
        {
            case 0xFF04: _div = 0; break; // reset DIV
            case 0xFF05: _tima = value; break;
            case 0xFF06: _tma = value; break;
            case 0xFF07: _tac = (byte)(value & 0x07); break;
        }
    }
}