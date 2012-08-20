/* Copyright 2012 Brian Todoroff
 * Encephal16 by Brian Todoroff is licensed under a Creative Commons Attribution-ShareAlike 3.0 Unported License
 * Based on a work at https://github.com/btodoroff/Encephal16.
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;


namespace e16
{
    public interface Ie16Hardware
    {
        uint HardwareID { get;}
        uint Manufacturer { get;}
        ushort HardwareVersion { get;}
        e16vm dcpu16 { get; set; }
        void Interrupt(ushort a);
        void Tick();
        void Reset();
        
    }

    public class operand
    {
        public operand() : this(0,0)
        {
        }
        public operand(int type, ushort value)
        {
            _type = type;
            _value = value;
            
        }
        public ushort _value;
        public int _type;
        public const int REG = 0;
        public const int PC = 1;
        public const int SP = 2;
        public const int EX = 3;
        public const int RAM = 4;
        public const int LIT = 5;
        public const int STK = 6;
    }

    public class e16vm
    {
        private ushort[] _RAM;
        private const uint RAMSize = 0x10000u;
        private ushort[] _Register;
        private const uint RegisterCount = 8;
        private const string RegisterOrder = "ABCXYZIJ";
        private const int _A = 0;
        private const int _B = 1;
        private const int _C = 2;
        private const int _X = 3;
        private const int _Y = 4;
        private const int _Z = 5;
        private const int _I = 6;
        private const int _J = 7;
        private ushort _PC;
        private ushort _SP;
        private ushort _EX;
        private ushort _IA;
        private uint _Cycles;
        private bool _IntEnabled;
        private System.Collections.Generic.Queue<ushort> _IntQueue;
        private int _CycleDebt;
        private System.Collections.Generic.Dictionary<ushort, Ie16Hardware> _Hardware;

        public uint Cycles { get { return _Cycles; } }

        public e16vm()
        {
            _RAM = new ushort[RAMSize];
            _Register = new ushort[RegisterCount];
            _IntQueue = new Queue<ushort>();
            _Hardware = new Dictionary<ushort, Ie16Hardware>();
            ClearMemory();
            Reset();

        }

        public void AttachHardware(Ie16Hardware hw, ushort address)
        {
            _Hardware.Add(address, hw);
            hw.dcpu16 = this;
            hw.Reset();
        }

        public string RegToString()
        {
            StringBuilder output = new StringBuilder();
            for (int i = 0; i < RegisterCount; i++)
            {
                output.Append(RegisterOrder[i] + ":  " + _Register[i].ToString("X4") + " ");
            }
            output.AppendLine();
            output.Append("PC: " + _PC.ToString("X4") + " ");
            output.Append("SP: " + _SP.ToString("X4") + " ");
            output.Append("EX: " + _EX.ToString("X4"));
            return output.ToString();
        }

        public string MemToString(ushort startAddr, ushort endAddr)
        {
            ushort currBlock = 0;
            ushort firstAddr = (ushort)(startAddr & (ushort)0xfff8u);
            ushort lastAddr = (ushort)(endAddr | (ushort)0x0003u);
            StringBuilder output = new StringBuilder();
            while (firstAddr + (currBlock * 8) < lastAddr)
            {
                output.Append(((firstAddr + (currBlock *8)).ToString("X4"))+":");
                for (ushort i = 0; i < 8; i++)
                {
                    output.Append(" " + _RAM[firstAddr + i + (currBlock * 8)].ToString("X4"));
                }
                output.Append("\n");
                currBlock++;
            }
            return output.ToString();
        }

        public override string ToString()
        {
            StringBuilder output = new StringBuilder();
            output.AppendLine(RegToString());
            output.Append(MemToString(_PC,(ushort)(_PC+0x0040u)));
            return output.ToString();
        }
        
        public ushort A { get { return _Register[_A]; } }
        public ushort B { get { return _Register[_B]; } }
        public ushort C { get { return _Register[_C]; } }
        public ushort X { get { return _Register[_X]; } }
        public ushort Y { get { return _Register[_Y]; } }
        public ushort Z { get { return _Register[_Z]; } }
        public ushort I { get { return _Register[_I]; } }
        public ushort J { get { return _Register[_J]; } }
        public ushort PC { get { return _PC; } }
        public ushort SP { get { return _SP; } }
        public ushort EX { get { return _EX; } }
        public ushort IA { get { return _IA; } }
        public ushort RAM(uint addr) {return _RAM[addr];}

        public void Reset()
        {
            for(int i=0; i<RegisterCount; _Register[i++]=0);
            _PC = 0;
            _SP = 0;
            _EX = 0;
            _IA = 0;
            _Cycles = 0;
            _CycleDebt = 0;
            _IntEnabled = true;
            _IntQueue.Clear();
            _state = ProcessorState.newInst;
            ClearMemory();
            foreach (Ie16Hardware hw in _Hardware.Values)
            {
                hw.Reset();
            }
        }

        public void ClearMemory()
        {
            for(int i=0; i<RAMSize; _RAM[i++]=0);
        }

        public void LoadMemory(ushort[] data, uint startAddr = 0)
        {
            for (int i = 0; i < data.Length; _RAM[startAddr + i] = data[i++]) ;
        }

        public void LoadMemory(string pathName, uint startAddr = 0)
        {
            byte[] filebytes = File.ReadAllBytes(pathName);
            ushort[] data = new ushort[(filebytes.Length + 1) >> 1];
            for (int i = 0; i < filebytes.Length; i++ )
            {
                if (i % 2 == 0)
                {
                    data[i >> 1] = (ushort)(filebytes[i] << 8);
                }
                else
                {
                    data[i >> 1] += (ushort)(filebytes[i]);
                }
            }
            LoadMemory(data, startAddr);
        }
        public void Step(int instructions=1)
        {
            for (; instructions > 0; )
            {
                Tick();
                if (_state == ProcessorState.newInst) instructions--;
            }
        }
        public void Tick(int cycles)
        {
            for (; cycles > 0; cycles--)
            {
                Tick();
            }
        }
        private enum ProcessorState { newInst, readOpA, readOpB, executeInst };
        private ProcessorState _state;
        private ushort Tick_inst;
        private ushort Tick_opcode;
        private ushort Tick_b;
        private ushort Tick_a;
        private operand Tick_opA;
        private operand Tick_opB;
        public void Tick()
        {
            _Cycles++;
            foreach (Ie16Hardware hw in _Hardware.Values)
            {
                hw.Tick();
            }
            if (--_CycleDebt == 0)
            {
                _CycleDebt--;
                return;
            }
            if (_IntEnabled && _IntQueue.Count > 0)
            {
                stackPUSH(_PC);
                stackPUSH(_A);
                _PC = _IA;
                _Register[_A] = _IntQueue.Dequeue();
                _IntEnabled = false;
            }
            if(_state == ProcessorState.newInst)
            {
                Tick_inst = nextWord();
                Tick_opcode = (ushort)(Tick_inst & (ushort)0x001fu);
                Tick_b = (ushort)((Tick_inst & (ushort)0x03e0u) >> 5);
                Tick_a = (ushort)((Tick_inst & (ushort)0xfc00u) >> 10);
                _state = ProcessorState.readOpA;
                _CycleDebt = operandCycles(Tick_a);
                if (_CycleDebt > 0) return;
            }
            if(_state == ProcessorState.readOpA)
            {
                Tick_opA = parseOperand(Tick_a);
                if (Tick_opcode == 0) // Non-basic opcodes
                {
                    _state = ProcessorState.executeInst;
                }
                else
                {
                    _CycleDebt = operandCycles(Tick_b);
                    _state = ProcessorState.readOpB;
                }
                if(_CycleDebt > 0) return;
            }
            if(_state == ProcessorState.readOpB)
            {
                Tick_opB = parseOperand(Tick_b);
                _state = ProcessorState.executeInst;
                _CycleDebt = opcodeCycles(Tick_a, Tick_opcode);
                if(_CycleDebt > 0) return;
            }
            if(_state == ProcessorState.executeInst)
            {
                if (Tick_opcode == 0) // Non-basic opcodes
                {
                    switch (Tick_b)
                    {
                        case 0x01:
                            opJSR(Tick_opA);
                            break;
                        case 0x08:
                            opINT(Tick_opA);
                            break;
                        case 0x09:
                            opIAG(Tick_opA);
                            break;
                        case 0x0a:
                            opIAS(Tick_opA);
                            break;
                        case 0x0b:
                            opRFI(Tick_opA);
                            break;
                        case 0x0c:
                            opIAQ(Tick_opA);
                            break;
                        case 0x10:
                            opHWN(Tick_opA);
                            break;
                        case 0x11:
                            opHWQ(Tick_opA);
                            break;
                        case 0x12:
                            opHWI(Tick_opA);
                            break;
                    }
                }
                else // Basic opcodes
                {
                    switch (Tick_opcode)
                    {
                        case 0x01:
                            opSET(Tick_opB, Tick_opA);
                            break;
                        case 0x02:
                            opADD(Tick_opB, Tick_opA);
                            break;
                        case 0x03:
                            opSUB(Tick_opB, Tick_opA);
                            break;
                        case 0x04:
                            opMUL(Tick_opB, Tick_opA);
                            break;
                        case 0x05:
                            opMLI(Tick_opB, Tick_opA);
                            break;
                        case 0x06:
                            opDIV(Tick_opB, Tick_opA);
                            break;
                        case 0x07:
                            opDVI(Tick_opB, Tick_opA);
                            break;
                        case 0x08:
                            opMOD(Tick_opB, Tick_opA);
                            break;
                        case 0x09:
                            opMDI(Tick_opB, Tick_opA);
                            break;
                        case 0x0a:
                            opAND(Tick_opB, Tick_opA);
                            break;
                        case 0x0b:
                            opBOR(Tick_opB, Tick_opA);
                            break;
                        case 0x0c:
                            opXOR(Tick_opB, Tick_opA);
                            break;
                        case 0x0d:
                            opSHR(Tick_opB, Tick_opA);
                            break;
                        case 0x0e:
                            opASR(Tick_opB, Tick_opA);
                            break;
                        case 0x0f:
                            opSHL(Tick_opB, Tick_opA);
                            break;
                        case 0x10:
                            opIFB(Tick_opB, Tick_opA);
                            break;
                        case 0x11:
                            opIFC(Tick_opB, Tick_opA);
                            break;
                        case 0x12:
                            opIFE(Tick_opB, Tick_opA);
                            break;
                        case 0x13:
                            opIFN(Tick_opB, Tick_opA);
                            break;
                        case 0x14:
                            opIFG(Tick_opB, Tick_opA);
                            break;
                        case 0x15:
                            opIFA(Tick_opB, Tick_opA);
                            break;
                        case 0x16:
                            opIFL(Tick_opB, Tick_opA);
                            break;
                        case 0x17:
                            opIFU(Tick_opB, Tick_opA);
                            break;
                        case 0x1a:
                            opADX(Tick_opB, Tick_opA);
                            break;
                        case 0x1b:
                            opSBX(Tick_opB, Tick_opA);
                            break;
                        case 0x1e:
                            opSTI(Tick_opB, Tick_opA);
                            break;
                        case 0x1f:
                            opSTD(Tick_opB, Tick_opA);
                            break;
                    }
                }
                _state = ProcessorState.newInst;
                return;
            }
        }

        public void skipNext(bool chain = true)
        {
            ushort inst = nextWord();
            ushort opcode = (ushort)(inst & (ushort)0x001fu);
            ushort a = (ushort)((inst & (ushort)0x03e0u) >> 5);
            ushort b = (ushort)((inst & (ushort)0xfc00u) >> 10);
            if (opcode == 0) // Non-basic opcodes
            {
                parseSkippedOperand(b);
            }
            else // Basic opcodes
            {
                parseSkippedOperand(a);
                parseSkippedOperand(b);
                if (chain && opcode > 0x0f && opcode < 0x18) skipNext(false);
            }

        }

        private ushort nextWord() { return _RAM[_PC++]; }

        private int operandCycles(ushort value)
        {
            if (value > 0x09 && value < 0x18) //[next word + register]
                return 1;
            switch (value)
            {
                case 0x1a: //PICK n
                    return 1;
                case 0x1e: //[next word]
                    return 1;
                case 0x1f: //next word (literal)
                    return 1;
            }
            return 0;
        }

        private static int[] _basicCycleCost = {
                                                   0,
                                                   1,
                                                   2,
                                                   2,
                                                   2,
                                                   2,
                                                   3,
                                                   3,
                                                   3,
                                                   3,
                                                   1,
                                                   1,
                                                   1,
                                                   1,
                                                   1,
                                                   1,
                                                   2,
                                                   2,
                                                   2,
                                                   2,
                                                   2,
                                                   2,
                                                   2,
                                                   2,
                                                   0,
                                                   0,
                                                   3,
                                                   3,
                                                   0,
                                                   0,
                                                   2,
                                                   2 };
        private static int[] _specialCycleCost = {
                                                   0,
                                                   3,
                                                   0,
                                                   0,
                                                   0,
                                                   0,
                                                   0,
                                                   0,
                                                   4,
                                                   1,
                                                   1,
                                                   3,
                                                   2,
                                                   0,
                                                   0,
                                                   0,
                                                   2,
                                                   4,
                                                   4,
                                                   0,
                                                   0,
                                                   0,
                                                   0,
                                                   0,
                                                   0,
                                                   0,
                                                   0,
                                                   0,
                                                   0,
                                                   0,
                                                   0,
                                                   0 };


        private int opcodeCycles(ushort special, ushort opcode)
        {
            if (opcode == 0)
                return _specialCycleCost[special];
            else
                return _basicCycleCost[opcode];
        }

        private operand parseOperand(ushort value)
        {
            if (value < 0x08) //Register
                return new operand(operand.REG, value);
            if (value < 0x10) //[Register]
                return new operand(operand.RAM, _Register[value - 0x08]);
            if (value < 0x18) //[next word + register]
                return new operand(operand.RAM, (ushort)(_Register[value - 0x10] + nextWord()));
            if (value > 0x1f) // literal 0x00 - 0x1f
                return new operand(operand.LIT, (ushort)(value - 0x21));
            switch (value)
            {
                case 0x18: //PUSH
                    return new operand(operand.STK, 0);
                case 0x19: //PEEK
                    return new operand(operand.RAM, _SP);
                case 0x1a: //PICK n
                    return new operand(operand.RAM, (ushort)(_SP + nextWord()));
                case 0x1b: //SP
                    return new operand(operand.SP, 0);
                case 0x1c: //PC
                    return new operand(operand.PC, 0);
                case 0x1d: //EX
                    return new operand(operand.EX, 0);
                case 0x1e: //[next word]
                    return new operand(operand.RAM, nextWord());
                case 0x1f: //next word (literal)
                    return new operand(operand.LIT, nextWord());
            }
            throw new Exception("Invalid parseOperand" + value.ToString("X4"));
        }

        private void parseSkippedOperand(ushort value)
        {
            if (value < 0x08) //Register
                return;
            if (value < 0x10) //[Register]
                return;
            if (value < 0x18) { //[next word + register]
                nextWord();
                return;
            }
            if (value > 0x1f) // literal 0x00 - 0x1f
                return;
            switch (value)
            {
                case 0x18: //PUSH
                    return;
                case 0x19: //PEEK
                    return;
                case 0x1a: //PICK n
                    nextWord();
                    return;
                case 0x1b: //SP
                    return;
                case 0x1c: //PC
                    return;
                case 0x1d: //O
                    return;
                case 0x1e: //[next word]
                    nextWord();
                    return;
                case 0x1f: //next word (literal)
                    nextWord();
                    return;
            }
            throw new Exception("Invalid parseSkippedOperand" + value.ToString("X4"));
        }
        private ushort readValue(operand op)
        {
            if (op._type == operand.REG)
                return _Register[op._value];
            if (op._type == operand.RAM)
                return _RAM[op._value];
            if (op._type == operand.LIT)
                return op._value;
            if (op._type == operand.STK)
                return stackPOP();
            switch (op._type)
            {
                case operand.PC:
                    return _PC;
                case operand.SP:
                    return _SP;
                case operand.EX:
                    return _EX;
            }
            throw new Exception("Invalid op._type: " + op._type.ToString());
        }

        private void writeValue(operand op, ushort data)
        {
            if (op._type == operand.REG)
            {
                _Register[op._value] = data;
                return;
            }
            if (op._type == operand.RAM)
            {
                _RAM[op._value] = data;
                return;
            }
            if (op._type == operand.LIT)
            {
                _RAM[op._value] = data;
                return;
            }
            if (op._type == operand.STK)
            {
                stackPUSH(data);
                return;
            }
            switch (op._type)
            {
                case operand.PC:
                    _PC = data;
                    return;
                case operand.SP:
                    _SP = data;
                    return;
                case operand.EX:
                    _EX = data;
                    return;
            }
            throw new Exception("Invalid op._type: " + op._type.ToString());
        }

        private void opSET(operand b, operand a)
        {
            ushort _a = readValue(a);
            ushort _b = readValue(b);
            writeValue(b, _a);
        }

        private void opADD(operand b, operand a)
        {
            ushort _a = readValue(a);
            ushort _b = readValue(b);
            writeValue(b, (ushort)(_b +_a));
            if ((_b + _a) > 0xffff) _EX = (ushort)0x0001u;
        }

        private void opSUB(operand b, operand a)
        {
            ushort _a = readValue(a);
            ushort _b = readValue(b);
            writeValue(b, (ushort)(_b - _a));
            if ((_b - _a) < 0) _EX = (ushort)0xffffu;
        }

        private void opMUL(operand b, operand a)
        {
            ushort _a = readValue(a);
            ushort _b = readValue(b);
            writeValue(b, (ushort)(_b * _a));
            _EX = (ushort)(((_b*_a)>>16)&0xffff);
        }

        private void opMLI(operand b, operand a)
        {
            short _a = (short)readValue(a);
            short _b = (short)readValue(b);
            writeValue(b, (ushort)(_b * _a));
            _EX = (ushort)(((_b * _a) >> 16) & 0xffff);
        }

        private void opDIV(operand b, operand a)
        {
            ushort _a = readValue(a);
            ushort _b = readValue(b);
            if (_a == 0)
            {
                writeValue(b, 0);
            }
            else
            {
                writeValue(b, (ushort)(_b / _a));
                _EX = (ushort)(((_b << 16) / _a) & 0xffff);
            }
        }

        private void opDVI(operand b, operand a)
        {
            short _a = (short)readValue(a);
            short _b = (short)readValue(b);
            if (_a == 0)
            {
                writeValue(b, 0);
            }
            else
            {
                writeValue(b, (ushort)(_b / _a));
                _EX = (ushort)(((_b << 16) / _a) & 0xffff);
            }
        }

        private void opMOD(operand b, operand a)
        {
            ushort _a = readValue(a);
            ushort _b = readValue(b);
            if (_a == 0)
            {
                writeValue(b, 0);
            }
            else
            {
                writeValue(b, (ushort)(_a % _b));
            }
        }

        private void opMDI(operand b, operand a)
        {
            short _a = (short)readValue(a);
            short _b = (short)readValue(b);
            if (_a == 0)
            {
                writeValue(b, 0);
            }
            else
            {
                writeValue(b, (ushort)(_a % _b));
            }
        }



        private void opAND(operand b, operand a)
        {
            ushort _a = readValue(a);
            ushort _b = readValue(b);
            writeValue(b,(ushort)(_b & _a));
        }

        private void opBOR(operand b, operand a)
        {
            ushort _a = readValue(a);
            ushort _b = readValue(b);
            writeValue(b,(ushort)(_b | _a));
        }

        private void opXOR(operand b, operand a)
        {
            ushort _a = readValue(a);
            ushort _b = readValue(b);
            writeValue(b,(ushort)(_b ^ _a));
        }

        private void opSHR(operand b, operand a)
        {
            ushort _a = readValue(a);
            ushort _b = readValue(b);
            writeValue(b, (ushort)(_b >> _a));
            _EX = (ushort)(((_b << 16) >> _a) & 0xffff);
        }

        private void opASR(operand b, operand a)
        {
            short _a = (short)readValue(a);
            short _b = (short)readValue(b);
            writeValue(b, (ushort)(_b >> _a));
            _EX = (ushort)(((_b << 16) >> _a) & 0xffff);
        }

        private void opSHL(operand b, operand a)
        {
            ushort _a = readValue(a);
            ushort _b = readValue(b);
            writeValue(b,(ushort)(_b << _a));
            _EX = (ushort)(((_b<<_a)>>16)&0xffff);
        }

        private void opIFB(operand b, operand a)
        {
            ushort _a = readValue(a);
            ushort _b = readValue(b);
            if (!((_b & _a) != 0)) skipNext();
        }

        private void opIFC(operand b, operand a)
        {
            ushort _a = readValue(a);
            ushort _b = readValue(b);
            if (!((_b & _a) == 0)) skipNext();
        }

        private void opIFE(operand b, operand a)
        {
            ushort _a = readValue(a);
            ushort _b = readValue(b);
            if (!(_b == _a)) skipNext();
        }

        private void opIFN(operand b, operand a)
        {
            ushort _a = readValue(a);
            ushort _b = readValue(b);
            if (!(_b != _a)) skipNext();
        }

        private void opIFG(operand b, operand a)
        {
            ushort _a = readValue(a);
            ushort _b = readValue(b);
            if (!(_b > _a)) skipNext();
        }

        private void opIFA(operand b, operand a)
        {
            short _a = (short)readValue(a);
            short _b = (short)readValue(b);
            if (!(_b > _a)) skipNext();
        }

        private void opIFL(operand b, operand a)
        {
            ushort _a = readValue(a);
            ushort _b = readValue(b);
            if (!(_b < _a)) skipNext();
        }

        private void opIFU(operand b, operand a)
        {
            short _a = (short)readValue(a);
            short _b = (short)readValue(b);
            if (!(_b < _a)) skipNext();
        }

        private void opADX(operand b, operand a)
        {
            ushort _a = readValue(a);
            ushort _b = readValue(b);
            uint v = (uint)(_b + _a + _EX);
            writeValue(b, (ushort)(v & 0xffffu));
            if (v > 0x0000ffffu) _EX = (ushort)0x0001u;
        }

        private void opSBX(operand b, operand a)
        {
            ushort _a = readValue(a);
            ushort _b = readValue(b);
            uint v = (uint)(_b - _a + _EX);
            writeValue(b, (ushort)(v & 0xffffu));
            if (v > 0x0000ffffu) _EX = (ushort)0xffffu;
        }

        private void opSTI(operand b, operand a)
        {
            ushort _a = readValue(a);
            ushort _b = readValue(b);
            uint v = (uint)(_b - _a + _EX);
            writeValue(b, _a);
            _Register[_I] = (ushort)(_Register[_I] + 0x0001u);
            _Register[_J] = (ushort)(_Register[_J] + 0x0001u);
        }

        private void opSTD(operand b, operand a)
        {
            ushort _a = readValue(a);
            ushort _b = readValue(b);
            uint v = (uint)(_b - _a + _EX);
            writeValue(b, _a);
            _Register[_I] = (ushort)(_Register[_I] - 0x0001u);
            _Register[_J] = (ushort)(_Register[_J] - 0x0001u);
        }

        private void opJSR(operand a)
        {
            ushort _a = readValue(a);
            stackPUSH(_PC);
            _PC = _a;
        }

        private void opINT(operand a)
        {
            ushort _a = readValue(a);
            if(_IA == 0) return;
            _IntQueue.Enqueue(_a);
        }

        private void opIAG(operand a)
        {
            ushort _a = readValue(a);
            writeValue(a, _IA);
        }

        private void opIAS(operand a)
        {
            ushort _a = readValue(a);
            _IA = _a;
        }

        private void opRFI(operand a)
        {
            ushort _a = readValue(a);
            _Register[_A] = stackPOP();
            _PC = stackPOP();
            _IntEnabled = true;
        }

        private void opIAQ(operand a)
        {
            ushort _a = readValue(a);
            _IntEnabled = (_a == 0);
        }

        private void opHWN(operand a)
        {
            ushort _a = readValue(a);
            writeValue(a, (ushort) _Hardware.Count);
        }

        private void opHWQ(operand a)
        {
            ushort _a = readValue(a);
            if(_Hardware.ContainsKey(_a))
            {
                Ie16Hardware hw = _Hardware[_a];
                _Register[_A] = (ushort)(hw.HardwareID&0x0000ffff);
                _Register[_B] = (ushort)((hw.HardwareID>>16)&0x0000ffff);
                _Register[_C] = hw.HardwareVersion;
                _Register[_X] = (ushort)(hw.Manufacturer&0x0000ffff);
                _Register[_Y] = (ushort)((hw.Manufacturer>>16)&0x0000ffff);
            }
        }

        private void opHWI(operand a)
        {
            ushort _a = readValue(a);
            if (_Hardware.ContainsKey(_a))
            {
                Ie16Hardware hw = _Hardware[_a];
                hw.Interrupt(_a);
            }
        }

        private void stackPUSH(ushort value)
        {
            _RAM[--_SP] = value;
        }

        private ushort stackPOP()
        {
            return _RAM[_SP++];
        }
    }
}
