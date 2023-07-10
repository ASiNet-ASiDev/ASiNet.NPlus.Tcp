namespace ASiNet.NPlus.Tcp;
public enum NPlusStatus : ushort
{
    None,
    Done,
    Disconnected,
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
