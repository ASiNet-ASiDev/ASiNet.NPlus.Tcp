using ASiNet.NPlus.Tcp;
using System.Net;
using System.Net.Sockets;

namespace ASiNet.NPlus.ServerModel;

public class NPlusServer : IDisposable
{
    public NPlusServer(IPAddress address, int port, AssociationModel model)
    {
        _listener = new(address, port);
        _model = model;
        _clients = new();

    }

    public int TimeUpdatePeriod { get; set; } = 100;

    private TcpListener _listener;
    private AssociationModel _model;

    private Timer? _timer;
    private List<ServerNplusClient> _clients;

    public void Start()
    {
        _listener.Start();
        _timer = new(OnTimeUpdate, null, 0, TimeUpdatePeriod);
    }

    public void Restart()
    {
        _listener.Stop();
        _timer?.Dispose();

        _listener.Start();
        _timer = new(OnTimeUpdate, null, 0, TimeUpdatePeriod);
    }

    public void Stop()
    {
        _listener.Stop();
        _timer?.Dispose();
    }

    private void OnTimeUpdate(object? obj)
    {
        Task.Run(AcceptClient);
        Task.Run(AcceptPackages);
    }

    private void AcceptClient()
    {
        try
        {
            var tcp = _listener.AcceptTcpClient();
            var nplus = new NPlusClient(tcp);
            var serverClient = new ServerNplusClient(nplus, _model);
            _clients.Add(serverClient);
        }
        catch (Exception)
        {

        }
    }

    private void AcceptPackages()
    {
        Parallel.ForEach(_clients, (client) =>
        {
            client.AcceptNext();
        });
    }

    public void Dispose()
    {
        _listener.Stop();
        _timer?.Dispose();
    }
}