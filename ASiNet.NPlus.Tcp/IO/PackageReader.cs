using ASiNet.NPlus.Tcp.Interfaces;

namespace ASiNet.NPlus.Tcp.IO;
public class PackageReader : IPackageReader
{
    public PackageReader(INPlusClient client)
    {
        _client = client;
    }

    public int AttemptsCount { get; set; } = 10;

    private INPlusClient _client;

    private Lazy<Dictionary<Guid, Package>> _packages = new(() => new Dictionary<Guid, Package>());

    private readonly object _locker = new object();

    public Package? ReadPackage(Guid id)
    {
        return Read(id);
    }

    public async Task<Package?> ReadPackageAsync(Guid id, CancellationToken token = default)
    {
        return await Task.Run(() => Read(id));
    }


    private Package? Read(Guid id)
    {
        var i = 0;
        while (i < 10)
        {
            lock (_locker)
            {
                if (_packages.IsValueCreated && _packages.Value.Remove(id, out var package))
                {
                    return package;
                }
                else
                {
                    var pack = _client.ReadNextPackage();
                    if (pack.Id == id)
                        return pack;
                    else
                        _packages.Value.Add(id, pack);
                }
            }
            i++;
        }
        return null;
    }

    public void Dispose()
    {
        _packages.Value.Clear();
        GC.SuppressFinalize(this);
    }
}
