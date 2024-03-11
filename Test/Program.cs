using ASiNet.NPlus.Tcp;
using ASiNet.NPlus.Tcp.Attributes;
using TestLib;

var client = new NPlusClient("127.0.0.1", 24024);

client.UpdateDelay = TimeSpan.FromMilliseconds(100);

client.SendPackage(new TestPackage1() { TestString = "Hello, Server!"});

Console.ReadLine();

[NPlusController]
public class TestController
{
    [NPlusMethod]
    public void OnTestPackage1(NPlusClient client, TestPackage1 package)
    {
        Console.WriteLine(package.TestString);
    }


    [NPlusMethod]
    public void OnTestPackage2(NPlusClient client, TestPackage2 package)
    {

    }
}
