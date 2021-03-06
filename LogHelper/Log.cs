﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Messaging;
using System.Text;
using System.Threading;

namespace LogHelper
{
    /// <summary>
    /// 企业应用框架的日志类
    /// </summary>
    /// <remarks>此日志类提供高性能的日志记录实现。
    /// 当调用Write方法时不会造成线程阻塞,而是立即完成方法调用,因此调用线程不用等待日志写入文件之后才返回。</remarks>
    public class Log : IDisposable
    {
        //日志对象的缓存队列
        //private static MessageQueue msgs;
        //日志文件保存的路径
        private static string path;

        public static string Path
        {
            get { return Log.path; }
            set { Log.path = value; }
        }
        //日志写入线程的控制标记
        private static bool state;
        //日志记录的类型
        private static LogType type;
        //日志文件生命周期的时间标记
        private static DateTime TimeSign;
        //日志文件写入流对象
        private static StreamWriter writer;
        private static Thread thread;
        public static int MaxSize { get; set; }
        public static string LogFileName { get; set; }
        private static string queuePath = ConfigurationManager.AppSettings["QueuePath"];


        static Log()
        {
            state = true;
            path = ".\\";
            type = LogType.Daily;
            MsmqHelper.CreateMq(queuePath, "日志队列");
            FileOpen();
            MaxSize = 10;
            thread = new Thread(Work);
            thread.IsBackground = true;
            thread.Start();
        }

        //日志文件写入线程执行的方法
        private static void Work()
        {
            while (true)
            {
                //判断队列中是否存在待写入的日志

                //001 这种方法更快些
                using (MessageQueue queue = new MessageQueue(queuePath))
                {
                    int count = 0;
                    foreach (Message m in queue)
                    {
                        count++;
                        XmlMessageFormatter formate = new XmlMessageFormatter(new Type[] { typeof(string) });
                        if (m != null)
                        {
                            m.Formatter = formate;
                            FileWrite(Msg.Deserialize(m.Body.ToString()));
                            queue.Receive();
                        }
                    }
                    if (count == 0)
                    {
                        Console.WriteLine("无队列数据");
                        Thread.Sleep(1000);
                    }
                }

                ////002 这种方法稍微慢一些
                //var ms = MsmqHelper.Receive(queuePath);
                //if (ms.Count > 0)
                //{
                //    foreach (var msg in ms)
                //    {
                //        FileWrite(Msg.Deserialize(msg));
                //    }
                //}
                //else
                //{
                //    //判断是否已经发出终止日志并关闭的消息
                //    if (state)
                //    {
                //        Console.WriteLine("无队列数据");
                //        Thread.Sleep(1000);
                //    }
                //    else
                //    {
                //        thread.Abort();
                //        FileClose();
                //    }
                //}
            }
        }

        //根据日志类型获取日志文件名，并同时创建文件到期的时间标记
        //通过判断文件的到期时间标记将决定是否创建新文件。
        private static string GetFilename()
        {
            DateTime now = DateTime.Now;
            string format = "";
            switch (type)
            {
                case LogType.Daily:
                    TimeSign = new DateTime(now.Year, now.Month, now.Day);
                    TimeSign = TimeSign.AddDays(1);
                    format = "yyyyMMdd'.log'";
                    break;
                case LogType.Weekly:
                    TimeSign = new DateTime(now.Year, now.Month, now.Day);
                    TimeSign = TimeSign.AddDays(7);
                    format = "yyyyMMdd'.log'";
                    break;
                case LogType.Monthly:
                    TimeSign = new DateTime(now.Year, now.Month, 1);
                    TimeSign = TimeSign.AddMonths(1);
                    format = "yyyyMM'.log'";
                    break;
                case LogType.Annually:
                    TimeSign = new DateTime(now.Year, 1, 1);
                    TimeSign = TimeSign.AddYears(1);
                    format = "yyyy'.log'";
                    break;
            }
            return now.ToString(format);
        }

        //写入日志文本到文件的方法
        private static void FileWrite(Msg msg)
        {
            try
            {
                if (writer == null)
                {
                    FileOpen();
                }
                else
                {
                    //判断文件到期标志，如果当前文件到期则关闭当前文件创建新的日志文件
                    if (DateTime.Now >= TimeSign)
                    {
                        FileClose();
                        FileOpen();
                    }
                    FileInfo fInfo = new FileInfo(LogFileName);
                    if (fInfo.Length > 1024 * 1024 * MaxSize)
                    {
                        FileClose();
                        File.Move(LogFileName, LogFileName + DateTime.Now.Ticks.ToString());
                        FileOpen();
                    }
                    writer.Write(msg.Datetime);
                    writer.Write('\t');
                    writer.Write(msg.Type);
                    writer.Write('\t');
                    writer.WriteLine(msg.Text);
                    writer.Flush();
                }
            }
            catch (Exception e)
            {
                Console.Out.Write(e);
            }
        }

        //打开文件准备写入
        private static void FileOpen()
        {
            writer = new StreamWriter(path + GetFilename(), true, Encoding.UTF8);
            LogFileName = path + GetFilename();
        }

        //关闭打开的日志文件
        private static void FileClose()
        {
            if (writer != null)
            {
                writer.Flush();
                writer.Close();
                writer.Dispose();
                writer = null;
            }
        }

        /// <summary>
        /// 写入新日志，根据指定的日志对象Msg
        /// </summary>
        /// <param name="msg">日志内容对象</param>
        public static void Write(Msg msg)
        {
            if (msg != null)
            {
                MsmqHelper.Send(msg.ToString(), queuePath);
            }
        }

        /// <summary>
        /// 写入新日志，根据指定的日志内容和信息类型，采用当前时间为日志时间写入新日志
        /// </summary>
        /// <param name="text">日志内容</param>
        /// <param name="type">信息类型</param>
        public void Write(string text, MsgType type)
        {
            Write(new Msg(text, type));
        }

        /// <summary>
        /// 写入新日志，根据指定的日志时间、日志内容和信息类型写入新日志
        /// </summary>
        /// <param name="dt">日志时间</param>
        /// <param name="text">日志内容</param>
        /// <param name="type">信息类型</param>
        public void Write(DateTime dt, string text, MsgType type)
        {
            Write(new Msg(dt, text, type));
        }

        /// <summary>
        /// 写入新日志，根据指定的异常类和信息类型写入新日志
        /// </summary>
        /// <param name="e">异常对象</param>
        /// <param name="type">信息类型</param>
        public void Write(Exception e, MsgType type)
        {
            Write(new Msg(e.Message, type));
        }

        #region IDisposable 成员

        /// <summary>
        /// 销毁日志对象
        /// </summary>
        public void Dispose()
        {
            state = false;
        }

        #endregion
    }
}
