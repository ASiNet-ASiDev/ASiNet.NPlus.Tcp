using ASiNet.NPlus.Tcp.Interfaces;

namespace TestLib;

public class TestPackage1 : INetworkPackage
{
    public string TestString { get; set; }
}

public class TestPackage2 : INetworkPackage
{
    public string TestString { get; set; }
}