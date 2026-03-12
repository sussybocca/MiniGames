namespace MiniGames.Emulators.GameBoy3;

public class Input
{
    // Button states (true = pressed)
    private bool _right, _left, _up, _down, _a, _b, _select, _start;

    // Register 0xFF00
    private byte _p1 = 0xFF; // all bits high initially

    public void SetButton(int button, bool pressed)
    {
        switch (button)
        {
            case 0: _right = pressed; break;
            case 1: _left  = pressed; break;
            case 2: _up    = pressed; break;
            case 3: _down  = pressed; break;
            case 4: _a     = pressed; break;
            case 5: _b     = pressed; break;
            case 6: _select= pressed; break;
            case 7: _start = pressed; break;
        }
    }

    public byte ReadJoypad()
    {
        byte res = 0xCF; // bits 4-5 are controlled by P1, others high
        bool selAction = (_p1 & 0x20) == 0; // bit 5 low -> action buttons
        bool selDir    = (_p1 & 0x10) == 0; // bit 4 low -> direction buttons

        if (selDir)
        {
            if (_right) res &= 0xFE;
            if (_left)  res &= 0xFD;
            if (_up)    res &= 0xFB;
            if (_down)  res &= 0xF7;
        }
        if (selAction)
        {
            if (_a)      res &= 0xFE;
            if (_b)      res &= 0xFD;
            if (_select) res &= 0xFB;
            if (_start)  res &= 0xF7;
        }
        return res;
    }

    public void WriteJoypad(byte value)
    {
        _p1 = (byte)(value | 0xCF); // only bits 4-5 writable
    }
}