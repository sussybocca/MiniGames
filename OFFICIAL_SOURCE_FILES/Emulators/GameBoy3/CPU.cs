using System;

namespace MiniGames.Emulators.GameBoy3
{
    /// <summary>
    /// GameBoy LR35902 CPU core – full implementation of all documented opcodes.
    /// </summary>
    public class CPU
    {
        private readonly MMU _mmu;
        private readonly Interrupts _int;

        // Registers
        public ushort PC { get; set; } = 0x0100;
        public ushort SP { get; set; } = 0xFFFE;

        public byte A { get; set; } = 0x01;
        public byte F { get; set; } = 0xB0;
        public byte B { get; set; } = 0x00;
        public byte C { get; set; } = 0x13;
        public byte D { get; set; } = 0x00;
        public byte E { get; set; } = 0xD8;
        public byte H { get; set; } = 0x01;
        public byte L { get; set; } = 0x4D;

        // Flag accessors
        public bool ZeroFlag
        {
            get => (F & 0x80) != 0;
            set => F = (byte)((F & ~0x80) | (value ? 0x80 : 0));
        }
        public bool SubtractFlag
        {
            get => (F & 0x40) != 0;
            set => F = (byte)((F & ~0x40) | (value ? 0x40 : 0));
        }
        public bool HalfCarryFlag
        {
            get => (F & 0x20) != 0;
            set => F = (byte)((F & ~0x20) | (value ? 0x20 : 0));
        }
        public bool CarryFlag
        {
            get => (F & 0x10) != 0;
            set => F = (byte)((F & ~0x10) | (value ? 0x10 : 0));
        }

        // 16-bit pairs
        public ushort AF
        {
            get => (ushort)((A << 8) | F);
            set { A = (byte)(value >> 8); F = (byte)(value & 0xF0); }
        }
        public ushort BC
        {
            get => (ushort)((B << 8) | C);
            set { B = (byte)(value >> 8); C = (byte)(value & 0xFF); }
        }
        public ushort DE
        {
            get => (ushort)((D << 8) | E);
            set { D = (byte)(value >> 8); E = (byte)(value & 0xFF); }
        }
        public ushort HL
        {
            get => (ushort)((H << 8) | L);
            set { H = (byte)(value >> 8); L = (byte)(value & 0xFF); }
        }

        public bool IME { get; set; } = false;   // Interrupt Master Enable
        public bool Halt { get; set; } = false;
        public bool Stop { get; set; } = false;
        public bool HaltBug { get; set; } = false; // For halt bug emulation

        public CPU(MMU mmu, Interrupts intHandler)
        {
            _mmu = mmu;
            _int = intHandler;
        }

        public int ExecuteNext()
        {
            // ----- HALT handling -----
            if (Halt)
            {
                // If an interrupt is pending and interrupts are enabled, wake up
                if (_int.HasPending && IME)
                {
                    Halt = false;
                    // Service the interrupt immediately (no instruction executed)
                    int intNum = _int.Acknowledge();
                    Console.WriteLine($"Servicing interrupt {intNum}, PC=0x{PC:X4}, SP=0x{SP:X4}");
                    _mmu.WriteByte(--SP, (byte)(PC >> 8));
                    _mmu.WriteByte(--SP, (byte)(PC & 0xFF));
                    PC = (ushort)(0x0040 + intNum * 8);
                    IME = false;
                    return 20; // approximate cycles for interrupt
                }
                else
                {
                    // No interrupt pending – stay halted
                    return 1;
                }
            }

            if (Stop) return 1; // Not fully emulated

            // ----- Fetch and execute next opcode -----
            byte opcode = _mmu.ReadByte(PC++);
            int cycles = ExecuteOpcode(opcode);

            // ----- Handle interrupts after instruction -----
            if (_int.HasPending && IME)
            {
                Halt = false;
                int intNum = _int.Acknowledge();
                if (intNum >= 0)
                {
                    Console.WriteLine($"Servicing interrupt {intNum}, PC=0x{PC:X4}, SP=0x{SP:X4}");
                    _mmu.WriteByte(--SP, (byte)(PC >> 8));
                    _mmu.WriteByte(--SP, (byte)(PC & 0xFF));
                    PC = (ushort)(0x0040 + intNum * 8);
                    IME = false;
                }
            }

            return cycles;
        }

        private int ExecuteOpcode(byte opcode)
        {
            switch (opcode)
            {
                // 8‑bit loads
                case 0x00: return 4; // NOP
                case 0x01: // LD BC, imm16
                    C = _mmu.ReadByte(PC++);
                    B = _mmu.ReadByte(PC++);
                    return 12;
                case 0x02: // LD (BC), A
                    _mmu.WriteByte(BC, A);
                    return 8;
                case 0x03: // INC BC
                    BC++;
                    return 8;
                case 0x04: // INC B
                    B = Inc(B);
                    return 4;
                case 0x05: // DEC B
                    B = Dec(B);
                    return 4;
                case 0x06: // LD B, d8
                    B = _mmu.ReadByte(PC++);
                    return 8;
                case 0x07: // RLCA
                    RLCA();
                    return 4;
                case 0x08: // LD (imm16), SP
                    {
                        ushort addr = _mmu.ReadWord(PC); PC += 2;
                        _mmu.WriteWord(addr, SP);
                    }
                    return 20;
                case 0x09: // ADD HL, BC
                    AddHL(BC);
                    return 8;
                case 0x0A: // LD A, (BC)
                    A = _mmu.ReadByte(BC);
                    return 8;
                case 0x0B: // DEC BC
                    BC--;
                    return 8;
                case 0x0C: // INC C
                    C = Inc(C);
                    return 4;
                case 0x0D: // DEC C
                    C = Dec(C);
                    return 4;
                case 0x0E: // LD C, d8
                    C = _mmu.ReadByte(PC++);
                    return 8;
                case 0x0F: // RRCA
                    RRCA();
                    return 4;
                case 0x10: // STOP
                    Stop = true;
                    PC++; // skip next byte (ignored)
                    return 4;
                case 0x11: // LD DE, imm16
                    E = _mmu.ReadByte(PC++);
                    D = _mmu.ReadByte(PC++);
                    return 12;
                case 0x12: // LD (DE), A
                    _mmu.WriteByte(DE, A);
                    return 8;
                case 0x13: // INC DE
                    DE++;
                    return 8;
                case 0x14: // INC D
                    D = Inc(D);
                    return 4;
                case 0x15: // DEC D
                    D = Dec(D);
                    return 4;
                case 0x16: // LD D, d8
                    D = _mmu.ReadByte(PC++);
                    return 8;
                case 0x17: // RLA
                    RLA();
                    return 4;
                case 0x18: // JR s8
                    {
                        sbyte offset = (sbyte)_mmu.ReadByte(PC++);
                        PC = (ushort)(PC + offset);
                    }
                    return 12;
                case 0x19: // ADD HL, DE
                    AddHL(DE);
                    return 8;
                case 0x1A: // LD A, (DE)
                    A = _mmu.ReadByte(DE);
                    return 8;
                case 0x1B: // DEC DE
                    DE--;
                    return 8;
                case 0x1C: // INC E
                    E = Inc(E);
                    return 4;
                case 0x1D: // DEC E
                    E = Dec(E);
                    return 4;
                case 0x1E: // LD E, d8
                    E = _mmu.ReadByte(PC++);
                    return 8;
                case 0x1F: // RRA
                    RRA();
                    return 4;
                case 0x20: // JR NZ, s8
                    if (!ZeroFlag)
                    {
                        sbyte offset = (sbyte)_mmu.ReadByte(PC++);
                        PC = (ushort)(PC + offset);
                        return 12;
                    }
                    PC++;
                    return 8;
                case 0x21: // LD HL, imm16
                    L = _mmu.ReadByte(PC++);
                    H = _mmu.ReadByte(PC++);
                    return 12;
                case 0x22: // LD (HL+), A
                    _mmu.WriteByte(HL, A);
                    HL++;
                    return 8;
                case 0x23: // INC HL
                    HL++;
                    return 8;
                case 0x24: // INC H
                    H = Inc(H);
                    return 4;
                case 0x25: // DEC H
                    H = Dec(H);
                    return 4;
                case 0x26: // LD H, d8
                    H = _mmu.ReadByte(PC++);
                    return 8;
                case 0x27: // DAA
                    DAA();
                    return 4;
                case 0x28: // JR Z, s8
                    if (ZeroFlag)
                    {
                        sbyte offset = (sbyte)_mmu.ReadByte(PC++);
                        PC = (ushort)(PC + offset);
                        return 12;
                    }
                    PC++;
                    return 8;
                case 0x29: // ADD HL, HL
                    AddHL(HL);
                    return 8;
                case 0x2A: // LD A, (HL+)
                    A = _mmu.ReadByte(HL);
                    HL++;
                    return 8;
                case 0x2B: // DEC HL
                    HL--;
                    return 8;
                case 0x2C: // INC L
                    L = Inc(L);
                    return 4;
                case 0x2D: // DEC L
                    L = Dec(L);
                    return 4;
                case 0x2E: // LD L, d8
                    L = _mmu.ReadByte(PC++);
                    return 8;
                case 0x2F: // CPL
                    A = (byte)~A;
                    SubtractFlag = true;
                    HalfCarryFlag = true;
                    return 4;
                case 0x30: // JR NC, s8
                    if (!CarryFlag)
                    {
                        sbyte offset = (sbyte)_mmu.ReadByte(PC++);
                        PC = (ushort)(PC + offset);
                        return 12;
                    }
                    PC++;
                    return 8;
                case 0x31: // LD SP, imm16
                    SP = _mmu.ReadWord(PC); PC += 2;
                    return 12;
                case 0x32: // LD (HL-), A
                    _mmu.WriteByte(HL, A);
                    HL--;
                    return 8;
                case 0x33: // INC SP
                    SP++;
                    return 8;
                case 0x34: // INC (HL)
                    {
                        byte val = _mmu.ReadByte(HL);
                        val = Inc(val);
                        _mmu.WriteByte(HL, val);
                    }
                    return 12;
                case 0x35: // DEC (HL)
                    {
                        byte val = _mmu.ReadByte(HL);
                        val = Dec(val);
                        _mmu.WriteByte(HL, val);
                    }
                    return 12;
                case 0x36: // LD (HL), d8
                    _mmu.WriteByte(HL, _mmu.ReadByte(PC++));
                    return 12;
                case 0x37: // SCF
                    CarryFlag = true;
                    SubtractFlag = false;
                    HalfCarryFlag = false;
                    return 4;
                case 0x38: // JR C, s8
                    if (CarryFlag)
                    {
                        sbyte offset = (sbyte)_mmu.ReadByte(PC++);
                        PC = (ushort)(PC + offset);
                        return 12;
                    }
                    PC++;
                    return 8;
                case 0x39: // ADD HL, SP
                    AddHL(SP);
                    return 8;
                case 0x3A: // LD A, (HL-)
                    A = _mmu.ReadByte(HL);
                    HL--;
                    return 8;
                case 0x3B: // DEC SP
                    SP--;
                    return 8;
                case 0x3C: // INC A
                    A = Inc(A);
                    return 4;
                case 0x3D: // DEC A
                    A = Dec(A);
                    return 4;
                case 0x3E: // LD A, d8
                    A = _mmu.ReadByte(PC++);
                    return 8;
                case 0x3F: // CCF
                    CarryFlag = !CarryFlag;
                    SubtractFlag = false;
                    HalfCarryFlag = false;
                    return 4;

                // 8-bit loads from registers
                case 0x40: B = B; return 4; // LD B, B
                case 0x41: B = C; return 4; // LD B, C
                case 0x42: B = D; return 4;
                case 0x43: B = E; return 4;
                case 0x44: B = H; return 4;
                case 0x45: B = L; return 4;
                case 0x46: B = _mmu.ReadByte(HL); return 8;
                case 0x47: B = A; return 4;

                case 0x48: C = B; return 4;
                case 0x49: C = C; return 4;
                case 0x4A: C = D; return 4;
                case 0x4B: C = E; return 4;
                case 0x4C: C = H; return 4;
                case 0x4D: C = L; return 4;
                case 0x4E: C = _mmu.ReadByte(HL); return 8;
                case 0x4F: C = A; return 4;

                case 0x50: D = B; return 4;
                case 0x51: D = C; return 4;
                case 0x52: D = D; return 4;
                case 0x53: D = E; return 4;
                case 0x54: D = H; return 4;
                case 0x55: D = L; return 4;
                case 0x56: D = _mmu.ReadByte(HL); return 8;
                case 0x57: D = A; return 4;

                case 0x58: E = B; return 4;
                case 0x59: E = C; return 4;
                case 0x5A: E = D; return 4;
                case 0x5B: E = E; return 4;
                case 0x5C: E = H; return 4;
                case 0x5D: E = L; return 4;
                case 0x5E: E = _mmu.ReadByte(HL); return 8;
                case 0x5F: E = A; return 4;

                case 0x60: H = B; return 4;
                case 0x61: H = C; return 4;
                case 0x62: H = D; return 4;
                case 0x63: H = E; return 4;
                case 0x64: H = H; return 4;
                case 0x65: H = L; return 4;
                case 0x66: H = _mmu.ReadByte(HL); return 8;
                case 0x67: H = A; return 4;

                case 0x68: L = B; return 4;
                case 0x69: L = C; return 4;
                case 0x6A: L = D; return 4;
                case 0x6B: L = E; return 4;
                case 0x6C: L = H; return 4;
                case 0x6D: L = L; return 4;
                case 0x6E: L = _mmu.ReadByte(HL); return 8;
                case 0x6F: L = A; return 4;

                case 0x70: _mmu.WriteByte(HL, B); return 8;
                case 0x71: _mmu.WriteByte(HL, C); return 8;
                case 0x72: _mmu.WriteByte(HL, D); return 8;
                case 0x73: _mmu.WriteByte(HL, E); return 8;
                case 0x74: _mmu.WriteByte(HL, H); return 8;
                case 0x75: _mmu.WriteByte(HL, L); return 8;
                case 0x76: // HALT
                    Halt = true;
                    // Halt bug: if IME=0 and an interrupt is pending, PC not incremented?
                    // Not emulated here for simplicity.
                    return 4;
                case 0x77: _mmu.WriteByte(HL, A); return 8;

                case 0x78: A = B; return 4;
                case 0x79: A = C; return 4;
                case 0x7A: A = D; return 4;
                case 0x7B: A = E; return 4;
                case 0x7C: A = H; return 4;
                case 0x7D: A = L; return 4;
                case 0x7E: A = _mmu.ReadByte(HL); return 8;
                case 0x7F: A = A; return 4;

                // 8-bit ALU
                case 0x80: Add(B); return 4;
                case 0x81: Add(C); return 4;
                case 0x82: Add(D); return 4;
                case 0x83: Add(E); return 4;
                case 0x84: Add(H); return 4;
                case 0x85: Add(L); return 4;
                case 0x86: Add(_mmu.ReadByte(HL)); return 8;
                case 0x87: Add(A); return 4;

                case 0x88: Adc(B); return 4;
                case 0x89: Adc(C); return 4;
                case 0x8A: Adc(D); return 4;
                case 0x8B: Adc(E); return 4;
                case 0x8C: Adc(H); return 4;
                case 0x8D: Adc(L); return 4;
                case 0x8E: Adc(_mmu.ReadByte(HL)); return 8;
                case 0x8F: Adc(A); return 4;

                case 0x90: Sub(B); return 4;
                case 0x91: Sub(C); return 4;
                case 0x92: Sub(D); return 4;
                case 0x93: Sub(E); return 4;
                case 0x94: Sub(H); return 4;
                case 0x95: Sub(L); return 4;
                case 0x96: Sub(_mmu.ReadByte(HL)); return 8;
                case 0x97: Sub(A); return 4;

                case 0x98: Sbc(B); return 4;
                case 0x99: Sbc(C); return 4;
                case 0x9A: Sbc(D); return 4;
                case 0x9B: Sbc(E); return 4;
                case 0x9C: Sbc(H); return 4;
                case 0x9D: Sbc(L); return 4;
                case 0x9E: Sbc(_mmu.ReadByte(HL)); return 8;
                case 0x9F: Sbc(A); return 4;

                case 0xA0: And(B); return 4;
                case 0xA1: And(C); return 4;
                case 0xA2: And(D); return 4;
                case 0xA3: And(E); return 4;
                case 0xA4: And(H); return 4;
                case 0xA5: And(L); return 4;
                case 0xA6: And(_mmu.ReadByte(HL)); return 8;
                case 0xA7: And(A); return 4;

                case 0xA8: Xor(B); return 4;
                case 0xA9: Xor(C); return 4;
                case 0xAA: Xor(D); return 4;
                case 0xAB: Xor(E); return 4;
                case 0xAC: Xor(H); return 4;
                case 0xAD: Xor(L); return 4;
                case 0xAE: Xor(_mmu.ReadByte(HL)); return 8;
                case 0xAF: Xor(A); return 4;

                case 0xB0: Or(B); return 4;
                case 0xB1: Or(C); return 4;
                case 0xB2: Or(D); return 4;
                case 0xB3: Or(E); return 4;
                case 0xB4: Or(H); return 4;
                case 0xB5: Or(L); return 4;
                case 0xB6: Or(_mmu.ReadByte(HL)); return 8;
                case 0xB7: Or(A); return 4;

                case 0xB8: Cp(B); return 4;
                case 0xB9: Cp(C); return 4;
                case 0xBA: Cp(D); return 4;
                case 0xBB: Cp(E); return 4;
                case 0xBC: Cp(H); return 4;
                case 0xBD: Cp(L); return 4;
                case 0xBE: Cp(_mmu.ReadByte(HL)); return 8;
                case 0xBF: Cp(A); return 4;

                // 16-bit loads / misc
                case 0xC0: // RET NZ
                    if (!ZeroFlag) { PC = PopWord(); return 20; }
                    return 8;
                case 0xC1: // POP BC
                    BC = PopWord(); return 12;
                case 0xC2: // JP NZ, a16
                    if (!ZeroFlag) { PC = _mmu.ReadWord(PC); return 16; }
                    PC += 2; return 12;
                case 0xC3: // JP a16
                    PC = _mmu.ReadWord(PC); return 16;
                case 0xC4: // CALL NZ, a16
                    if (!ZeroFlag) { Call(); return 24; }
                    PC += 2; return 12;
                case 0xC5: // PUSH BC
                    PushWord(BC); return 16;
                case 0xC6: // ADD A, d8
                    Add(_mmu.ReadByte(PC++)); return 8;
                case 0xC7: // RST 00h
                    Rst(0x00); return 16;
                case 0xC8: // RET Z
                    if (ZeroFlag) { PC = PopWord(); return 20; }
                    return 8;
                case 0xC9: // RET
                    PC = PopWord(); return 16;
                case 0xCA: // JP Z, a16
                    if (ZeroFlag) { PC = _mmu.ReadWord(PC); return 16; }
                    PC += 2; return 12;
                case 0xCB: // Prefix CB
                    return ExecuteCB();
                case 0xCC: // CALL Z, a16
                    if (ZeroFlag) { Call(); return 24; }
                    PC += 2; return 12;
                case 0xCD: // CALL a16
                    Call(); return 24;
                case 0xCE: // ADC A, d8
                    Adc(_mmu.ReadByte(PC++)); return 8;
                case 0xCF: // RST 08h
                    Rst(0x08); return 16;
                case 0xD0: // RET NC
                    if (!CarryFlag) { PC = PopWord(); return 20; }
                    return 8;
                case 0xD1: // POP DE
                    DE = PopWord(); return 12;
                case 0xD2: // JP NC, a16
                    if (!CarryFlag) { PC = _mmu.ReadWord(PC); return 16; }
                    PC += 2; return 12;
                case 0xD3: // Invalid
                    return 4;
                case 0xD4: // CALL NC, a16
                    if (!CarryFlag) { Call(); return 24; }
                    PC += 2; return 12;
                case 0xD5: // PUSH DE
                    PushWord(DE); return 16;
                case 0xD6: // SUB A, d8
                    Sub(_mmu.ReadByte(PC++)); return 8;
                case 0xD7: // RST 10h
                    Rst(0x10); return 16;
                case 0xD8: // RET C
                    if (CarryFlag) { PC = PopWord(); return 20; }
                    return 8;
                case 0xD9: // RETI
                    PC = PopWord();
                    IME = true;
                    return 16;
                case 0xDA: // JP C, a16
                    if (CarryFlag) { PC = _mmu.ReadWord(PC); return 16; }
                    PC += 2; return 12;
                case 0xDB: // Invalid
                    return 4;
                case 0xDC: // CALL C, a16
                    if (CarryFlag) { Call(); return 24; }
                    PC += 2; return 12;
                case 0xDD: // Invalid
                    return 4;
                case 0xDE: // SBC A, d8
                    Sbc(_mmu.ReadByte(PC++)); return 8;
                case 0xDF: // RST 18h
                    Rst(0x18); return 16;
                case 0xE0: // LDH (a8), A
                    {
                        byte offset = _mmu.ReadByte(PC++);
                        _mmu.WriteByte((ushort)(0xFF00 + offset), A);
                    }
                    return 12;
                case 0xE1: // POP HL
                    HL = PopWord(); return 12;
                case 0xE2: // LDH (C), A
                    _mmu.WriteByte((ushort)(0xFF00 + C), A); return 8;
                case 0xE3: // Invalid
                    return 4;
                case 0xE4: // Invalid
                    return 4;
                case 0xE5: // PUSH HL
                    PushWord(HL); return 16;
                case 0xE6: // AND A, d8
                    And(_mmu.ReadByte(PC++)); return 8;
                case 0xE7: // RST 20h
                    Rst(0x20); return 16;
                case 0xE8: // ADD SP, s8
                    {
                        sbyte offset = (sbyte)_mmu.ReadByte(PC++);
                        int result = SP + offset;
                        ZeroFlag = false;
                        SubtractFlag = false;
                        HalfCarryFlag = ((SP & 0xF) + (offset & 0xF)) > 0xF;
                        CarryFlag = ((SP & 0xFF) + (offset & 0xFF)) > 0xFF;
                        SP = (ushort)result;
                    }
                    return 16;
                case 0xE9: // JP HL
                    PC = HL; return 4;
                case 0xEA: // LD (a16), A
                    {
                        ushort addr = _mmu.ReadWord(PC); PC += 2;
                        _mmu.WriteByte(addr, A);
                    }
                    return 16;
                case 0xEB: // Invalid
                    return 4;
                case 0xEC: // Invalid
                    return 4;
                case 0xED: // Invalid
                    return 4;
                case 0xEE: // XOR A, d8
                    Xor(_mmu.ReadByte(PC++)); return 8;
                case 0xEF: // RST 28h
                    Rst(0x28); return 16;
                case 0xF0: // LDH A, (a8)
                    {
                        byte offset = _mmu.ReadByte(PC++);
                        A = _mmu.ReadByte((ushort)(0xFF00 + offset));
                    }
                    return 12;
                case 0xF1: // POP AF
                    AF = PopWord(); return 12;
                case 0xF2: // LDH A, (C)
                    A = _mmu.ReadByte((ushort)(0xFF00 + C)); return 8;
                case 0xF3: // DI
                    IME = false; return 4;
                case 0xF4: // Invalid
                    return 4;
                case 0xF5: // PUSH AF
                    PushWord(AF); return 16;
                case 0xF6: // OR A, d8
                    Or(_mmu.ReadByte(PC++)); return 8;
                case 0xF7: // RST 30h
                    Rst(0x30); return 16;
                case 0xF8: // LD HL, SP + s8
                    {
                        sbyte offset = (sbyte)_mmu.ReadByte(PC++);
                        int result = SP + offset;
                        ZeroFlag = false;
                        SubtractFlag = false;
                        HalfCarryFlag = ((SP & 0xF) + (offset & 0xF)) > 0xF;
                        CarryFlag = ((SP & 0xFF) + (offset & 0xFF)) > 0xFF;
                        HL = (ushort)result;
                    }
                    return 12;
                case 0xF9: // LD SP, HL
                    SP = HL; return 8;
                case 0xFA: // LD A, (a16)
                    {
                        ushort addr = _mmu.ReadWord(PC); PC += 2;
                        A = _mmu.ReadByte(addr);
                    }
                    return 16;
                case 0xFB: // EI
                    IME = true; return 4;
                case 0xFC: // Invalid
                    return 4;
                case 0xFD: // Invalid
                    return 4;
                case 0xFE: // CP A, d8
                    Cp(_mmu.ReadByte(PC++)); return 8;
                case 0xFF: // RST 38h
                    Rst(0x38); return 16;

                default:
                    throw new InvalidOperationException($"Unknown opcode: {opcode:X2}");
            }
        }

        private int ExecuteCB()
        {
            byte cb = _mmu.ReadByte(PC++);
            switch (cb)
            {
                // RLC
                case 0x00: B = Rlc(B); return 8;
                case 0x01: C = Rlc(C); return 8;
                case 0x02: D = Rlc(D); return 8;
                case 0x03: E = Rlc(E); return 8;
                case 0x04: H = Rlc(H); return 8;
                case 0x05: L = Rlc(L); return 8;
                case 0x06: _mmu.WriteByte(HL, Rlc(_mmu.ReadByte(HL))); return 16;
                case 0x07: A = Rlc(A); return 8;
                // RRC
                case 0x08: B = Rrc(B); return 8;
                case 0x09: C = Rrc(C); return 8;
                case 0x0A: D = Rrc(D); return 8;
                case 0x0B: E = Rrc(E); return 8;
                case 0x0C: H = Rrc(H); return 8;
                case 0x0D: L = Rrc(L); return 8;
                case 0x0E: _mmu.WriteByte(HL, Rrc(_mmu.ReadByte(HL))); return 16;
                case 0x0F: A = Rrc(A); return 8;
                // RL
                case 0x10: B = Rl(B); return 8;
                case 0x11: C = Rl(C); return 8;
                case 0x12: D = Rl(D); return 8;
                case 0x13: E = Rl(E); return 8;
                case 0x14: H = Rl(H); return 8;
                case 0x15: L = Rl(L); return 8;
                case 0x16: _mmu.WriteByte(HL, Rl(_mmu.ReadByte(HL))); return 16;
                case 0x17: A = Rl(A); return 8;
                // RR
                case 0x18: B = Rr(B); return 8;
                case 0x19: C = Rr(C); return 8;
                case 0x1A: D = Rr(D); return 8;
                case 0x1B: E = Rr(E); return 8;
                case 0x1C: H = Rr(H); return 8;
                case 0x1D: L = Rr(L); return 8;
                case 0x1E: _mmu.WriteByte(HL, Rr(_mmu.ReadByte(HL))); return 16;
                case 0x1F: A = Rr(A); return 8;
                // SLA
                case 0x20: B = Sla(B); return 8;
                case 0x21: C = Sla(C); return 8;
                case 0x22: D = Sla(D); return 8;
                case 0x23: E = Sla(E); return 8;
                case 0x24: H = Sla(H); return 8;
                case 0x25: L = Sla(L); return 8;
                case 0x26: _mmu.WriteByte(HL, Sla(_mmu.ReadByte(HL))); return 16;
                case 0x27: A = Sla(A); return 8;
                // SRA
                case 0x28: B = Sra(B); return 8;
                case 0x29: C = Sra(C); return 8;
                case 0x2A: D = Sra(D); return 8;
                case 0x2B: E = Sra(E); return 8;
                case 0x2C: H = Sra(H); return 8;
                case 0x2D: L = Sra(L); return 8;
                case 0x2E: _mmu.WriteByte(HL, Sra(_mmu.ReadByte(HL))); return 16;
                case 0x2F: A = Sra(A); return 8;
                // SWAP
                case 0x30: B = Swap(B); return 8;
                case 0x31: C = Swap(C); return 8;
                case 0x32: D = Swap(D); return 8;
                case 0x33: E = Swap(E); return 8;
                case 0x34: H = Swap(H); return 8;
                case 0x35: L = Swap(L); return 8;
                case 0x36: _mmu.WriteByte(HL, Swap(_mmu.ReadByte(HL))); return 16;
                case 0x37: A = Swap(A); return 8;
                // SRL
                case 0x38: B = Srl(B); return 8;
                case 0x39: C = Srl(C); return 8;
                case 0x3A: D = Srl(D); return 8;
                case 0x3B: E = Srl(E); return 8;
                case 0x3C: H = Srl(H); return 8;
                case 0x3D: L = Srl(L); return 8;
                case 0x3E: _mmu.WriteByte(HL, Srl(_mmu.ReadByte(HL))); return 16;
                case 0x3F: A = Srl(A); return 8;
                // BIT
                case 0x40: Bit(0, B); return 8;
                case 0x41: Bit(0, C); return 8;
                case 0x42: Bit(0, D); return 8;
                case 0x43: Bit(0, E); return 8;
                case 0x44: Bit(0, H); return 8;
                case 0x45: Bit(0, L); return 8;
                case 0x46: Bit(0, _mmu.ReadByte(HL)); return 12;
                case 0x47: Bit(0, A); return 8;
                case 0x48: Bit(1, B); return 8;
                case 0x49: Bit(1, C); return 8;
                case 0x4A: Bit(1, D); return 8;
                case 0x4B: Bit(1, E); return 8;
                case 0x4C: Bit(1, H); return 8;
                case 0x4D: Bit(1, L); return 8;
                case 0x4E: Bit(1, _mmu.ReadByte(HL)); return 12;
                case 0x4F: Bit(1, A); return 8;
                case 0x50: Bit(2, B); return 8;
                case 0x51: Bit(2, C); return 8;
                case 0x52: Bit(2, D); return 8;
                case 0x53: Bit(2, E); return 8;
                case 0x54: Bit(2, H); return 8;
                case 0x55: Bit(2, L); return 8;
                case 0x56: Bit(2, _mmu.ReadByte(HL)); return 12;
                case 0x57: Bit(2, A); return 8;
                case 0x58: Bit(3, B); return 8;
                case 0x59: Bit(3, C); return 8;
                case 0x5A: Bit(3, D); return 8;
                case 0x5B: Bit(3, E); return 8;
                case 0x5C: Bit(3, H); return 8;
                case 0x5D: Bit(3, L); return 8;
                case 0x5E: Bit(3, _mmu.ReadByte(HL)); return 12;
                case 0x5F: Bit(3, A); return 8;
                case 0x60: Bit(4, B); return 8;
                case 0x61: Bit(4, C); return 8;
                case 0x62: Bit(4, D); return 8;
                case 0x63: Bit(4, E); return 8;
                case 0x64: Bit(4, H); return 8;
                case 0x65: Bit(4, L); return 8;
                case 0x66: Bit(4, _mmu.ReadByte(HL)); return 12;
                case 0x67: Bit(4, A); return 8;
                case 0x68: Bit(5, B); return 8;
                case 0x69: Bit(5, C); return 8;
                case 0x6A: Bit(5, D); return 8;
                case 0x6B: Bit(5, E); return 8;
                case 0x6C: Bit(5, H); return 8;
                case 0x6D: Bit(5, L); return 8;
                case 0x6E: Bit(5, _mmu.ReadByte(HL)); return 12;
                case 0x6F: Bit(5, A); return 8;
                case 0x70: Bit(6, B); return 8;
                case 0x71: Bit(6, C); return 8;
                case 0x72: Bit(6, D); return 8;
                case 0x73: Bit(6, E); return 8;
                case 0x74: Bit(6, H); return 8;
                case 0x75: Bit(6, L); return 8;
                case 0x76: Bit(6, _mmu.ReadByte(HL)); return 12;
                case 0x77: Bit(6, A); return 8;
                case 0x78: Bit(7, B); return 8;
                case 0x79: Bit(7, C); return 8;
                case 0x7A: Bit(7, D); return 8;
                case 0x7B: Bit(7, E); return 8;
                case 0x7C: Bit(7, H); return 8;
                case 0x7D: Bit(7, L); return 8;
                case 0x7E: Bit(7, _mmu.ReadByte(HL)); return 12;
                case 0x7F: Bit(7, A); return 8;
                // RES
                case 0x80: B = Res(0, B); return 8;
                case 0x81: C = Res(0, C); return 8;
                case 0x82: D = Res(0, D); return 8;
                case 0x83: E = Res(0, E); return 8;
                case 0x84: H = Res(0, H); return 8;
                case 0x85: L = Res(0, L); return 8;
                case 0x86: _mmu.WriteByte(HL, Res(0, _mmu.ReadByte(HL))); return 16;
                case 0x87: A = Res(0, A); return 8;
                case 0x88: B = Res(1, B); return 8;
                case 0x89: C = Res(1, C); return 8;
                case 0x8A: D = Res(1, D); return 8;
                case 0x8B: E = Res(1, E); return 8;
                case 0x8C: H = Res(1, H); return 8;
                case 0x8D: L = Res(1, L); return 8;
                case 0x8E: _mmu.WriteByte(HL, Res(1, _mmu.ReadByte(HL))); return 16;
                case 0x8F: A = Res(1, A); return 8;
                case 0x90: B = Res(2, B); return 8;
                case 0x91: C = Res(2, C); return 8;
                case 0x92: D = Res(2, D); return 8;
                case 0x93: E = Res(2, E); return 8;
                case 0x94: H = Res(2, H); return 8;
                case 0x95: L = Res(2, L); return 8;
                case 0x96: _mmu.WriteByte(HL, Res(2, _mmu.ReadByte(HL))); return 16;
                case 0x97: A = Res(2, A); return 8;
                case 0x98: B = Res(3, B); return 8;
                case 0x99: C = Res(3, C); return 8;
                case 0x9A: D = Res(3, D); return 8;
                case 0x9B: E = Res(3, E); return 8;
                case 0x9C: H = Res(3, H); return 8;
                case 0x9D: L = Res(3, L); return 8;
                case 0x9E: _mmu.WriteByte(HL, Res(3, _mmu.ReadByte(HL))); return 16;
                case 0x9F: A = Res(3, A); return 8;
                case 0xA0: B = Res(4, B); return 8;
                case 0xA1: C = Res(4, C); return 8;
                case 0xA2: D = Res(4, D); return 8;
                case 0xA3: E = Res(4, E); return 8;
                case 0xA4: H = Res(4, H); return 8;
                case 0xA5: L = Res(4, L); return 8;
                case 0xA6: _mmu.WriteByte(HL, Res(4, _mmu.ReadByte(HL))); return 16;
                case 0xA7: A = Res(4, A); return 8;
                case 0xA8: B = Res(5, B); return 8;
                case 0xA9: C = Res(5, C); return 8;
                case 0xAA: D = Res(5, D); return 8;
                case 0xAB: E = Res(5, E); return 8;
                case 0xAC: H = Res(5, H); return 8;
                case 0xAD: L = Res(5, L); return 8;
                case 0xAE: _mmu.WriteByte(HL, Res(5, _mmu.ReadByte(HL))); return 16;
                case 0xAF: A = Res(5, A); return 8;
                case 0xB0: B = Res(6, B); return 8;
                case 0xB1: C = Res(6, C); return 8;
                case 0xB2: D = Res(6, D); return 8;
                case 0xB3: E = Res(6, E); return 8;
                case 0xB4: H = Res(6, H); return 8;
                case 0xB5: L = Res(6, L); return 8;
                case 0xB6: _mmu.WriteByte(HL, Res(6, _mmu.ReadByte(HL))); return 16;
                case 0xB7: A = Res(6, A); return 8;
                case 0xB8: B = Res(7, B); return 8;
                case 0xB9: C = Res(7, C); return 8;
                case 0xBA: D = Res(7, D); return 8;
                case 0xBB: E = Res(7, E); return 8;
                case 0xBC: H = Res(7, H); return 8;
                case 0xBD: L = Res(7, L); return 8;
                case 0xBE: _mmu.WriteByte(HL, Res(7, _mmu.ReadByte(HL))); return 16;
                case 0xBF: A = Res(7, A); return 8;
                // SET
                case 0xC0: B = Set(0, B); return 8;
                case 0xC1: C = Set(0, C); return 8;
                case 0xC2: D = Set(0, D); return 8;
                case 0xC3: E = Set(0, E); return 8;
                case 0xC4: H = Set(0, H); return 8;
                case 0xC5: L = Set(0, L); return 8;
                case 0xC6: _mmu.WriteByte(HL, Set(0, _mmu.ReadByte(HL))); return 16;
                case 0xC7: A = Set(0, A); return 8;
                case 0xC8: B = Set(1, B); return 8;
                case 0xC9: C = Set(1, C); return 8;
                case 0xCA: D = Set(1, D); return 8;
                case 0xCB: E = Set(1, E); return 8;
                case 0xCC: H = Set(1, H); return 8;
                case 0xCD: L = Set(1, L); return 8;
                case 0xCE: _mmu.WriteByte(HL, Set(1, _mmu.ReadByte(HL))); return 16;
                case 0xCF: A = Set(1, A); return 8;
                case 0xD0: B = Set(2, B); return 8;
                case 0xD1: C = Set(2, C); return 8;
                case 0xD2: D = Set(2, D); return 8;
                case 0xD3: E = Set(2, E); return 8;
                case 0xD4: H = Set(2, H); return 8;
                case 0xD5: L = Set(2, L); return 8;
                case 0xD6: _mmu.WriteByte(HL, Set(2, _mmu.ReadByte(HL))); return 16;
                case 0xD7: A = Set(2, A); return 8;
                case 0xD8: B = Set(3, B); return 8;
                case 0xD9: C = Set(3, C); return 8;
                case 0xDA: D = Set(3, D); return 8;
                case 0xDB: E = Set(3, E); return 8;
                case 0xDC: H = Set(3, H); return 8;
                case 0xDD: L = Set(3, L); return 8;
                case 0xDE: _mmu.WriteByte(HL, Set(3, _mmu.ReadByte(HL))); return 16;
                case 0xDF: A = Set(3, A); return 8;
                case 0xE0: B = Set(4, B); return 8;
                case 0xE1: C = Set(4, C); return 8;
                case 0xE2: D = Set(4, D); return 8;
                case 0xE3: E = Set(4, E); return 8;
                case 0xE4: H = Set(4, H); return 8;
                case 0xE5: L = Set(4, L); return 8;
                case 0xE6: _mmu.WriteByte(HL, Set(4, _mmu.ReadByte(HL))); return 16;
                case 0xE7: A = Set(4, A); return 8;
                case 0xE8: B = Set(5, B); return 8;
                case 0xE9: C = Set(5, C); return 8;
                case 0xEA: D = Set(5, D); return 8;
                case 0xEB: E = Set(5, E); return 8;
                case 0xEC: H = Set(5, H); return 8;
                case 0xED: L = Set(5, L); return 8;
                case 0xEE: _mmu.WriteByte(HL, Set(5, _mmu.ReadByte(HL))); return 16;
                case 0xEF: A = Set(5, A); return 8;
                case 0xF0: B = Set(6, B); return 8;
                case 0xF1: C = Set(6, C); return 8;
                case 0xF2: D = Set(6, D); return 8;
                case 0xF3: E = Set(6, E); return 8;
                case 0xF4: H = Set(6, H); return 8;
                case 0xF5: L = Set(6, L); return 8;
                case 0xF6: _mmu.WriteByte(HL, Set(6, _mmu.ReadByte(HL))); return 16;
                case 0xF7: A = Set(6, A); return 8;
                case 0xF8: B = Set(7, B); return 8;
                case 0xF9: C = Set(7, C); return 8;
                case 0xFA: D = Set(7, D); return 8;
                case 0xFB: E = Set(7, E); return 8;
                case 0xFC: H = Set(7, H); return 8;
                case 0xFD: L = Set(7, L); return 8;
                case 0xFE: _mmu.WriteByte(HL, Set(7, _mmu.ReadByte(HL))); return 16;
                case 0xFF: A = Set(7, A); return 8;
            }
        }

        // ---- Helper Methods ----

        private byte Inc(byte val)
        {
            byte res = (byte)(val + 1);
            ZeroFlag = res == 0;
            SubtractFlag = false;
            HalfCarryFlag = (val & 0x0F) == 0x0F;
            return res;
        }

        private byte Dec(byte val)
        {
            byte res = (byte)(val - 1);
            ZeroFlag = res == 0;
            SubtractFlag = true;
            HalfCarryFlag = (val & 0x0F) == 0x00;
            return res;
        }

        private void Add(byte val)
        {
            int result = A + val;
            CarryFlag = result > 0xFF;
            HalfCarryFlag = ((A & 0x0F) + (val & 0x0F)) > 0x0F;
            A = (byte)result;
            ZeroFlag = A == 0;
            SubtractFlag = false;
        }

        private void Adc(byte val)
        {
            int carry = CarryFlag ? 1 : 0;
            int result = A + val + carry;
            CarryFlag = result > 0xFF;
            HalfCarryFlag = ((A & 0x0F) + (val & 0x0F) + carry) > 0x0F;
            A = (byte)result;
            ZeroFlag = A == 0;
            SubtractFlag = false;
        }

        private void Sub(byte val)
        {
            int result = A - val;
            CarryFlag = result < 0;
            HalfCarryFlag = ((A & 0x0F) - (val & 0x0F)) < 0;
            A = (byte)result;
            ZeroFlag = A == 0;
            SubtractFlag = true;
        }

        private void Sbc(byte val)
        {
            int carry = CarryFlag ? 1 : 0;
            int result = A - val - carry;
            CarryFlag = result < 0;
            HalfCarryFlag = ((A & 0x0F) - (val & 0x0F) - carry) < 0;
            A = (byte)result;
            ZeroFlag = A == 0;
            SubtractFlag = true;
        }

        private void And(byte val)
        {
            A &= val;
            ZeroFlag = A == 0;
            SubtractFlag = false;
            HalfCarryFlag = true;
            CarryFlag = false;
        }

        private void Or(byte val)
        {
            A |= val;
            ZeroFlag = A == 0;
            SubtractFlag = false;
            HalfCarryFlag = false;
            CarryFlag = false;
        }

        private void Xor(byte val)
        {
            A ^= val;
            ZeroFlag = A == 0;
            SubtractFlag = false;
            HalfCarryFlag = false;
            CarryFlag = false;
        }

        private void Cp(byte val)
        {
            int result = A - val;
            ZeroFlag = result == 0;
            SubtractFlag = true;
            HalfCarryFlag = ((A & 0x0F) - (val & 0x0F)) < 0;
            CarryFlag = result < 0;
        }

        private void AddHL(ushort val)
        {
            uint result = (uint)(HL + val);
            CarryFlag = (result & 0x10000) != 0;
            HalfCarryFlag = ((HL & 0xFFF) + (val & 0xFFF)) > 0xFFF;
            HL = (ushort)result;
            SubtractFlag = false;
        }

        private void RLCA()
        {
            CarryFlag = (A & 0x80) != 0;
            A = (byte)((A << 1) | (A >> 7));
            ZeroFlag = false;
            SubtractFlag = false;
            HalfCarryFlag = false;
        }

        private void RLA()
        {
            bool oldCarry = CarryFlag;
            CarryFlag = (A & 0x80) != 0;
            A = (byte)((A << 1) | (oldCarry ? 1 : 0));
            ZeroFlag = false;
            SubtractFlag = false;
            HalfCarryFlag = false;
        }

        private void RRCA()
        {
            CarryFlag = (A & 0x01) != 0;
            A = (byte)((A >> 1) | ((A & 0x01) << 7));
            ZeroFlag = false;
            SubtractFlag = false;
            HalfCarryFlag = false;
        }

        private void RRA()
        {
            bool oldCarry = CarryFlag;
            CarryFlag = (A & 0x01) != 0;
            A = (byte)((A >> 1) | (oldCarry ? 0x80 : 0));
            ZeroFlag = false;
            SubtractFlag = false;
            HalfCarryFlag = false;
        }

        private void DAA()
        {
            int a = A;
            if (!SubtractFlag)
            {
                if (HalfCarryFlag || (a & 0x0F) > 9)
                    a += 0x06;
                if (CarryFlag || a > 0x9F)
                    a += 0x60;
            }
            else
            {
                if (HalfCarryFlag)
                    a = (a - 6) & 0xFF;
                if (CarryFlag)
                    a -= 0x60;
            }
            CarryFlag = CarryFlag || (a > 0xFF);
            A = (byte)(a & 0xFF);
            ZeroFlag = A == 0;
            HalfCarryFlag = false;
        }

        private byte Rlc(byte val)
        {
            CarryFlag = (val & 0x80) != 0;
            val = (byte)((val << 1) | (val >> 7));
            ZeroFlag = val == 0;
            SubtractFlag = false;
            HalfCarryFlag = false;
            return val;
        }

        private byte Rrc(byte val)
        {
            CarryFlag = (val & 0x01) != 0;
            val = (byte)((val >> 1) | ((val & 0x01) << 7));
            ZeroFlag = val == 0;
            SubtractFlag = false;
            HalfCarryFlag = false;
            return val;
        }

        private byte Rl(byte val)
        {
            bool oldCarry = CarryFlag;
            CarryFlag = (val & 0x80) != 0;
            val = (byte)((val << 1) | (oldCarry ? 1 : 0));
            ZeroFlag = val == 0;
            SubtractFlag = false;
            HalfCarryFlag = false;
            return val;
        }

        private byte Rr(byte val)
        {
            bool oldCarry = CarryFlag;
            CarryFlag = (val & 0x01) != 0;
            val = (byte)((val >> 1) | (oldCarry ? 0x80 : 0));
            ZeroFlag = val == 0;
            SubtractFlag = false;
            HalfCarryFlag = false;
            return val;
        }

        private byte Sla(byte val)
        {
            CarryFlag = (val & 0x80) != 0;
            val <<= 1;
            ZeroFlag = val == 0;
            SubtractFlag = false;
            HalfCarryFlag = false;
            return val;
        }

        private byte Sra(byte val)
        {
            CarryFlag = (val & 0x01) != 0;
            val = (byte)((val >> 1) | (val & 0x80));
            ZeroFlag = val == 0;
            SubtractFlag = false;
            HalfCarryFlag = false;
            return val;
        }

        private byte Swap(byte val)
        {
            val = (byte)((val << 4) | (val >> 4));
            ZeroFlag = val == 0;
            SubtractFlag = false;
            HalfCarryFlag = false;
            CarryFlag = false;
            return val;
        }

        private byte Srl(byte val)
        {
            CarryFlag = (val & 0x01) != 0;
            val >>= 1;
            ZeroFlag = val == 0;
            SubtractFlag = false;
            HalfCarryFlag = false;
            return val;
        }

        private void Bit(int bit, byte val)
        {
            ZeroFlag = (val & (1 << bit)) == 0;
            SubtractFlag = false;
            HalfCarryFlag = true;
        }

        private byte Res(int bit, byte val)
        {
            return (byte)(val & ~(1 << bit));
        }

        private byte Set(int bit, byte val)
        {
            return (byte)(val | (1 << bit));
        }

        private void Call()
        {
            ushort addr = _mmu.ReadWord(PC);
            PC += 2;
            _mmu.WriteByte(--SP, (byte)(PC >> 8));
            _mmu.WriteByte(--SP, (byte)(PC & 0xFF));
            PC = addr;
        }

        private void Rst(byte vec)
        {
            _mmu.WriteByte(--SP, (byte)(PC >> 8));
            _mmu.WriteByte(--SP, (byte)(PC & 0xFF));
            PC = (ushort)(0x0000 + vec);
        }

        private ushort PopWord()
        {
            ushort lo = _mmu.ReadByte(SP++);
            ushort hi = _mmu.ReadByte(SP++);
            return (ushort)((hi << 8) | lo);
        }

        private void PushWord(ushort val)
        {
            _mmu.WriteByte(--SP, (byte)(val >> 8));
            _mmu.WriteByte(--SP, (byte)(val & 0xFF));
        }
    }
}