using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LogTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.ReadKey();
            LogHelper.Log log = new LogHelper.Log(LogHelper.LogType.Daily);

            for (int i = 0; i < 100000; i++)
            {
                log.Write(new LogHelper.Msg
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
