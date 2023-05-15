using ASiNet.NPlus.Tcp;
using System.Net.Sockets;

var npClient = new NPlusClient(new("127.0.0.1", 44444));

Parallel.For(0, 10000, (i) =>
{
    var buffer = new byte[16];
    Random.Shared.NextBytes(buffer);
    var result = npClient.SendAndWaitResponse(buffer);

    if (result.AcceptedTime > DateTime.MinValue)
        Console.WriteLine($"[{i}]\nBuffer:[{npClient.PackagesInBuffer}]\nP:[{npClient.AcceptedPackages}/{npClient.SendedPackages}]\nBytes[{npClient.SendedBytes}/{npClient.AcceptedBytes}]\nS: [{string.Join(' ', buffer)}] \nA: [{string.Join(' ', result.Data)}]");
    else
        Console.WriteLine($"[{i}] PACKAGE NOT FOUND!");
});
Console.ReadLine();
