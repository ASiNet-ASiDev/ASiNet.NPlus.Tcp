using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASiNet.NPlus.Tcp;
internal static class Helper
{
    public static bool ImplementsInterface<T>(this Type type) =>
        type.GetInterfaces().Contains(typeof(T));
}
