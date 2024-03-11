using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASiNet.NPlus.Tcp.Interfaces;

namespace ASiNet.NPlus.Tcp.Loggers;
public class ConsoleLogger : INPlusLogger
{
    private readonly object _locker = new();

    public void SendError(string text, object? sender = null) =>
        Send("ERROR", sender, text, ConsoleColor.Red);

    public Task SendErrorAsync(string text, object? sender = null) =>
        Task.Run(() => SendError(text, sender));

    public void SendInfo(string text, object? sender = null) =>
        Send("INFO", sender, text, ConsoleColor.Gray);

    public Task SendInfoAsync(string text, object? sender = null) =>
        Task.Run(() => SendInfo(text, sender));

    public void SendWarning(string text, object? sender = null) =>
        Send("WARNING", sender, text, ConsoleColor.Yellow);

    public Task SendWarningAsync(string text, object? sender = null) =>
        Task.Run(() => SendWarning(text, sender));

    private void Send(string tag, object? sender, string text, ConsoleColor color)
    {
        lock (_locker)
        {
            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = color;

            if(sender is null) 
                Console.WriteLine($"[{DateTime.Now}:{tag}] -> {text}");
            else
                Console.WriteLine($"[{DateTime.Now:hh:mm:ss:ffff}:{tag}] : [{sender}] -> {text}");

            Console.ForegroundColor = oldColor;
        }
    }
}
