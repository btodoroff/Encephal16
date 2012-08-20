/* Copyright 2012 Brian Todoroff
 * Encephal16 by Brian Todoroff is licensed under a Creative Commons Attribution-ShareAlike 3.0 Unported License
 * Based on a work at https://github.com/btodoroff/Encephal16.
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using e16;

namespace e16Test
{
    class Program
    {
        static void Main(string[] args)
        {
            e16vm dut = new e16vm();
            Console.WriteLine(dut.ToString());
            ushort[] testPgm = {
                  0x7c01, 0x0030,
                  0x7de1, 0x1000, 0x0020,
                  0x7803, 0x1000,
                  0xc00d,
                  0x7dc1, 0x001a,
                  0xa861,
                  0x7c01, 0x2000,
                  0x2161, 0x2000,
                  0x8463,
                  0x806d,
                  0x7dc1, 0x000d,
                  0x9031,
                  0x7c10, 0x0018,
                  0x7dc1, 0x001a,
                  0x9037,
                  0x61c1,
                  0x7dc1, 0x001a
                               };
            dut.LoadMemory(testPgm);
            Console.WriteLine("Loaded Program");
            Console.WriteLine(dut.ToString());
            do {
                dut.Tick();
                Console.WriteLine(dut.ToString());
                Console.WriteLine(dut.MemToString(0x1000, 0x1000));
                Console.WriteLine(dut.MemToString(0x2000, 0x2010));
            } while (Console.ReadKey().Key != ConsoleKey.Escape);
        }
    }
}
