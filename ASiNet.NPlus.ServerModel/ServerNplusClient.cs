using ASiNet.Binary.Lib;
using ASiNet.Binary.Lib.Expressions.BaseTypes;
using ASiNet.NPlus.Core.Enums;
using ASiNet.NPlus.Tcp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASiNet.NPlus.ServerModel;

internal delegate void ActionResult(ref BinaryBuffer buffer, ref BinaryBuffer resultBuffer);

public class ServerNplusClient : IDisposable
{

    public ServerNplusClient(INPlusClient client, AssociationModel model)
    {
        _client = client;
        _model = model;
        _controllers = new();
    }


    private INPlusClient _client;
    private AssociationModel _model;
    private ControllersCollection _controllers;

    public void AcceptNext()
    {
        var package = _client.AcceptNext();
        try
        {
            if(package.Status == NPlusStatus.NotAvalible)
                return;
            var buffer = new BinaryBuffer(package.Data, stackalloc byte[short.MaxValue]);

            var action = buffer.ReadEnum<ControllerAction>();
            ActionResult result = action switch
            {
                ControllerAction.CreateController => (ref BinaryBuffer buffer, ref BinaryBuffer result) =>
                {
                    var name = buffer.ReadString(Encoding.UTF8);
                    var createResult = CreateController(name);

                    result.Write(createResult.Action);
                    result.Write(createResult.ControllerId);
                }
                ,
                ControllerAction.ExecuteController => (ref BinaryBuffer buffer, ref BinaryBuffer result) =>
                {
                    var id = buffer.ReadGuid();
                    var name = buffer.ReadString(Encoding.UTF8);

                    var exeResult = ExecuteControllerObject(id, name, ref buffer);

                    result.Write(exeResult.Action);
                    if (exeResult.Obj is not null)
                        BinaryBufferSerializer.Serialize(exeResult.Obj!.GetType(), exeResult.Obj!, ref result);
                    
                }
                ,
                ControllerAction.CloseController => (ref BinaryBuffer buffer, ref BinaryBuffer result) =>
                {
                    var id = buffer.ReadGuid();
                    var closeResult = CloseController(id);

                    result.Write(closeResult);
                }
                ,
                _ => throw new NotImplementedException(),
            };
            var resultBuffer = new BinaryBuffer(new byte[ushort.MaxValue], stackalloc byte[short.MaxValue]);
            result.Invoke(ref buffer, ref resultBuffer);
            _client.SendResponse(package.Id, resultBuffer.ToSpan());
        }
        catch (Exception)
        {
            _client.SendResponse(package.Id, Span<byte>.Empty, NPlusStatus.NotFound);
        }

    }

    private (ServerActionResponse Action, Guid ControllerId) CreateController(string name)
    {
        try
        {
            var result = _model.CreateControllerAndAddToControllersCollection(name, _controllers);
            return (ServerActionResponse.CreateControllerDone, result.Id);
        }
        catch
        {
            return (ServerActionResponse.CreateControllerError, Guid.Empty);
        }
    }

    private ServerActionResponse CloseController(Guid id)
    {
        try
        {
            var result = _controllers.RemoveController(id);
            return ServerActionResponse.CloseControllerDone;
        }
        catch
        {
            return ServerActionResponse.CloseControllerError;
        }
    }

    private (ServerActionResponse Action, object? Obj) ExecuteControllerObject(Guid id, string name, ref BinaryBuffer buffer)
    {
        try
        {
            var result = _controllers.ExecuteControllerMethodObject(id, name, ref buffer);
            return (ServerActionResponse.ExecuteControllerDone, result);
        }
        catch
        {
            return (ServerActionResponse.ExecuteControllerError, null);
        }
    }

    public void Dispose()
    {
        (_client as IDisposable)?.Dispose();
        GC.SuppressFinalize(this);
    }
}
