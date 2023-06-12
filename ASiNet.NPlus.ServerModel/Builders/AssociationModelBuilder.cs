using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ASiNet.NPlus.ServerModel.Builders;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class ControllerNameAttrubute : Attribute
{
    public ControllerNameAttrubute(string name)
    {
        Name = name;
    }

    public string Name { get; set; }
}

public class AssociationModelBuilder
{

    private List<(string ControllerName, Type ControllerType)> _controllers = new();

    public AssociationModelBuilder AddController<TController>() where TController : new()
    {
        var type = typeof(TController);
        var attr = type.GetCustomAttribute<ControllerNameAttrubute>();

        if(attr is null)
            throw new Exception($"{nameof(ControllerNameAttrubute)} not found");

        _controllers.Add((attr.Name, typeof(TController)));
        return this;
    }

    public AssociationModelBuilder AddController<TController>(string name) where TController : new()
    {
        _controllers.Add((name, typeof(TController)));
        return this;
    }

    public AssociationModelBuilder AddController(string name, Type controllerType)
    {
        _controllers.Add((name, controllerType));
        return this;
    }


    public AssociationModel Build()
    {
        {
            var dublicates = _controllers.GroupBy(x => x.ControllerName).Where(x => x.Count() > 1).ToList();
            if(dublicates.Count > 0)
                throw new Exception($"Dublicate Controllers Names: [{string.Join(", ", dublicates.Select(x => x.Key))}]");
        }
        var controllers = new Dictionary<string, ControllerModel>();
        foreach( var controller in _controllers)
            controllers.Add(controller.ControllerName, new ControllerModel(controller.ControllerName, controller.ControllerType));
        
        var result = new AssociationModel(controllers);

        return result;
    }
}
