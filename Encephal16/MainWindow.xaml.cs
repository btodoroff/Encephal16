﻿/* Copyright 2012 Brian Todoroff
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
using e16.Hardware;

namespace Encephal16
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        e16vm dut;
        LEM1802 lem;
        public MainWindow()
        {
            InitializeComponent();
            dut = new e16vm();
            lem = new LEM1802();
            dutMemoryView.dut = dut;
            dutRegisterView.dut = dut;
            WatchView1.dut = dut;
            WatchView2.dut = dut;
            lEM1802View1.LEM = lem;
            dut.AttachHardware(lem, 0);
            UpdateViews();
        }
        
        public void UpdateViews()
        {
            this.dutMemoryView.Update();
            this.dutRegisterView.Update();
            this.WatchView1.Update();
            this.WatchView2.Update();
        }

        private void btnLoad_Click(object sender, RoutedEventArgs e)
        {
            if (System.IO.File.Exists(tbImageFile.Text))
            {
                dut.Reset();
                dut.LoadMemory(tbImageFile.Text);
                UpdateViews();
            }
            else
            {
                MessageBox.Show("The listed image file doesn't exist.");
            }
        }

        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            dut.Reset();
            dutRegisterView.CaptureRegisters();
            UpdateViews();
        }

        private void btnTick_Click(object sender, RoutedEventArgs e)
        {
            dutRegisterView.CaptureRegisters();
            try
            {
                if (cbRealTime.IsChecked.Value)
                    dut.TickRealtime(int.Parse(txtTickCount.Text));
                else
                    dut.Tick(int.Parse(txtTickCount.Text));
                UpdateViews();
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void tbImageFile_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Configure open file dialog box
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.FileName = "Document"; // Default file name
            dlg.DefaultExt = ".dcpu16"; // Default file extension
            dlg.Filter = "DCPU16 Image (.dcpu16)|*.dcpu16"; // Filter files by extension

            // Show open file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                // Open document
                tbImageFile.Text = dlg.FileName;
            }
        }

        private void btnStep_Click(object sender, RoutedEventArgs e)
        {
            dutRegisterView.CaptureRegisters();
            dut.Step();
            UpdateViews();
        }
    }
}
