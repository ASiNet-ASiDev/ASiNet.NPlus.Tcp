using System.Net.Sockets;
using ASiNet.NPlus.Tcp.Contexts;
using ASiNet.NPlus.Tcp.Interfaces;
using ASiNet.Data.Serialization;
using ASiNet.NPlus.Tcp.Enums;
using ASiNet.Data.Serialization.Exceptions;
using ASiNet.NPlus.Tcp.Controllers.Base.Packages;

namespace ASiNet.NPlus.Tcp;

public class NPlusClient : IDisposable
{
    public NPlusClient(TcpClient client, Guid id)
    {
        _client = client;
        _stream = client.GetStream();
        Id = id;
    }

    public NPlusClient(string address, int port)
    {
        try
        {
            _client = new(address, port);
            _stream = _client.GetStream();
        }
        catch (Exception ex)
        {
            if (ex.InnerException is SocketException se)
            {
                _controllers.Logger?.SendError($"Client not connected.\nSocket Exception[Code: {se.ErrorCode}/{se.SocketErrorCode}]\nMessage: {se.Message}", this);
                Status = ClientStatus.Disconnected;
            }
            throw;
        }
        GetId();
    }

    public Guid Id { get; internal set; }

    public double Ping {  get; internal set; }

    public ClientStatus Status { get; internal set; } = ClientStatus.Authorization;

    public DateTime LastUpdateTme { get; private set; } = DateTime.UtcNow;

    private TcpClient _client;
    private NetworkStream _stream;

    private ControllersContext _controllers = ControllersContext.SharedContext;


    private readonly object _readerLocker = new();
    private readonly object _writerLocker = new();

    public TimeSpan UpdateDelay 
    { 
        get => _updateDelay;
        set
        {
            _updateDelay = value;
            if (_updateDelay != TimeSpan.Zero)
            {
                _timer?.Dispose();
                _timer = new(x => Update(), null, TimeSpan.Zero, value);
            }
        }
    }

    private TimeSpan _updateDelay = TimeSpan.Zero;

    private Timer? _timer;

    public async Task UpdateAsync() =>
        await Task.Run(Update);

    public async Task SendPackageAsync(INetworkPackage package) =>
        await Task.Run(() => SendPackage(package));

    public void Update()
    {
        if (_client.Available == 0)
            return;
        lock (_writerLocker) 
        {
            try
            {
                var package = _controllers.Serializer.Deserialize<INetworkPackage>(_stream);
                if (package is null)
                    return;
                LastUpdateTme = DateTime.UtcNow;
                _controllers.RoutePackage(this, package);
            }
            catch (IOException ex)
            {
                if(ex.InnerException is SocketException se)
                {
                    _controllers.Logger?.SendError($"Client<{Id}> closed.\nSocket Exception[Code: {se.ErrorCode}/{se.SocketErrorCode}]\nMessage: {se.Message}", this);
                    Status = ClientStatus.Disconnected;
                }
            }
            catch (ReaderException ex) 
            {
                _controllers.Logger?.SendError($"Client<{Id}> throwed reader exception.\nMessage: {ex.Message}", this);
                Status = ClientStatus.Disconnected;
            }
            catch (Exception ex)
            {
                _controllers.Logger?.SendError($"Client<{Id}> throwed other exception.\nMessage: {ex.Message}\nSt:{ex.StackTrace}", this);
                Status = ClientStatus.Disconnected;
            }
        }
    }

    public void SendPackage(INetworkPackage package)
    {
        lock (_readerLocker)
        {
            try
            {
                _controllers.Serializer.Serialize(package, _stream);
                LastUpdateTme = DateTime.UtcNow;
            }
            catch (IOException ex)
            {
                if (ex.InnerException is SocketException se)
                {
                    _controllers.Logger?.SendError($"Client<{Id}> closed.\nSocket Exception[Code: {se.ErrorCode}/{se.SocketErrorCode}]\nMessage: {se.Message}", this);
                    Status = ClientStatus.Disconnected;
                }
            }
            catch (WriterException ex)
            {
                _controllers.Logger?.SendError($"Client<{Id}> throwed writer exception.\nMessage: {ex.Message}", this);
                Status = ClientStatus.Disconnected;
            }
            catch (Exception ex)
            {
                _controllers.Logger?.SendError($"Client<{Id}> throwed other exception.\nMessage: {ex.Message}\nSt:{ex.StackTrace}", this);
                Status = ClientStatus.Disconnected;
            }
        }
    }

    public void GetId()
    {
        var package = new IdPackage() { IsRequest = true };
        SendPackage(package);
    }


    public void Dispose()
    {
        lock (_writerLocker)
        {
            lock (_readerLocker)
            {
                Status = ClientStatus.Disconnected;
                _client.Dispose();
                _stream.Dispose();
            }
        }
    }
}
