using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ASiNet.NPlus.Tcp.Attributes;
using ASiNet.NPlus.Tcp.Controllers;
using ASiNet.NPlus.Tcp.Exceptions;
using ASiNet.NPlus.Tcp.Interfaces;

namespace ASiNet.NPlus.Tcp.Generators;

public delegate void NPlusMethodDelegate<T>(T controller, NPlusClient client, INetworkPackage package);

public class ControllersGenerator
{


    public ControllerInstance<T> GenerateController<T>() where T : class, new()
    {
        var types = new List<Type>();
        var deleg = GenerateLambda<T>(types);
        var inst = new ControllerInstance<T>(deleg, [.. types]);


        return inst;
    }

    public NPlusMethodDelegate<T> GenerateLambda<T>(List<Type> types)
    {
        var type = typeof(T);
        var client = Expression.Parameter(typeof(NPlusClient), "client");
        var package = Expression.Parameter(typeof(INetworkPackage), "pack");
        var controller = Expression.Parameter(type, "controller");

        var body = Expression.Block(EnumerateMethods(type, controller, client, package, types));

        var lambda = Expression.Lambda<NPlusMethodDelegate<T>>(body, controller, client, package);
        return lambda.Compile();
    }


    private IEnumerable<Expression> EnumerateMethods(Type instType, Expression controller, Expression client, Expression package, List<Type> types)
    {
        foreach (var mi in instType.GetMethods()
            .Where(x => x.GetCustomAttribute<NPlusMethodAttribute>() is not null))
        {
            var miParams = mi.GetParameters();
            if (miParams.Length == 2 && 
                miParams[0].ParameterType == typeof(NPlusClient) && 
                miParams[1].ParameterType.ImplementsInterface<INetworkPackage>())
            {
                types.Add(miParams[1].ParameterType);
                yield return Expression.IfThen(

                    Expression.TypeIs(
                        package,
                        miParams[1].ParameterType
                        ),

                    Expression.Call(
                        controller,
                        mi,
                        client,
                        Expression.Convert(
                            package,
                            miParams[1].ParameterType
                            )
                        )
                    );
            }
            else
                throw new GeneratorException(
                    $"Method {mi.Name} marked with attribute {nameof(NPlusMethodAttribute)} in type {instType.FullName ?? instType.Name} does not meet expectations.");
        }
    }
}
