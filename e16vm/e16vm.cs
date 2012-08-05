using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;


namespace e16
{
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

        public uint Cycles { get { return _Cycles; } }

        public e16vm()
        {
            _RAM = new ushort[RAMSize];
            _Register = new ushort[RegisterCount];
            ClearMemory();
            Reset();

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
            _SP = (ushort)(RAMSize - 1);
            _EX = 0;
            _IA = 0;
            _Cycles = 0;
            ClearMemory();
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

        public void Tick()
        {
            _Cycles++;
            ushort inst = nextWord();
            ushort opcode = (ushort)(inst & (ushort)0x001fu);
            ushort b = (ushort)((inst & (ushort)0x03e0u) >> 5);
            ushort a = (ushort)((inst & (ushort)0xfc00u) >> 10);
            if (opcode == 0) // Non-basic opcodes
            {
                operand opA = parseOperand(a);
                switch (b)
                {
                    case 0x01:
                        opJSR(opA);
                        break;
                }
            }
            else // Basic opcodes
            {
                operand opA = parseOperand(a);
                operand opB = parseOperand(b);
                switch (opcode)
                {
                    case 0x01:
                        opSET(opB, opA);
                        break;
                    case 0x02:
                        opADD(opB, opA);
                        break;
                    case 0x03:
                        opSUB(opB, opA);
                        break;
                    case 0x04:
                        opMUL(opB, opA);
                        break;
                    case 0x05:
                        opMLI(opB, opA);
                        break;
                    case 0x06:
                        opDIV(opB, opA);
                        break;
                    case 0x07:
                        opDVI(opB, opA);
                        break;
                    case 0x08:
                        opMOD(opB, opA);
                        break;
                    case 0x0a:
                        opAND(opB, opA);
                        break;
                    case 0x0b:
                        opBOR(opB, opA);
                        break;
                    case 0x0c:
                        opXOR(opB, opA);
                        break;
                    case 0x0d:
                        opSHR(opB, opA);
                        break;
                    case 0x0f:
                        opSHL(opB, opA);
                        break;
                    case 0x10:
                        opIFB(opB, opA);
                        break;
                    case 0x12:
                        opIFE(opB, opA);
                        break;
                    case 0x13:
                        opIFN(opB, opA);
                        break;
                    case 0x14:
                        opIFG(opB, opA);
                        break;
                }
            }
        }

        public void skipNext()
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
            }

        }

        private ushort nextWord() { return _RAM[_PC++]; }

        private operand parseOperand(ushort value)
        {
            if (value < 0x08) //Register
                return new operand(operand.REG,value);
            if (value < 0x10) //[Register]
                return new operand(operand.RAM, _Register[value - 0x08]);
            if (value < 0x18) //[next word + register]
                return new operand(operand.RAM,(ushort)(_Register[value - 0x10] + nextWord()));
            if (value > 0x1f) // literal 0x00 - 0x1f
                return new operand(operand.LIT,(ushort)(value - 0x21));
            switch (value)
            {
                case 0x18: //PUSH
                    return new operand(operand.STK,0);
                case 0x19: //PEEK
                    return new operand(operand.RAM,_SP);
                case 0x1a: //PICK n
                    return new operand(operand.RAM,(ushort)(_SP+nextWord()));
                case 0x1b: //SP
                    return new operand(operand.SP,0);
                case 0x1c: //PC
                    return new operand(operand.PC,0);
                case 0x1d: //EX
                    return new operand(operand.EX,0);
                case 0x1e: //[next word]
                    return new operand(operand.RAM,nextWord());
                case 0x1f: //next word (literal)
                    return new operand(operand.LIT,nextWord());
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
                return;
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


        private void opSHL(operand b, operand a)
        {
            ushort _a = readValue(a);
            ushort _b = readValue(b);
            writeValue(b,(ushort)(_b << _a));
            _EX = (ushort)(((_b<<_a)>>16)&0xffff);
        }

        private void opSHR(operand b, operand a)
        {
            ushort _a = readValue(a);
            ushort _b = readValue(b);
            writeValue(b,(ushort)(_b >> _a));
            _EX = (ushort)(((_b<<16)>>_a)&0xffff);
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

        private void opIFE(operand b, operand a)
        {
            ushort _a = readValue(a);
            ushort _b = readValue(b);
            if (_b != _a) skipNext();
        }

        private void opIFN(operand b, operand a)
        {
            ushort _a = readValue(a);
            ushort _b = readValue(b);
            if (_b == _a) skipNext();
        }

        private void opIFG(operand b, operand a)
        {
            ushort _a = readValue(a);
            ushort _b = readValue(b);
            if (_b <= _a) skipNext();
        }

        private void opIFB(operand b, operand a)
        {
            ushort _a = readValue(a);
            ushort _b = readValue(b);
            if ((_b&_a)!=0) skipNext();
        }

        private void opJSR(operand a)
        {
            ushort _a = readValue(a);
            stackPUSH(_PC);
            //_RAM[--_SP] = _PC;
            _PC = _a;
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
