using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASiNet.NPlus.Tcp.Interfaces;
public interface INPlusLogger
{
    public void SendInfo(string text, object? sender = null);

    public void SendError(string text, object? sender = null);

    public void SendWarning(string text, object? sender = null);

    public Task SendInfoAsync(string text, object? sender = null);

    public Task SendErrorAsync(string text, object? sender = null);

    public Task SendWarningAsync(string text, object? sender = null);

}
