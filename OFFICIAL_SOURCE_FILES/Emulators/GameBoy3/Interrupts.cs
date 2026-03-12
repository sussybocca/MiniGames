namespace MiniGames.Emulators.GameBoy3;

public class Interrupts
{
    // Interrupt flags (IF) – address 0xFF0F
    private byte _if = 0xE0; // top 3 bits always 1
    // Interrupt enable (IE) – address 0xFFFF
    private byte _ie = 0x00;

    public bool HasPending => (_ie & _if & 0x1F) != 0;

    // Interrupt vectors
    private const int VBLANK = 0; // 0x40
    private const int LCDC   = 1; // 0x48
    private const int TIMER  = 2; // 0x50
    private const int SERIAL = 3; // 0x58
    private const int JOYPAD = 4; // 0x60

    public void RequestInterrupt(int bit)
    {
        Console.WriteLine($"Interrupt requested: bit {bit}");
        _if |= (byte)(1 << bit);
    }

    public void ClearInterrupt(int bit)
    {
        _if &= (byte)~(1 << bit);
    }

    public int Acknowledge()
    {
        for (int i = 0; i < 5; i++)
        {
            if ((_ie & (1 << i)) != 0 && (_if & (1 << i)) != 0)
            {
                ClearInterrupt(i);
                return i;
            }
        }
        return -1;
    }

    public byte ReadIF() => (byte)(_if | 0xE0);
    public void WriteIF(byte value) => _if = (byte)(value & 0x1F);

    public byte ReadIE() => _ie;
    public void WriteIE(byte value) => _ie = value;

    public void Update() { } // no per‑cycle update needed
}