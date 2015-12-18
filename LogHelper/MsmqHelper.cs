using System;
using System.Collections.Generic;
using System.Linq;
using System.Messaging;
using System.Text;

namespace LogHelper
{
    public class MsmqHelper
    {
        public static bool Send(string json, string path)
        {
            try
            {
                using (MessageQueue queue = new MessageQueue(path))
                {
                    queue.Send(json);
                    return true;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public static List<string> Receive(string path)
        {
            try
            {
                using (MessageQueue queue = new MessageQueue(path))
                {
                    var messages = queue.GetAllMessages();
                    XmlMessageFormatter formate = new XmlMessageFormatter(new Type[] { typeof(string) });
                    var res = new List<string>();
                    if (messages.Length > 0)
                    {
                        foreach (var m in messages)
                        {
                            if (m != null)
                            {
                                m.Formatter = formate;
                                res.Add(m.Body.ToString());
                            }
                        }

                        for (var i = 0; i < messages.Length; i++)
                        {
                            queue.Receive();
                        }
                    }
                    return res;
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }


        public static MessageQueue CreateMq(string path, string name)
        {
            if (!MessageQueue.Exists(path))
            {
                var queue = MessageQueue.Create(path);
                queue.QueueName = name;
                queue.SetPermissions("", MessageQueueAccessRights.FullControl);
            }
            return new MessageQueue(path);
        }
    }
}
