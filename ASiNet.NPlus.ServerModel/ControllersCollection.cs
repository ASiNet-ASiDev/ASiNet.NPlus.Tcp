using ASiNet.Binary.Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASiNet.NPlus.ServerModel;

public record ControllerInst(string Name, object Inst, ControllerModel Model);

public class ControllersCollection : IDisposable
{

    private Dictionary<Guid, ControllerInst> _controllers = new();

    public Guid AddController(ControllerInst inst)
    {
        var id = Guid.NewGuid();
        _controllers.Add(id, inst);
        return id;
    }

    public object? ExecuteControllerMethodObject(Guid id, string methodName, ref BinaryBuffer buffer)
    {
        if(_controllers.TryGetValue(id, out var controller))
        {
            return controller.Model.ExecuteMethodObjectResult(methodName, controller.Inst, ref buffer);
        }
        throw new Exception();
    }

    public byte[] ExecuteControllerMethodBinary(Guid id, string methodName, ref BinaryBuffer buffer)
    {
        if (_controllers.TryGetValue(id, out var controller))
        {
            return controller.Model.ExecuteMethodBinaryResult(methodName, controller.Inst, ref buffer);
        }
        throw new Exception();
    }

    public bool RemoveController(Guid id)
    {
        if(_controllers.TryGetValue(id,out var controller))
        {
            (controller.Inst as IDisposable)?.Dispose();
            return _controllers.Remove(id);
        }
        return false;
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}
