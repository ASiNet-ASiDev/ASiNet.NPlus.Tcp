using ASiNet.NPlus.Tcp.Attributes;
using ASiNet.NPlus.Tcp.Controllers.Base.Packages;

namespace ASiNet.NPlus.Tcp.Controllers.Base;

[NPlusController]
public class ClientBaseController
{

    [NPlusMethod]
    public void OnId(NPlusClient client, IdPackage package)
    {
        if (package.IsRequest)
        {
            package.IsRequest = false;
            package.Id = client.Id;
            client.SendPackage(package);
        }
        else
        {
            client.Id = package.Id;
            if (client.Status == Enums.ClientStatus.Authorization)
                client.Status = Enums.ClientStatus.Connected;
        }
    }
}
