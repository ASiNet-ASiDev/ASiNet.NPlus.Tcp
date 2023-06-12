
using ASiNet.NPlus.ServerModel;
using ASiNet.NPlus.ServerModel.Builders;

var associationModel = new AssociationModelBuilder()
    .AddController<AuthServise>("auth")
    .Build();

var server = new NPlusServer(System.Net.IPAddress.Any, 44999, associationModel);

server.Start();

Console.ReadLine();


class AuthServise
{
    [MethodName("login")]
    public AuthResult Login(AuthResponse response)
    {
        if (response.Login == "adm" && response.Password == "0000")
            return new() { IsDone = true };
        else
            return new() { IsDone = false };
    }
}

class AuthResponse
{
    public string Login { get; set; }

    public string Password { get; set; }
}

class AuthResult
{
    public bool IsDone { get; set; }
}