using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASiNet.NPlus.Tcp;
public readonly record struct ResponsePackage(byte[] Data, DateTime SendedTime, DateTime AcceptedTime);

public readonly record struct RequestPackage(Guid Id, byte[] Data, DateTime SendedTime, DateTime AcceptedTime);

public readonly record struct ResponsePackage<TObj>(TObj Data, DateTime SendedTime, DateTime AcceptedTime);

public readonly record struct RequestPackage<TObj>(Guid Id, TObj Data, DateTime SendedTime, DateTime AcceptedTime);