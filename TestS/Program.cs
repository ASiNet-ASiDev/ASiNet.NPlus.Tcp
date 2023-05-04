using ASiNet.NPlus.Tcp;
using System.Net.Sockets;

var listener = new TcpListener(System.Net.IPAddress.Any, 44444);
listener.Start();

var npClient = new NPlusClient(listener.AcceptTcpClient());


while (true)
{
    var result = await npClient.AcceptNextAsync();
    if(result.Id == Guid.Empty)
    {
        await Task.Delay(1000);
        continue;
    }
    npClient.SendResponse(result.Id, result.Data);

    Console.WriteLine($"[{result.Id.ToString("N")}] [{string.Join(' ', result.Data)}]");
}



Console.ReadLine();