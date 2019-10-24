using System;

namespace BeatStripper
{
    internal static class Logger
    {
        internal static void Log(object message)
        {
            string time = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
            Console.WriteLine($"{time} > {message.ToString()}");
        }
    }
}
