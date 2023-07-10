namespace ASiNet.NPlus.Tcp.Interfaces;
public interface INPlusClient : IDisposable
{
    /// <summary>
    /// <see cref="true"/> если подключён к удалённому сокету, иначе <see cref="false"/>.
    /// </summary>
    public bool IsConnected { get; }
    /// <summary>
    /// <see cref="true"/> если есть в потоке данные для чтения, иначе <see cref="false"/>.
    /// </summary>
    public bool DataAvalible { get; }
    /// <summary>
    /// Размер данных доступных для чтения из потока.
    /// </summary>
    public int AvalibleBytes { get; }
    /// <summary>
    /// Количество успешно принятых пакетов.
    /// </summary>
    public int AcceptedPackages { get; }
    /// <summary>
    /// Количество успешно отправленных пакетов.
    /// </summary>
    public int SendedPackages { get; }
    /// <summary>
    /// Количество успешно принятых байт.
    /// </summary>
    public long AcceptedBytes { get; }
    /// <summary>
    /// Количество успешно отправленых байт.
    /// </summary>
    public long SendedBytes { get; }

    public TimeSpan AcceptTimeout { get; set; }


    /// <summary>
    /// Считать следующий пакет из потока.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="Exceptions.NPlusConnectionException"/>
    /// <exception cref="Exceptions.NPlusReadException"/>
    public Package ReadNextPackage();

    /// <summary>
    /// Получить текущий <see cref="IO.PackageReader"/>
    /// </summary>
    /// <returns></returns>
    /// <exception cref="Exceptions.NPlusConnectionException"/>
    public IPackageReader GetReader();
    /// <summary>
    /// Получить текущий <see cref="IO.PackageWriter"/>
    /// </summary>
    /// <returns></returns>
    /// <exception cref="Exceptions.NPlusConnectionException"/>
    public IPackageWriter GetWriter();
    /// <summary>
    /// Записать пакет в поток.
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    /// <exception cref="Exceptions.NPlusConnectionException"/>
    /// <exception cref="Exceptions.NPlusWriteException"/>
    public Guid WriteNextPackage(Span<byte> data);
    /// <summary>
    /// Записать пакет в поток.
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    /// <exception cref="Exceptions.NPlusConnectionException"/>
    /// <exception cref="Exceptions.NPlusWriteException"/>
    public Guid WriteNextPackage(byte[] data);
}
