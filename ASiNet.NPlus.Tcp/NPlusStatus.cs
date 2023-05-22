using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASiNet.NPlus.Tcp;
public enum NPlusStatus : ushort
{
    None,
    NotAvalible,
    NotFound,
    Timeout,
    SendError,
    AcceptError,
    SerializeError,
    DeserializeError,
    RemoteSerizlizeError,
    RemoteDeserializeError,
}
