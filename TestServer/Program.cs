using ASiNet.NPlus.Tcp.Attributes;
using ASiNet.NPlus.Tcp;
using TestLib;

var server = new NPlusListener(24024);

server.Start();

while (true)
{
    server.Update();
    Task.Delay(100).Wait();
}


[NPlusController]
public class TestController
{
    [NPlusMethod]
    public void OnTestPackage1(NPlusClient client, TestPackage1 package)
    {
        Console.WriteLine(package.TestString);

        client.SendPackage(new TestPackage1() { TestString = "Hello, Client!" });
    }


    [NPlusMethod]
    public void OnTestPackage2(NPlusClient client, TestPackage2 package)
    {

    }
}