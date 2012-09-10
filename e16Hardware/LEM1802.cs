using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
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

        public uint HardwareID { get { return 0x7349f615u; } }
        public uint Manufacturer { get { return 0x1c6c8b36; } }
        public ushort HardwareVersion { get { return 0x1802; } }
        public e16vm dcpu16 { get; set; }
        private Color[,] _ScreenImage;
        public bool ScreenDirty { get; set; }
        public Color[,] ScreenImage { 
            get { 
                //if(ScreenDirty) DrawScreen(); 
                return _ScreenImage; 
            } 
        }
        public Color[] ScreenPalette { get; set; }
        private ushort _CharMemAddr;
        private ushort _PaletteMemAddr;
        private ushort _FontMemAddr;
        private bool _BlinkState;
        private int _BlinkCounts;
        private int _RefreshCounts;
        private int _RefreshIntervalTicks = 1667; // 100000Hz / 60 fps = 1667 ticks / frame
        public Color BorderColor { get { return GetPaletteColor(_BorderColor); } }
        private ushort _BorderColor;
        private int _BlinkIntervalTicks = 100000; // 100000Hz / 1 invert/sec = 100000 ticks / frame
        public const int XChars = 32;
        public const int YChars = 12;


        public LEM1802()
        {
            _ScreenImage = new Color[XChars * 4, YChars * 8];
            ScreenPalette = new Color[_DefaultPalette.Length];
            _PreviousCharData = null;
            Reset();
        }
        public void Reset()
        {
            _CharMemAddr = 0;
            _PaletteMemAddr = 0;
            _FontMemAddr = 0;
            _BlinkState = false;
            _BlinkCounts = _BlinkIntervalTicks;
            ClearScreenImage();
            ResetPalette();
            ScreenDirty = true;
            //Test code
            byte[] charBuf = GetChar(65);
            Color foreColor = GetPaletteColor(0);
            Color backColor = GetPaletteColor(15);
            PlotChar(0, 0, charBuf, foreColor, backColor);
        }
        public void Interrupt(ushort a) 
        {
            switch((Interupts)a)
            {
                case Interupts.MEM_MAP_SCREEN:
                    _CharMemAddr = dcpu16.B;
                    ScreenDirty = true;
                    _PreviousCharData = new ushort[XChars, YChars]; //TODO: Need to capture the screen char data at each tick and only draw if something changed.
                    return;
                case Interupts.MEM_MAP_FONT:
                    _FontMemAddr = dcpu16.B;
                    ScreenDirty = true;
                    return;
                case Interupts.MEM_MAP_PALETTE:
                    _PaletteMemAddr = dcpu16.B;
                    ScreenDirty = true;
                    return;
                case Interupts.SET_BORDER_COLOR:
                    _BorderColor = (ushort)(dcpu16.B&0x000f);
                    ScreenDirty = true;
                    return;
                case Interupts.MEM_DUMP_FONT:
                    dcpu16.LoadMemory(_DefaultFont,dcpu16.B);
                    return;
                case Interupts.MEM_DUMP_PALETTE:
                    dcpu16.LoadMemory(_DefaultFont,dcpu16.B);
                    return;
            }
        }

        public void ClearScreenImage()
        {
            ClearScreenImage(Color.FromArgb(0, 0, 0));
        }
        public void ClearScreenImage(Color toColor)
        {
            for (int i = 0; i < _ScreenImage.GetLength(0); i++)
                for (int j = 0; j < _ScreenImage.GetLength(1); j++)
                    _ScreenImage[i, j] = toColor;
            ScreenDirty = false;
        }
        public void ResetPalette()
        {
            ScreenPalette = new Color[_DefaultPalette.Length];
            for (int i = 0; i < ScreenPalette.Length; i++)
                ScreenPalette[i] = Color.FromArgb((int)_DefaultPalette[i]);
            ScreenDirty = true;
        }
        public void Tick()
        {
            if(_BlinkCounts-- < 1)
            {
                _BlinkCounts = _BlinkIntervalTicks;
                _BlinkState = !_BlinkState;
            }
            if (_RefreshCounts-- < 1) // Only refresh the screen occasionally to avoid overhead.
            {
                _RefreshCounts = _RefreshIntervalTicks;
                DrawScreen();
            }
        }
        private ushort[,] _PreviousCharData;
        public void DrawScreen()
        {
            byte[] charBuf;
            ushort charData;
            int charCode;
            Color foreColor;
            Color backColor;
            if (_CharMemAddr == 0) return;
            for (int curY = 0; curY < YChars; curY++)
            {
                for (int curX = 0; curX < XChars; curX++)
                {
                    charData = dcpu16.RAM((uint)(_CharMemAddr + curX + (curY * XChars)));
                    if(_PreviousCharData[curX,curY] != charData)
                    {
                        _PreviousCharData[curX, curY] = charData;
                        ScreenDirty = true;
                        charCode = charData & 0x007f;
                        charBuf = GetChar(charCode);
                        foreColor = GetPaletteColor((charData & 0xf000) >> 12);
                        backColor = GetPaletteColor((charData & 0x0f00) >> 8);
                        if (_BlinkState || ((charData & 0x0080) == 0))
                            PlotChar(curX, curY, charBuf, foreColor, backColor);
                        else
                            PlotChar(curX, curY, charBuf, backColor, foreColor);
                    }
                }
            }
        }
        public byte[] GetChar(int charCode)
        {
            byte[] output = new byte[4];
            ushort[] data = new ushort[2];
            if (_FontMemAddr == 0)
            {
                data[0] = _DefaultFont[charCode * 2];
                data[1] = _DefaultFont[charCode * 2 + 1];
            }
            else
            {
                data[0] = dcpu16.RAM((uint)(_FontMemAddr + (charCode * 2)));
                data[1] = dcpu16.RAM((uint)(_FontMemAddr + 1 +(charCode * 2)));
            }
            output[0] = (byte)((data[0]&0xff00)>>8);
            output[1] = (byte)(data[0] & 0x00ff);
            output[2] = (byte)((data[1] & 0xff00) >> 8);
            output[3] = (byte)(data[1] & 0x00ff);
            return output;
        }
        public Color GetPaletteColor(int colorIndex)
        {
            uint colorData;
            if (_PaletteMemAddr == 0)
                colorData = _DefaultPalette[colorIndex];
            else
                colorData = dcpu16.RAM((uint)(_PaletteMemAddr + colorIndex));
            return Color.FromArgb((int)(colorData|0xff000000u));
        }
        public void PlotChar(int x, int y, byte[] charData, Color foreground, Color background)
        {
            Color pixelColor;
            for(int charX = 0; charX < 4; charX ++)
            {
                for (int charY = 0; charY < 8; charY++)
                {

                    if ((charData[charX] & (0x01 << charY)) != 0)
                        pixelColor = foreground;
                    else
                        pixelColor = background;
                    _ScreenImage[(x * 4) + charX, _ScreenImage.GetLength(1) - 1 - ((y * 8) + charY)] = pixelColor;
                }
            }
            ScreenDirty = true;
        }
        private static readonly uint[] _DefaultPalette = 
        { 
            0xff000000,
            0xff0000aa,
            0xff00aa00,
            0xff00aaaa,
            0xffaa0000,
            0xffaa00aa,
            0xffaa5500,
            0xffaaaaaa,
            0xff555555,
            0xff5555ff,
            0xff55ff55,
            0xff55ffff,
            0xffff5555,
            0xffff55ff,
            0xffffff55,
            0xffffffff
        };
        private static readonly ushort[] _DefaultFont = 
        {
            0xB79E,
            0x388E,
            0x722C,
            0x75F4,
            0x19BB,
            0x7F8F,
            0x85F9,
            0xB158,
            0x242E,
            0x2400,
            0x082A,
            0x0800,
            0x0008,
            0x0000,
            0x0808,
            0x0808,
            0x00FF,
            0x0000,
            0x00F8,
            0x0808,
            0x08F8,
            0x0000,
            0x080F,
            0x0000,
            0x000F,
            0x0808,
            0x00FF,
            0x0808,
            0x08F8,
            0x0808,
            0x08FF,
            0x0000,
            0x080F,
            0x0808,
            0x08FF,
            0x0808,
            0x6633,
            0x99CC,
            0x9933,
            0x66CC,
            0xFEF8,
            0xE080,
            0x7F1F,
            0x0701,
            0x0107,
            0x1F7F,
            0x80E0,
            0xF8FE,
            0x5500,
            0xAA00,
            0x55AA,
            0x55AA,
            0xFFAA,
            0xFF55,
            0x0F0F,
            0x0F0F,
            0xF0F0,
            0xF0F0,
            0x0000,
            0xFFFF,
            0xFFFF,
            0x0000,
            0xFFFF,
            0xFFFF,
            0x0000,
            0x0000,
            0x005F,
            0x0000,
            0x0300,
            0x0300,
            0x3E14,
            0x3E00,
            0x266B,
            0x3200,
            0x611C,
            0x4300,
            0x3629,
            0x7650,
            0x0002,
            0x0100,
            0x1C22,
            0x4100,
            0x4122,
            0x1C00,
            0x1408,
            0x1400,
            0x081C,
            0x0800,
            0x4020,
            0x0000,
            0x0808,
            0x0800,
            0x0040,
            0x0000,
            0x601C,
            0x0300,
            0x3E49,
            0x3E00,
            0x427F,
            0x4000,
            0x6259,
            0x4600,
            0x2249,
            0x3600,
            0x0F08,
            0x7F00,
            0x2745,
            0x3900,
            0x3E49,
            0x3200,
            0x6119,
            0x0700,
            0x3649,
            0x3600,
            0x2649,
            0x3E00,
            0x0024,
            0x0000,
            0x4024,
            0x0000,
            0x0814,
            0x2241,
            0x1414,
            0x1400,
            0x4122,
            0x1408,
            0x0259,
            0x0600,
            0x3E59,
            0x5E00,
            0x7E09,
            0x7E00,
            0x7F49,
            0x3600,
            0x3E41,
            0x2200,
            0x7F41,
            0x3E00,
            0x7F49,
            0x4100,
            0x7F09,
            0x0100,
            0x3E41,
            0x7A00,
            0x7F08,
            0x7F00,
            0x417F,
            0x4100,
            0x2040,
            0x3F00,
            0x7F08,
            0x7700,
            0x7F40,
            0x4000,
            0x7F06,
            0x7F00,
            0x7F01,
            0x7E00,
            0x3E41,
            0x3E00,
            0x7F09,
            0x0600,
            0x3E41,
            0xBE00,
            0x7F09,
            0x7600,
            0x2649,
            0x3200,
            0x017F,
            0x0100,
            0x3F40,
            0x3F00,
            0x1F60,
            0x1F00,
            0x7F30,
            0x7F00,
            0x7708,
            0x7700,
            0x0778,
            0x0700,
            0x7149,
            0x4700,
            0x007F,
            0x4100,
            0x031C,
            0x6000,
            0x0041,
            0x7F00,
            0x0201,
            0x0200,
            0x8080,
            0x8000,
            0x0001,
            0x0200,
            0x2454,
            0x7800,
            0x7F44,
            0x3800,
            0x3844,
            0x2800,
            0x3844,
            0x7F00,
            0x3854,
            0x5800,
            0x087E,
            0x0900,
            0x4854,
            0x3C00,
            0x7F04,
            0x7800,
            0x447D,
            0x4000,
            0x2040,
            0x3D00,
            0x7F10,
            0x6C00,
            0x417F,
            0x4000,
            0x7C18,
            0x7C00,
            0x7C04,
            0x7800,
            0x3844,
            0x3800,
            0x7C14,
            0x0800,
            0x0814,
            0x7C00,
            0x7C04,
            0x0800,
            0x4854,
            0x2400,
            0x043E,
            0x4400,
            0x3C40,
            0x7C00,
            0x1C60,
            0x1C00,
            0x7C30,
            0x7C00,
            0x6C10,
            0x6C00,
            0x4C50,
            0x3C00,
            0x6454,
            0x4C00,
            0x0836,
            0x4100,
            0x0077,
            0x0000,
            0x4136,
            0x0800,
            0x0201,
            0x0201,
            0x0205,
            0x0200 
        };

    }
}
