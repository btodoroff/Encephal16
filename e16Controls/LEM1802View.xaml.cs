using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using e16.Hardware;

namespace e16.Controls
{
    /// <summary>
    /// Interaction logic for LEM1802View.xaml
    /// </summary>
    public partial class LEM1802View : UserControl
    {
        public LEM1802View()
        {
            InitializeComponent();
            _ScreenBitmap = new WriteableBitmap(128, 96, 48, 48, PixelFormats.Rgb24, null);
            LEM1802ScreenImage.Source = _ScreenBitmap;
            RenderOptions.SetBitmapScalingMode(LEM1802ScreenImage, BitmapScalingMode.NearestNeighbor);
        }
        private LEM1802 _LEM;
        private WriteableBitmap _ScreenBitmap;
        public LEM1802 LEM
        {
            set
            {
                _LEM = value;
                Update();
            }
            get
            {
                return _LEM;
            }
        }

        public void Update()
        {
            if (LEM == null)
            {
                ClearScreenImage();
            }
            else
            {
                for (int x = 0; x < 128; x++)
                    for (int y = 0; y < 96; y++)
                        WritePixel(x, 95-y, System.Windows.Media.Color.FromRgb(LEM.ScreenImage[x,y].R,LEM.ScreenImage[x,y].G,LEM.ScreenImage[x,y].B));
            }
        }

        private void ClearScreenImage()
        {
            for (int x = 0; x < 128; x++)
                for (int y = 0; y < 96; y++)
                    WritePixel(x, y, Colors.Aquamarine);
        }

        private void WritePixel(int X, int Y, Color color)
        {
            byte[] ColorData = { 0, 0, 0, 0 }; // R G B _
            ColorData[0] = color.R;
            ColorData[1] = color.G;
            ColorData[2] = color.B;
            Int32Rect rect = new Int32Rect(
                    X, 
                    Y, 
                    1, 
                    1);

            _ScreenBitmap.WritePixels(rect, ColorData, 4, 0);
        }
    }
}
