namespace ASiNet.NPlus.Tcp.Exceptions;
public class NPlusReadException : Exception
{
    public NPlusReadException(NPlusStatus status)
    {
        Status = status;
    }

    public NPlusStatus Status { get; }

}
