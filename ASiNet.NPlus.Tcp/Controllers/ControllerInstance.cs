using ASiNet.NPlus.Tcp.Generators;
using ASiNet.NPlus.Tcp.Interfaces;

namespace ASiNet.NPlus.Tcp.Controllers;
public class ControllerInstance<T>(NPlusMethodDelegate<T> methodDelegate, Type[] packagesTypes) : IControllerInstance where T : class, new()
{

    public readonly T Instance = new();

    private NPlusMethodDelegate<T> _delegate = methodDelegate;

    private Type[] _packagesTypes = packagesTypes;

    public IEnumerable<Type> GetPackagesTypes() => _packagesTypes;


    public void RoutePackage(NPlusClient client, INetworkPackage package)
    {
        _delegate.Invoke(Instance, client, package);
    }
}
