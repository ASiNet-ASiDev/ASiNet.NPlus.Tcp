using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASiNet.NPlus.Tcp;
public readonly record struct ResponsePackage(byte[] Data, NPlusStatus Status, DateTime SendedTime, DateTime AcceptedTime);

public readonly record struct RequestPackage(Guid Id, byte[] Data, NPlusStatus Status, DateTime SendedTime, DateTime AcceptedTime);

public readonly record struct ResponsePackage<TObj>(TObj? Data, NPlusStatus Status, DateTime SendedTime, DateTime AcceptedTime);

public readonly record struct RequestPackage<TObj>(Guid Id, NPlusStatus Status, TObj? Data, DateTime SendedTime, DateTime AcceptedTime);


internal struct PackageHeader
{
    public const int HEADER_BINARY_SIZE = sizeof(decimal) + sizeof(long) + sizeof(ushort) + sizeof(ushort);

    public Guid Id { get; set; }
    public DateTime SendedTime { get; set; }
    public NPlusStatus Status { get; set; }
    public ushort DataSize { get; set; }
}