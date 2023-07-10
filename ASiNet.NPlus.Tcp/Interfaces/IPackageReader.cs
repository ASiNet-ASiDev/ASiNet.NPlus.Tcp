namespace ASiNet.NPlus.Tcp.Interfaces;
public interface IPackageReader : IDisposable
{
    public int AttemptsCount { get; set; }

    public Task<Package?> ReadPackageAsync(Guid id, CancellationToken token = default);

    public Package? ReadPackage(Guid id);

}
