using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASiNet.NPlus.Tcp.Interfaces;

public interface IControllerInstance
{
    public void RoutePackage(NPlusClient client, INetworkPackage package);

    public IEnumerable<Type> GetPackagesTypes();
}
