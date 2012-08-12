using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using e16;

namespace e16.Hardware
{
    public class LEM1802 : Ie16Hardware
    {
        enum Interupts : ushort
        {
            MEM_MAP_SCREEN = 0,
            MEM_MAP_FONT = 1,
            MEM_MAP_PALETTE = 2,
            SET_BORDER_COLOR = 3,
            MEM_DUMP_FONT = 4,
            MEM_DUMP_PALETTE = 5
        };

        public uint HardwareID { get; set; }
        public uint Manufacturer { get; set; }
        public ushort HardwareVersion { get; set; }
        public e16vm dcpu16 { get; set; }
        public void Interrupt(ushort a) { }
        public void Tick() {}

        public void Reset()
        {
            _MemMapAddr = 0;
            _Palette = _DefaultPalette;
        }
        private uint[] _DefaultPalette = { 
                                     0x000000,
                                     0x0000aa,
                                     0x00aa00,
                                     0x00aaaa,
                                     0xaa0000,
                                     0xaa00aa,
                                     0xaa5500,
                                     0xaaaaaa,
                                     0x555555,
                                     0x5555ff,
                                     0x55ff55,
                                     0x55ffff,
                                     0xff5555,
                                     0xff55ff,
                                     0xffff55,
                                     0xffffff
                                 };
        private ushort _MemMapAddr;
        private uint[] _Palette;

    }
}
