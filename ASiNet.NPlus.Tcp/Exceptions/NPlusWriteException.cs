namespace ASiNet.NPlus.Tcp.Exceptions;
public class NPlusWriteException : Exception
{
    public NPlusWriteException(NPlusStatus status)
    {
        Status = status;
    }

    public NPlusStatus Status { get; }
}
