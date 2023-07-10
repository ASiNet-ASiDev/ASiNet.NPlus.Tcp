namespace ASiNet.NPlus.Tcp.Interfaces;
public interface IPackageWriter : IDisposable
{
    public Task<Guid> WritePackageAsync(byte[] package, CancellationToken token = default);

    public Guid WritePackage(byte[] package);

    public Guid WritePackage(Span<byte> package);
}
