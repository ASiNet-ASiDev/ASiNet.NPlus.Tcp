using ASiNet.NPlus.Tcp;
using ASiNetNPlus.ServerModel.Client;
using System.Net.Sockets;

var npClient = new NPlusServerModelClient("127.0.0.1", 44999);

using (var authRC = npClient.GetController("auth"))
{
    var result = authRC.ExecuteController<AuthResult, AuthResponse>("login", new AuthResponse() { Login = "adm", Password = "0000" });

    Console.WriteLine(result.IsDone);
}


Console.ReadLine();


class AuthResponse
{
    public string Login { get; set; }

    public string Password { get; set; }
}

class AuthResult
{
    public bool IsDone { get; set; }
}