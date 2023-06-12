using ASiNet.Binary.Lib;
using ASiNet.Binary.Lib.Expressions.BaseTypes;
using ASiNet.NPlus.Core.Enums;
using ASiNet.NPlus.Tcp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASiNetNPlus.ServerModel.Client;
public class RemoteController : IDisposable
{
    public RemoteController(Guid id, string name, NPlusClient client, bool isConnected)
    {
        _client = client;
        ControllerId = id;
        ControllerName = name;
        IsConnected = isConnected;
    }

    public bool IsConnected { get; private set; }

    public Guid ControllerId { get; init; }
    public string ControllerName { get; init; }

    private NPlusClient _client;

    public T? ExecuteController<T, R>(string methodName, in R data) where T : new()
    {
        var buffer = new BinaryBuffer(stackalloc byte[byte.MaxValue + ushort.MaxValue], stackalloc byte[short.MaxValue]);
        buffer.Write(ControllerAction.ExecuteController);
        buffer.Write(ControllerId);
        buffer.Write(methodName, Encoding.UTF8);
        BinaryBufferSerializer.Serialize(data, ref buffer);
        var response = _client.SendAndWaitResponse(buffer.ToSpan());

        var responseBuffer = new BinaryBuffer(response.Data, stackalloc byte[short.MaxValue]);
        var responseAction = responseBuffer.ReadEnum<ServerActionResponse>();
        if (responseAction == ServerActionResponse.ExecuteControllerDone)
        {
            var result = BinaryBufferSerializer.Deserialize<T>(ref responseBuffer);
            return result;
        }
        else
        {
            return default;
        }
    }

    public void Dispose()
    {
        if(!IsConnected)
        {
            GC.SuppressFinalize(this); 
            return;
        }

        var buffer = new BinaryBuffer(stackalloc byte[byte.MaxValue], stackalloc byte[byte.MaxValue]);
        buffer.Write(ControllerAction.CloseController);
        buffer.Write(ControllerId);

        var response = _client.SendAndWaitResponse(buffer.ToSpan());

        var responseBuffer = new BinaryBuffer(response.Data, stackalloc byte[short.MaxValue]);
        var responseAction = responseBuffer.ReadEnum<ServerActionResponse>();
        if(responseAction == ServerActionResponse.CloseControllerDone)
        {
            IsConnected = false;
        }
        else
        {
            throw new Exception("Close controller error!");
        }
        GC.SuppressFinalize(this);
    }
}