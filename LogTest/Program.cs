using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using LogHelper;

namespace LogTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.ReadKey();
            Log.MaxSize = 8;

            Stopwatch sw = new Stopwatch();
            sw.Start();
            for (int i = 0; i < 1000000; i++)
            {
                Log.Write(new LogHelper.Msg
                {
                    Datetime = DateTime.Now,
                    Text = "i=" + i,
                    Type = LogHelper.MsgType.Information
                });
            }

            sw.Stop();
            Console.WriteLine(sw.ElapsedMilliseconds);

            Console.WriteLine("OK");

            Console.ReadKey();
        }
    }
}
