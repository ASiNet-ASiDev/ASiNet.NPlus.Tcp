using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using ASiNet.NPlus.Tcp.Contexts;

namespace ASiNet.NPlus.Tcp;
public class NPlusListener
{
    public NPlusListener(int port)
    {
        _clients = [];
        _listener = new(IPAddress.Any, port);
    }

    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan InactiveLifetime { get; set; } = TimeSpan.FromMinutes(5);
    public int ClientsCount => _clients.Count;

    private ControllersContext _context = ControllersContext.SharedContext;

    private Dictionary<Guid, NPlusClient> _clients;

    private TcpListener _listener;

    private CancellationTokenSource? _acceptorCts;

    private readonly object _locker = new();

    public void Start()
    {
        if(_acceptorCts is null || _acceptorCts.IsCancellationRequested)
        {
            _acceptorCts?.Cancel();
            _acceptorCts?.Dispose();

            _acceptorCts = new();

            _listener.Start();
        }
        Task.Run(() => AcceptClient(_acceptorCts.Token), _acceptorCts.Token);
    }

    public void Stop()
    {
        if (_acceptorCts is not null && !_acceptorCts.IsCancellationRequested)
        {
            _acceptorCts.Cancel();
            _acceptorCts.Dispose();

            _listener.Stop();
        }
    }

    private void AcceptClient(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                var tcpClient = _listener.AcceptTcpClient();
                var id = Guid.NewGuid();
                var client = new NPlusClient(tcpClient, id);
                lock (_locker)
                {
                    _clients.Add(id, client);
                }
            }
        }
        catch (Exception ex)
        {
            _context.Logger?.SendErrorAsync(ex.Message);
            throw;
        }
    }

    public void Update()
    {
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(Timeout);
        Task.WaitAll(
            Task.Run(() => UpdateClients(cts.Token)),
            Task.Run(() => RemoveDisconnectedClients(cts.Token))
            );
    }

    private void UpdateClients(CancellationToken token)
    {
        var parOptions = new ParallelOptions() { CancellationToken = token, MaxDegreeOfParallelism = 20 };
        lock (_locker)
        {
            try
            {
                Parallel.ForEach(_clients, parOptions, client =>
                {
                    if (client.Value.Status != Enums.ClientStatus.Disconnected)
                    {
                        client.Value.Update();
                    }
                });
            }
            catch (OperationCanceledException)
            {
                _context.Logger?.SendWarningAsync("Timeout!", this);
            }
        }
    }

    private void RemoveDisconnectedClients(CancellationToken token)
    {
        var buffer = new List<NPlusClient>();
        var time = DateTime.UtcNow;
        foreach (var client in _clients.Values)
        {
            if(token.IsCancellationRequested)
            {
                _context.Logger?.SendWarningAsync("Timeout!", this);
                return;
            }
            if(client.Status == Enums.ClientStatus.Disconnected)
                buffer.Add(client);
            else if(client.Status == Enums.ClientStatus.Authorization && (time - client.LastUpdateTme) > InactiveLifetime)
                buffer.Add(client);
        }

        foreach (var client in buffer)
        {
            if (token.IsCancellationRequested)
            {
                _context.Logger?.SendWarningAsync("Timeout!", this);
                return;
            }
            client.Dispose();
            lock (_locker)
            {
                _clients.Remove(client.Id);
            }
        }
    }
}
