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
using e16;

namespace e16.Controls
{
    /// <summary>
    /// Interaction logic for MemoryView.xaml
    /// </summary>
    public partial class MemoryView : UserControl
    {
        public e16vm dut {
            set
            {
                _dut=value;
                Update();
            }
            get
            {
                return _dut;
            }
        }
        private ushort _StartAddr;
        public ushort StartAddr { set { _StartAddr = value; } get { return _StartAddr; } }
        
        private ushort _Length;
        public ushort Length { set{_Length = value;} get{return _Length;}}
        private e16vm _dut;

        public MemoryView()
        {
            InitializeComponent();
            StartAddr = 0;
            Length = 8 * 32;
            Update();
            
        }

        public void Update()
        {
            if (dut == null)
            {
                DataBlock.Text = "No controller attached.";
            }
            else
            {
                DataBlock.Text = dut.MemToString(StartAddr, (ushort)(StartAddr+Length));
            }
        }
    }
}
