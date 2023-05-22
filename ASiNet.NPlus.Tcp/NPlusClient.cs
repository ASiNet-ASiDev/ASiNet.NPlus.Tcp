using ASiNet.Binary.Lib;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;

namespace ASiNet.NPlus.Tcp;
public class NPlusClient : INplusClient, ITypedNPlusClient
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

        WriteNextPackage(id, package);

        var result = AcceptNext(stream, id, token);
        return new(result.Data, NPlusStatus.None, result.SendedTime, result.AcceptedTime);
    }

    public ResponsePackage<TOut> SendAndWaitResponse<TIn, TOut>(TIn data, CancellationToken token = default) where TOut : new()
    {
        var stream = _tcp.GetStream();
        var id = Guid.NewGuid();

        var buffer = new BinaryBuffer(stackalloc byte[(int)SerializerBufferSize]);

        if(!BinaryBufferSerializer.Serialize(data, ref buffer))
            throw new Exception("Serialize <TIn> error!");

        WriteNextPackage(id, buffer.ToSpan());

        buffer.Clear();

        var result = AcceptNext(stream, id, token);
        if (BinaryBufferSerializer.Deserialize<TOut>(ref buffer) is TOut dataObj)
            return new(dataObj, NPlusStatus.None, result.SendedTime, result.AcceptedTime);
        else
            throw new Exception("Deserialize <TOut> error!");
    }

    public async Task<ResponsePackage> SendAndWaitResponseAsync(byte[] package, CancellationToken token = default)
    {
        var stream = _tcp.GetStream();
        var id = Guid.NewGuid();

        WriteNextPackage(id, package);
        var result = await Task.Run(() => AcceptNext(stream, id, token));
        return new(result.Data, NPlusStatus.None, result.SendedTime, result.AcceptedTime);
        
    }

    public async Task<ResponsePackage<TOut>> SendAndWaitResponseAsync<TIn, TOut>(TIn data, CancellationToken token = default) where TOut : new()
    {
        var result = await Task.Run(() =>
        {
            var stream = _tcp.GetStream();
            var id = Guid.NewGuid();

            var buffer = new BinaryBuffer(new byte[(int)SerializerBufferSize]);

            if (!BinaryBufferSerializer.Serialize(data, ref buffer))
                throw new Exception("Serialize <TIn> error!");

            WriteNextPackage(id, buffer.ToSpan());
            var result = AcceptNext(stream, id, token);

            if (BinaryBufferSerializer.Deserialize<TOut>(ref buffer) is TOut dataObj)
                return new ResponsePackage<TOut>(dataObj, NPlusStatus.None, result.SendedTime, result.AcceptedTime);
            else
                throw new Exception("Deserialize <TOut> error!");
        });
        return result;
    }

    public void SendResponse(Guid id, byte[] package) => WriteNextPackage(id, package);

    public void SendResponse<TOut>(Guid id, TOut data)
    {
        var buffer = new BinaryBuffer(new byte[(int)SerializerBufferSize]);

        if (!BinaryBufferSerializer.Serialize(data, ref buffer))
            throw new Exception("Serialize <TOut> error!");

        WriteNextPackage(id, buffer.ToSpan());
    }

    public RequestPackage AcceptNext() => ReadNextPackage();

    public RequestPackage<TOut> AcceptNext<TOut>() where TOut : new()
    {
        var package = ReadNextPackage();

        var buffer = new BinaryBuffer(new byte[(int)SerializerBufferSize]);

        buffer.Write(package.Data);

        if (BinaryBufferSerializer.Deserialize<TOut>(ref buffer) is TOut dataObj)
            return new RequestPackage<TOut>(package.Id, NPlusStatus.None, dataObj, package.SendedTime, package.AcceptedTime);
        else
            throw new Exception("Deserialize <TOut> error!");
    }

    public async Task<RequestPackage> AcceptNextAsync(CancellationToken token = default) => await Task.Run(ReadNextPackage, token);

    public async Task<RequestPackage<TOut>> AcceptNextAsync<TOut>(CancellationToken token = default) where TOut : new()
    {
        var result = await Task.Run(() => {
            var package = ReadNextPackage();

            var buffer = new BinaryBuffer(new byte[(int)SerializerBufferSize]);

            buffer.Write(package.Data);

            if (BinaryBufferSerializer.Deserialize<TOut>(ref buffer) is TOut dataObj)
                return new RequestPackage<TOut>(package.Id, NPlusStatus.None, dataObj, package.SendedTime, package.AcceptedTime);
            else
                throw new Exception("Deserialize <TOut> error!");
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
        try
        {
            if(_tcp.Available == 0)
                return new(Guid.Empty, Array.Empty<byte>(), NPlusStatus.NotAvalible, DateTime.MinValue, DateTime.MinValue);
            lock (_readerLocker)
            {
                Span<byte> sizeBin = stackalloc byte[sizeof(int)];
                Span<byte> idBin = stackalloc byte[sizeof(decimal)];
                Span<byte> sendedTimeBin = stackalloc byte[sizeof(long)];
                Span<byte> statusBin = stackalloc byte[sizeof(ushort)];
                var sizeValue = 0;
                var idValue = Guid.Empty;
                var stream = _tcp.GetStream();
                var sendedTime = DateTime.MinValue;
                var status = (ushort)0;

                stream.Read(idBin);
                stream.Read(statusBin);
                stream.Read(sendedTimeBin);
                stream.Read(sizeBin);
                idValue = new(idBin);
                sizeValue = BitConverter.ToInt32(sizeBin);
                status = BitConverter.ToUInt16(statusBin);
                var buffer = new byte[sizeValue];
                stream.Read(buffer);

                sendedTime = DateTime.FromBinary(BitConverter.ToInt64(sendedTimeBin));
                _acceptedPackages++;
                _acceptedBytes += buffer.Length + sizeof(int) + sizeof(decimal) + sizeof(long) + sizeof(ushort);
                return new(idValue, buffer, (NPlusStatus)status, sendedTime, DateTime.UtcNow);

            }
        }
        catch (IOException)
        {
            return new(Guid.Empty, Array.Empty<byte>(), NPlusStatus.AcceptError, DateTime.MinValue, DateTime.MinValue);
        }
    }

    protected void WriteNextPackage(Guid id, in Span<byte> package, NPlusStatus error = NPlusStatus.None)
    {
        try
        {
            var stream = _tcp.GetStream();
            lock (_writerLocker)
            {
                stream.Write(id.ToByteArray());
                stream.Write(BitConverter.GetBytes((ushort)error));
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