using ASiNet.Binary.Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ASiNet.NPlus.ServerModel;
[AttributeUsage(AttributeTargets.Method)]
public class MethodNameAttribute : Attribute
{
    public MethodNameAttribute(string name)
    {
          Name = name;
    }

    public string Name { get; set; }
}
public class ControllerModel : IDisposable
{
    public ControllerModel(string name, Type type)
    {
        ControllerType = type;
        ControllerName = name;
        _methods = new();
        MethodNameAttribute? attr = null;
        foreach (var method in type.GetMethods().Where(x => (attr = x.GetCustomAttribute<MethodNameAttribute>()) is not null))
        {
            _methods.Add(attr!.Name, method);
        }
        _methods.TrimExcess();
    }

    public Type ControllerType { get; } = null!;
    public string ControllerName { get; } = null!;

    private Dictionary<string, MethodInfo> _methods = null!;


    public ControllerInst CreateController()
    {
        var inst = Activator.CreateInstance(ControllerType);
        if(inst is null)
            throw new Exception("Create controller failed!");
        return new(ControllerName, inst, this);
    }

    public object? ExecuteMethodObjectResult(string name, object controllerInst, ref BinaryBuffer buffer)
    {
        if (!ControllerType.Equals(controllerInst.GetType()))
            throw new Exception("Controller type Error!");
        if(_methods.TryGetValue(name, out var value))
        {
            var parameter = value.GetParameters().FirstOrDefault();
            if(parameter is null)
                return value.Invoke(controllerInst, new object[] {  });
            else
            {
                var pResult = BinaryBufferSerializer.Deserialize(parameter.ParameterType, ref buffer);
                return value.Invoke(controllerInst, new object?[] { pResult });
            }
        }
            
        else
            throw new Exception("Method not Found!");
    }

    public byte[] ExecuteMethodBinaryResult(string name, object controllerInst, ref BinaryBuffer buffer)
    {
        if (!ControllerType.Equals(controllerInst))
            throw new Exception("Controller type Error!");
        if (_methods.TryGetValue(name, out var value))
        {
            var parameter = value.GetParameters().FirstOrDefault();
            if (parameter is null)
                return value.Invoke(controllerInst, new object[] { }) as byte[] ?? Array.Empty<byte>();
            else
                return value.Invoke(controllerInst, new object?[] { buffer.ToArray(true) }) as byte[] ?? Array.Empty<byte>();
        }

        else
            throw new Exception("Method not Found!");
    }

    public void Dispose()
    {
        _methods.Clear();
        GC.SuppressFinalize(this);
    }
}
