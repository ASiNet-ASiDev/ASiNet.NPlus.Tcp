using ASiNet.Binary.Lib.Serializer;
using ASiNet.NPlus.Tcp.Exceptions;
using ASiNet.NPlus.Tcp.Interfaces;
using ASiNet.NPlus.Tcp.IO;
using System.Net.Sockets;

namespace ASiNet.NPlus.Tcp;
public class NPlusClient : INPlusClient
{

    public NPlusClient(TcpClient client)
    {
        client.ReceiveTimeout = TimeSpan.FromSeconds(60).Milliseconds;
        _tcp = client;

        _reader = new(() => new PackageReader(this));
        _writer = new(() => new PackageWriter(this));
    }

    public NPlusClient(string host, int port)
    {
        var client = new TcpClient(host, port)
        {
            ReceiveTimeout = TimeSpan.FromSeconds(60).Milliseconds
        };
        _tcp = client;

        _reader = new(() => new PackageReader(this));
        _writer = new(() => new PackageWriter(this));
    }

    public bool IsConnected => CheckConnected();
    public bool DataAvalible => _tcp?.Available > 0;
    public int AvalibleBytes => _tcp?.Available ?? -1;
    public int AcceptedPackages => _acceptedPackages;
    public int SendedPackages => _sendedPackages;
    public long AcceptedBytes => _acceptedBytes;
    public long SendedBytes => _sendedBytes;

    public TimeSpan AcceptTimeout { get; set; } = TimeSpan.FromSeconds(30);

    private readonly object _readerLocker = new();
    private readonly object _writerLocker = new();

    private int _acceptedPackages;
    private int _sendedPackages;
    private long _acceptedBytes;
    private long _sendedBytes;

    private Lazy<IPackageReader> _reader;
    public Lazy<IPackageWriter> _writer;

    private TcpClient _tcp = null!;

    public Guid WriteNextPackage(Span<byte> data)
    {
        var id = Guid.NewGuid();
        var result = WritePackage(id, data);

        if (result == NPlusStatus.Done)
            return id;
        else
            throw new NPlusWriteException(result);
    }

    public Guid WriteNextPackage(byte[] data)
    {
        var id = Guid.NewGuid();
        var result = WritePackage(id, data);

        if (result == NPlusStatus.Done)
            return id;
        else
            throw new NPlusReadException(result);
    }

    public IPackageReader GetReader()
    {
        if (!IsConnected)
            throw new NPlusConnectionException();
        return _reader.Value;
    }

    public IPackageWriter GetWriter()
    {
        if (!IsConnected)
            throw new NPlusConnectionException();
        return _writer.Value;
    }

    public Package ReadNextPackage()
    {
        return ReadPackage();
    }

    protected Package ReadPackage()
    {
        if (!IsConnected)
            throw new NPlusConnectionException();
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
            throw new NPlusConnectionException();
        }
    }

    protected NPlusStatus WritePackage(Guid id, in Span<byte> package, NPlusStatus error = NPlusStatus.Done)
    {
        if (!IsConnected)
            throw new NPlusConnectionException();
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
                if (BinarySerializer.Serialize(header, buffer) == -1)
                    throw new NPlusSerializerException();

                stream.Write(buffer);
                stream.Write(package);
                _sendedPackages++;
                _sendedBytes += package.Length + PackageHeader.HEADER_BINARY_SIZE;
            }
            return NPlusStatus.Done;
        }
        catch (IOException)
        {
            throw new NPlusConnectionException();
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
        if (_reader.IsValueCreated)
            _reader.Value.Dispose();
        if (_writer.IsValueCreated)
            _writer.Value.Dispose();
        GC.SuppressFinalize(this);
    }
}