using ASiNet.Binary.Lib;
using ASiNet.Binary.Lib.Expressions.BaseTypes;
using ASiNet.NPlus.Core.Enums;
using ASiNet.NPlus.Tcp;
using System.Text;

namespace ASiNetNPlus.ServerModel.Client;
public class NPlusServerModelClient : IDisposable
{
    public NPlusServerModelClient(string host, int port)
    {
        _client = new(host, port);
    }

    private NPlusClient _client;

    public RemoteController GetController(string name, CancellationToken token = default)
    {
        var buffer = new BinaryBuffer(stackalloc byte[short.MaxValue], stackalloc byte[short.MaxValue]);
        buffer.Write(ControllerAction.CreateController);
        buffer.Write(name, Encoding.UTF8);
        var response = _client.SendAndWaitResponse(buffer.ToSpan(), token);
        if(response.Status == NPlusStatus.Done)
        {
            var responseBuffer = new BinaryBuffer(response.Data, stackalloc byte[short.MaxValue]);
            var serverAction = responseBuffer.ReadEnum<ServerActionResponse>();
            var controllerId = responseBuffer.ReadGuid();
            if(serverAction == ServerActionResponse.CreateControllerDone)
            {
                var result = new RemoteController(controllerId, name, _client, true);
                return result;
            }
            else
            {
                var result = new RemoteController(controllerId, name, _client, false);
                return result;
            }
        }
        else
        {
            var result = new RemoteController(Guid.Empty, name, _client, false);
            return result;
        }
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}