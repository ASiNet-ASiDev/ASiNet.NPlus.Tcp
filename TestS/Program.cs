
using ASiNet.NPlus.Tcp;
using System.Net.Sockets;

var listener = new TcpListener(System.Net.IPAddress.Any, 44544);

listener.Start();

var np = new NPlusClient(listener.AcceptTcpClient());

var package = np.ReadNextPackage();