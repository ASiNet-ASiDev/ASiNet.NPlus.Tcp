using System.Collections.Frozen;
using System.Reflection;
using ASiNet.Data.Serialization;
using ASiNet.Data.Serialization.Interfaces;
using ASiNet.NPlus.Tcp.Attributes;
using ASiNet.NPlus.Tcp.Exceptions;
using ASiNet.NPlus.Tcp.Generators;
using ASiNet.NPlus.Tcp.Interfaces;
using ASiNet.NPlus.Tcp.Loggers;

namespace ASiNet.NPlus.Tcp.Contexts;
public class ControllersContext
{
    public ControllersContext()
    {
        #if DEBUG
        Logger = new ConsoleLogger();
        #endif

        _generator = new();
        var initResult = InitContext(this);
        _controllers = initResult.Controllers;

        _binarySerializer = BinarySerializer.NewReadonlySerializer(new(), initResult.Packages);
    }

    public static ControllersContext SharedContext => _sharedContext.Value;

    private static Lazy<ControllersContext> _sharedContext = new(() => new());

    private FrozenDictionary<Type, IControllerInstance> _controllers;

    private ControllersGenerator _generator;

    private IBinarySerializer _binarySerializer;

    public IBinarySerializer Serializer => _binarySerializer;

    public INPlusLogger? Logger { get; set; }

    public void RoutePackage(NPlusClient client, INetworkPackage package)
    {
        var type = package.GetType();
        if (_controllers.TryGetValue(type, out var controller))
        {
            controller.RoutePackage(client, package);
            Logger?.SendInfoAsync($"Route package[{type.Name}]", this);
            return;
        }

        Logger?.SendWarningAsync($"The controller of type {type.Name} was not found", this);
        //throw new ContextException($"The controller of type {package.GetType().Name} was not found");
    }

    private static (FrozenDictionary<Type, IControllerInstance> Controllers, Type[] Packages) InitContext(ControllersContext context)
    {
        var controllers = new Dictionary<Type, IControllerInstance>();
        var packages = new List<Type>();

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (var ct in assembly.GetTypes().Where(x => x.GetCustomAttribute<NPlusControllerAttribute>() is not null))
            {
                context.Logger?.SendInfoAsync($"Finded controller[{ct.Name}]", context);

                var controllerInst = (IControllerInstance)InvokeGenerickMethod(context._generator, nameof(ControllersGenerator.GenerateController), [ct], [])!;

                context.Logger?.SendInfoAsync($"Created controller[{ct.Name}]", context);

                foreach (var pt in controllerInst.GetPackagesTypes())
                {
                    packages.Add(pt);
                    if(!controllers.TryAdd(pt, controllerInst))
                        throw new ContextException($"Failed to add controller[{ct.Name}] from package[{pt.Name}]");

                    context.Logger?.SendInfoAsync($"Finded network package[{pt.Name}]", context);
                }
            }
        }
        return (controllers.ToFrozenDictionary(), packages.ToArray());
    }

    private static object? InvokeGenerickMethod(object inst, string methodName, Type[] genericParameters, object?[] parameters)
    {
        var method = inst
            .GetType()
            .GetMethods()
            .Where(x => x.Name == methodName)
            .Where(x => x.GetGenericArguments().Length == genericParameters.Length)
            .First();

        return method
            .MakeGenericMethod(genericParameters)
            .Invoke(inst, parameters);
    }
}
