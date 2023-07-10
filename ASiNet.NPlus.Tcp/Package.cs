namespace ASiNet.NPlus.Tcp;

public readonly record struct Package(Guid Id, byte[] Data, NPlusStatus Status, DateTime SendedTime, DateTime AcceptedTime);
public readonly record struct Package<TObj>(Guid Id, NPlusStatus Status, TObj? Data, DateTime SendedTime, DateTime AcceptedTime);


internal struct PackageHeader
{
    public const int HEADER_BINARY_SIZE = sizeof(decimal) + sizeof(long) + sizeof(ushort) + sizeof(ushort);

    public Guid Id { get; set; }
    public DateTime SendedTime { get; set; }
    public NPlusStatus Status { get; set; }
    public ushort DataSize { get; set; }
}