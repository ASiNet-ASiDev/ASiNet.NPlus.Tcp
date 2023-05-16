using System.IO;
using System.Net.Http;
using System.Net.Sockets;

namespace ASiNet.NPlus.Tcp;
public class NPlusClient : INplusClient
{

    public NPlusClient(TcpClient client)
    {
        client.ReceiveTimeout = TimeSpan.FromSeconds(60).Milliseconds;
        _tcp = client;
    }

    public bool IsConnected => CheckConnected();
    public bool DataAvalible => _tcp?.Available > 0;
    public int AvalibleBytes => _tcp?.Available ?? -1;
    public int AcceptedPackages => _acceptedPackages;
    public int SendedPackages => _sendedPackages;
    public long AcceptedBytes => _acceptedBytes;
    public long SendedBytes => _sendedBytes;

    public TimeSpan AcceptTimeout { get; set; } = TimeSpan.FromSeconds(30);

    public int PackagesInBuffer => _buffer.Count;

    private readonly object _readerLocker = new();
    private readonly object _writerLocker = new();
    private readonly object _bufferLocker = new();

    private int _acceptedPackages;
    private int _sendedPackages;
    private long _acceptedBytes;
    private long _sendedBytes;

    private TcpClient _tcp = null!;
    private Dictionary<Guid, RequestPackage> _buffer = new();

    public ResponsePackage SendAndWaitResponse(Span<byte> package, CancellationToken token = default)
    {
        var stream = _tcp.GetStream();
        var id = Guid.NewGuid();

        WriteNextPackage(id, package);

        var result = AcceptNext(stream, id, token);
        return new(result.Data, result.SendedTime, result.AcceptedTime);
    }

    public async Task<ResponsePackage> SendAndWaitResponseAsync(byte[] package, CancellationToken token = default)
    {
        var stream = _tcp.GetStream();
        var id = Guid.NewGuid();

        WriteNextPackage(id, package);
        var result = await Task.Run(() => AcceptNext(stream, id, token));
        return new(result.Data, result.SendedTime, result.AcceptedTime);
        
    }

    public void SendResponse(Guid id, byte[] package) => WriteNextPackage(id, package);

    public async Task<RequestPackage> AcceptNextAsync(CancellationToken token = default) => await Task.Run(ReadNextPackage, token);
    public RequestPackage AcceptNext() => ReadNextPackage();

    protected RequestPackage AcceptNext(NetworkStream stream, Guid id, CancellationToken token = default)
    {
        RequestPackage package = default;
        var source = new CancellationTokenSource();
        source.CancelAfter(AcceptTimeout);
        while (!token.IsCancellationRequested && !source.Token.IsCancellationRequested)
        {
            lock (_readerLocker)
            {
                if (stream.DataAvailable)
                {
                    package = ReadNextPackage();

                    if (package.Id != id)
                        lock (_bufferLocker)
                            _buffer.Add(package.Id, package);
                            
                    else
                        return package;

                }
            }
            lock (_bufferLocker)
            {
                if(_buffer.ContainsKey(id) && _buffer.Remove(id, out var data))
                    return data;
            }

        }
        return new(Guid.Empty, Array.Empty<byte>(), DateTime.MinValue, DateTime.MinValue);
    }

    protected RequestPackage ReadNextPackage()
    {
        try
        {
            if(_tcp.Available == 0)
                return new(Guid.Empty, Array.Empty<byte>(), DateTime.MinValue, DateTime.MinValue);
            lock (_readerLocker)
            {
                Span<byte> sizeBin = stackalloc byte[sizeof(int)];
                Span<byte> idBin = stackalloc byte[sizeof(decimal)];
                Span<byte> sendedTimeBin = stackalloc byte[sizeof(long)];
                var sizeValue = 0;
                var idValue = Guid.Empty;
                var stream = _tcp.GetStream();
                var sendedTime = DateTime.MinValue;

                stream.Read(idBin);
                stream.Read(sendedTimeBin);
                stream.Read(sizeBin);
                idValue = new(idBin);
                sizeValue = BitConverter.ToInt32(sizeBin);
                var buffer = new byte[sizeValue];
                stream.Read(buffer);

                sendedTime = DateTime.FromBinary(BitConverter.ToInt64(sendedTimeBin));
                _acceptedPackages++;
                _acceptedBytes += buffer.Length + sizeof(int) + sizeof(decimal) + sizeof(long);
                return new(idValue, buffer, sendedTime, DateTime.UtcNow);

            }
        }
        catch (IOException)
        {
            return new(Guid.Empty, Array.Empty<byte>(), DateTime.MinValue, DateTime.MinValue);
        }
    }

    protected void WriteNextPackage(Guid id, in Span<byte> package)
    {
        try
        {
            var stream = _tcp.GetStream();
            lock (_writerLocker)
            {
                stream.Write(id.ToByteArray());
                stream.Write(BitConverter.GetBytes(DateTime.UtcNow.ToBinary()));
                stream.Write(BitConverter.GetBytes(package.Length));
                stream.Write(package);
                _sendedPackages++;
                _sendedBytes += package.Length + sizeof(int) + sizeof(decimal) + sizeof(long);
            }
        }
        catch (IOException)
        { }
    }

    protected bool CheckConnected()
    {
        try
        {
            if (_tcp?.Client?.Connected ?? true)
            {
                if (_tcp!.Client!.Poll(0, SelectMode.SelectRead))
                {
                    Span<byte> buff = stackalloc byte[1];
                    if (_tcp!.Client!.Receive(buff, SocketFlags.Peek) == 0)
                        return false;
                    return true;
                }
                return true;
            }
            else
                return false;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        _tcp?.Dispose();
        GC.SuppressFinalize(this);
    }
}