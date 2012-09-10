/* Copyright 2012 Brian Todoroff
 * Encephal16 by Brian Todoroff is licensed under a Creative Commons Attribution-ShareAlike 3.0 Unported License
 * Based on a work at https://github.com/btodoroff/Encephal16.
*/
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
        public struct dutRegisters {
            public ushort PC;
            public ushort SP;
            public ushort EX;
            public ushort IA;
            public ushort A;
            public ushort B;
            public ushort C;
            public ushort X;
            public ushort Y;
            public ushort Z;
            public ushort I;
            public ushort J;
            public uint Cycles;
        };
        public dutRegisters PrevRegisters;
        public void CaptureRegisters()
        {
            dutRegisters regSet = new dutRegisters();
            regSet.PC = dut.PC;
            regSet.SP = dut.SP;
            regSet.EX = dut.EX;
            regSet.IA = dut.IA;
            regSet.A = dut.A;
            regSet.B = dut.B;
            regSet.C = dut.C;
            regSet.X = dut.X;
            regSet.Y = dut.Y;
            regSet.Z = dut.Z;
            regSet.I = dut.I;
            regSet.J = dut.J;
            regSet.Cycles = dut.Cycles;
            PrevRegisters = regSet;
        }

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
                DataBlock.Document.Blocks.Clear();
                DataBlock.AppendText("No controller attached.");
            }
            else
            {
                Brush same = Brushes.Black;
                Brush change = Brushes.Red;
                DataBlock.Document.Blocks.Clear();
                AppendColorText("A :" + dut.A.ToString("X4") + " ", PrevRegisters.A == dut.A ? same : change);
                AppendColorText("B :" + dut.B.ToString("X4") + " ", PrevRegisters.B == dut.B ? same : change);
                AppendColorText("C :" + dut.C.ToString("X4") + " ", PrevRegisters.C == dut.C ? same : change);
                AppendColorText("X :" + dut.X.ToString("X4") + " ", PrevRegisters.X == dut.X ? same : change);
                AppendColorText("Y :" + dut.Y.ToString("X4") + " ", PrevRegisters.Y == dut.Y ? same : change);
                AppendColorText("Z :" + dut.Z.ToString("X4") + " ", PrevRegisters.Z == dut.Z ? same : change);
                AppendColorText("I :" + dut.I.ToString("X4") + " ", PrevRegisters.I == dut.I ? same : change);
                AppendColorText("J :" + dut.J.ToString("X4") + "\n", PrevRegisters.J == dut.J ? same : change);
                AppendColorText("PC:" + dut.PC.ToString("X4") + " ", PrevRegisters.PC == dut.PC ? same : change);
                AppendColorText("SP:" + dut.SP.ToString("X4") + " ", PrevRegisters.SP == dut.SP ? same : change);
                AppendColorText("EX:" + dut.EX.ToString("X4") + " ", PrevRegisters.EX == dut.EX ? same : change);
                AppendColorText("IA:" + dut.IA.ToString("X4") + " ", PrevRegisters.IA == dut.IA ? same : change);
                AppendColorText("Cycles:" + dut.Cycles.ToString(), PrevRegisters.Cycles == dut.Cycles ? same : change);
            }
        }
        private void AppendColorText(String text, Brush brush)
        {
            TextRange tr = new TextRange(DataBlock.Document.ContentEnd, DataBlock.Document.ContentEnd);
            tr.Text = text;
            tr.ApplyPropertyValue(TextElement.ForegroundProperty, brush); 
        }
    }
}
