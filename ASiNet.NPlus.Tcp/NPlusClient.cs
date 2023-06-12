using ASiNet.Binary.Lib;
using ASiNet.Binary.Lib.Serializer;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;

namespace ASiNet.NPlus.Tcp;
public class NPlusClient : INPlusClient, ITypedNPlusClient
{

    public NPlusClient(TcpClient client)
    {
        client.ReceiveTimeout = TimeSpan.FromSeconds(60).Milliseconds;
        _tcp = client;
    }

    public NPlusClient(string host, int port)
    {
        var client = new TcpClient(host, port);
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

    public uint SerializerBufferSize { get; set; } = 4096;

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

        var wr = WriteNextPackage(id, package);
        if(wr != NPlusStatus.Done)
            return new(Array.Empty<byte>(), wr, DateTime.MinValue, DateTime.MinValue);

        var result = AcceptNext(stream, id, token);
        return new(result.Data, NPlusStatus.Done, result.SendedTime, result.AcceptedTime);
    }

    public ResponsePackage<TOut> SendAndWaitResponse<TIn, TOut>(TIn data, CancellationToken token = default) where TOut : new()
    {
        var stream = _tcp.GetStream();
        var id = Guid.NewGuid();

        Span<byte> buffer = SerializerBufferSize < ushort.MaxValue ? stackalloc byte[(int)SerializerBufferSize] : new byte[(int)SerializerBufferSize];

        var serializeResult = BinarySerializer.Serialize(data, buffer);

        if (serializeResult == -1)
            return new(default, NPlusStatus.SerializeError, DateTime.MinValue, DateTime.MinValue);

        var wr = WriteNextPackage(id, buffer.Slice(0, serializeResult));
        if (wr != NPlusStatus.Done)
            return new(default, wr, DateTime.MinValue, DateTime.MinValue);


        var result = AcceptNext(stream, id, token);
        if (BinarySerializer.Deserialize<TOut>(result.Data) is TOut dataObj)
            return new(dataObj, NPlusStatus.Done, result.SendedTime, result.AcceptedTime);
        else
            return new(default, NPlusStatus.DeserializeError, DateTime.MinValue, DateTime.MinValue);
    }

    public async Task<ResponsePackage> SendAndWaitResponseAsync(byte[] package, CancellationToken token = default)
    {
        var stream = _tcp.GetStream();
        var id = Guid.NewGuid();

        var wr = WriteNextPackage(id, package);
        if (wr != NPlusStatus.Done)
            return new(Array.Empty<byte>(), wr, DateTime.MinValue, DateTime.MinValue);

        var result = await Task.Run(() => AcceptNext(stream, id, token));
        return new(result.Data, NPlusStatus.Done, result.SendedTime, result.AcceptedTime);
        
    }

    public async Task<ResponsePackage<TOut>> SendAndWaitResponseAsync<TIn, TOut>(TIn data, CancellationToken token = default) where TOut : new()
    {
        var result = await Task.Run(() =>
        {
            var stream = _tcp.GetStream();
            var id = Guid.NewGuid();

            Span<byte> buffer = SerializerBufferSize < ushort.MaxValue ? stackalloc byte[(int)SerializerBufferSize] : new byte[(int)SerializerBufferSize];
            var serializeResult = BinarySerializer.Serialize(data, buffer);

            if (serializeResult == -1)
                return new(default, NPlusStatus.SerializeError, DateTime.MinValue, DateTime.MinValue);

            var wr = WriteNextPackage(id, buffer.Slice(0, serializeResult));
            if (wr != NPlusStatus.Done)
                return new(default, wr, DateTime.MinValue, DateTime.MinValue);

            var result = AcceptNext(stream, id, token);

            if (BinarySerializer.Deserialize<TOut>(result.Data) is TOut dataObj)
                return new ResponsePackage<TOut>(dataObj, NPlusStatus.Done, result.SendedTime, result.AcceptedTime);
            else
                return new(default, NPlusStatus.DeserializeError, DateTime.MinValue, DateTime.MinValue);
        });
        return result;
    }

    public NPlusStatus SendResponse(Guid id, byte[] package, NPlusStatus status = NPlusStatus.Done) => WriteNextPackage(id, package, status);

    public NPlusStatus SendResponse(Guid id, Span<byte> package, NPlusStatus status = NPlusStatus.Done) => WriteNextPackage(id, package, status);

    public NPlusStatus SendResponse<TOut>(Guid id, TOut data)
    {
        Span<byte> buffer = SerializerBufferSize < ushort.MaxValue ? stackalloc byte[(int)SerializerBufferSize] : new byte[(int)SerializerBufferSize];

        var serializeResult = BinarySerializer.Serialize(data, buffer);

        if (serializeResult == -1)
            return NPlusStatus.SerializeError;

        return WriteNextPackage(id, buffer.Slice(0, serializeResult));
    }

    public RequestPackage AcceptNext() => ReadNextPackage();

    public RequestPackage<TOut> AcceptNext<TOut>() where TOut : new()
    {
        var package = ReadNextPackage();

        if (package.Status != NPlusStatus.Done)
            return new(Guid.Empty, package.Status, default, DateTime.MinValue, DateTime.MinValue);

        if (BinarySerializer.Deserialize<TOut>(package.Data) is TOut dataObj)
            return new RequestPackage<TOut>(package.Id, NPlusStatus.Done, dataObj, package.SendedTime, package.AcceptedTime);
        else
            return new(Guid.Empty, NPlusStatus.DeserializeError, default, DateTime.MinValue, DateTime.MinValue);
    }

    public async Task<RequestPackage> AcceptNextAsync(CancellationToken token = default) => await Task.Run(ReadNextPackage, token);

    public async Task<RequestPackage<TOut>> AcceptNextAsync<TOut>(CancellationToken token = default) where TOut : new()
    {
        var result = await Task.Run(() => {
            var package = ReadNextPackage();

            if(package.Status != NPlusStatus.Done)
                return new(Guid.Empty, package.Status, default, DateTime.MinValue, DateTime.MinValue);

            if (BinarySerializer.Deserialize<TOut>(package.Data) is TOut dataObj)
                return new RequestPackage<TOut>(package.Id, NPlusStatus.Done, dataObj, package.SendedTime, package.AcceptedTime);
            else
                return new(Guid.Empty, NPlusStatus.DeserializeError, default, DateTime.MinValue, DateTime.MinValue);
        });
        return result;
    }

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
                else
                    Task.Delay(100).Wait();
            }
            lock (_bufferLocker)
            {
                if(_buffer.ContainsKey(id) && _buffer.Remove(id, out var data))
                    return data;
            }

        }
        return new(Guid.Empty, Array.Empty<byte>(), NPlusStatus.Timeout, DateTime.MinValue, DateTime.MinValue);
    }

    protected RequestPackage ReadNextPackage()
    {
        if (!IsConnected)
            return new(Guid.Empty, Array.Empty<byte>(), NPlusStatus.Disconnected, DateTime.MinValue, DateTime.MinValue);
        try
        {
            if (_tcp.Available == 0)
                return new(Guid.Empty, Array.Empty<byte>(), NPlusStatus.NotAvalible, DateTime.MinValue, DateTime.MinValue);
            lock (_readerLocker)
            {
                var stream = _tcp.GetStream();
                Span<byte> rb = stackalloc byte[PackageHeader.HEADER_BINARY_SIZE];

                stream.Read(rb);

                var header = BinarySerializer.Deserialize<PackageHeader>(rb);

                var dataBuffer = new byte[header.DataSize];
                stream.Read(dataBuffer);

                _acceptedPackages++;
                _acceptedBytes += header.DataSize + PackageHeader.HEADER_BINARY_SIZE;
                return new(header.Id, dataBuffer, header.Status, header.SendedTime, DateTime.UtcNow);

            }
        }
        catch (IOException)
        {
            return new(Guid.Empty, Array.Empty<byte>(), NPlusStatus.AcceptError, DateTime.MinValue, DateTime.MinValue);
        }
    }

    protected NPlusStatus WriteNextPackage(Guid id, in Span<byte> package, NPlusStatus error = NPlusStatus.Done)
    {
        if (!IsConnected)
            return NPlusStatus.Disconnected;
        try
        {
            var stream = _tcp.GetStream();
            lock (_writerLocker)
            {
                var header = new PackageHeader()
                { 
                    DataSize = (ushort)package.Length,
                    Id = id,
                    SendedTime = DateTime.UtcNow,
                    Status = error
                };

                Span<byte> buffer = stackalloc byte[PackageHeader.HEADER_BINARY_SIZE];
                if(BinarySerializer.Serialize(header, buffer) == -1)
                    return NPlusStatus.SerializeError;

                stream.Write(buffer);
                stream.Write(package);
                _sendedPackages++;
                _sendedBytes += package.Length + PackageHeader.HEADER_BINARY_SIZE;
            }
            return NPlusStatus.Done;
        }
        catch (IOException)
        {
            return NPlusStatus.Disconnected;
        }
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