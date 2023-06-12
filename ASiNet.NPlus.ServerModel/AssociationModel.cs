using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASiNet.NPlus.ServerModel;
public class AssociationModel : IDisposable
{
    public AssociationModel(Dictionary<string, ControllerModel> controllerModels)
    {
        _controllerModels = controllerModels;
    }


    private Dictionary<string, ControllerModel> _controllerModels;

    public ControllerInst CreateController(string name)
    {
        if (_controllerModels.TryGetValue(name, out var value))
        {
            var inst = value.CreateController();
            return inst;
        }
        else
            throw new Exception("Controller not found!");
    }

    public (ControllerInst Inst, Guid Id) CreateControllerAndAddToControllersCollection(string name, ControllersCollection collection)
    {
        if (_controllerModels.TryGetValue(name, out var value))
        {
            var inst = value.CreateController();
            var id = collection.AddController(inst);
            return (inst, id);
        }
        else
            throw new Exception("Controller not found!");
    }

    public void Dispose()
    {
        foreach (var item in _controllerModels)
            item.Value.Dispose();
        _controllerModels.Clear();
        GC.SuppressFinalize(this);
    }
}
