using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASiNet.NPlus.Core.Enums;
public enum ControllerAction : byte
{
    CreateController,
    CloseController,
    ExecuteController,
}

public enum ServerActionResponse : byte
{
    CreateControllerDone,
    CloseControllerDone,
    ExecuteControllerDone,

    CreateControllerError,
    CloseControllerError,
    ExecuteControllerError,
}
