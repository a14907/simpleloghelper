using System;
using System.Collections.Generic;
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

            for (int i = 0; i < 100000; i++)
            {
                Log.Write(new LogHelper.Msg
                {
                    Datetime = DateTime.Now,
                    Text = "i=" + i,
                    Type = LogHelper.MsgType.Information
                });
            }


            Console.WriteLine("OK");

            Console.ReadKey();
        }
    }
}
