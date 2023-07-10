using ASiNet.NPlus.Tcp.Interfaces;

namespace ASiNet.NPlus.Tcp.IO;
public class PackageWriter : IPackageWriter
{

    public PackageWriter(INPlusClient client)
    {
        _client = client;
    }

    private INPlusClient _client;

    private readonly object _locker = new object();

    public Guid WritePackage(byte[] package)
    {
        lock (_locker)
        {
            return _client.WriteNextPackage(package);
        }
    }

    public Guid WritePackage(Span<byte> package)
    {
        lock (_locker)
        {
            return _client.WriteNextPackage(package);
        }
    }

    public async Task<Guid> WritePackageAsync(byte[] package, CancellationToken token = default)
    {
        return await Task.Run(() =>
        {
            lock (_locker)
            {
                return _client.WriteNextPackage(package);
            }
        }, token);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
