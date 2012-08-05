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
    /// Interaction logic for RegisterView.xaml
    /// </summary>
    public partial class RegisterView : UserControl
    {
        public e16vm dut
        {
            set
            {
                _dut = value;
                Update();
            }
            get
            {
                return _dut;
            }
        }
        private e16vm _dut;
        public RegisterView()
        {
            InitializeComponent();
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
                DataBlock.Text = dut.RegToString() + " Cycles: " + dut.Cycles.ToString();
            }
        }
    }
}
