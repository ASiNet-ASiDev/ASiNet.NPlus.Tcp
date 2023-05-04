using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASiNet.NPlus.Tcp;
public interface INplusClient : IDisposable
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

    public int PackagesInBuffer { get; }

    public TimeSpan AcceptTimeout { get; set; }

    /// <summary>
    /// Отправить пакет и получить ответ.
    /// </summary>
    /// <param name="package">Данные.</param>
    /// <param name="token">Токен отмены операции.</param>
    /// <returns>Полученые данные.</returns>
    public Task<(byte[] Data, DateTime SendedDate, DateTime AcceptedDate)> SendAndWaitResponseAsync(byte[] package, CancellationToken token = default);

    /// <summary>
    /// Отправить пакет и получить ответ.
    /// </summary>
    /// <param name="package">Данные.</param>
    /// <param name="token">Токен отмены операции.</param>
    /// <returns>Полученые данные.</returns>
    public (byte[] Data, DateTime SendedDate, DateTime AcceptedDate) SendAndWaitResponse(Span<byte> package, CancellationToken token = default);
    /// <summary>
    /// Отправить данные.
    /// </summary>
    /// <param name="id">id приёмника на который требуется отправить данные.</param>
    /// <param name="package">Данные.</param>
    public void SendResponse(Guid id, byte[] package);
    /// <summary>
    /// Принять следующий пакет данных.
    /// </summary>
    /// <param name="token">Токен отмены.</param>
    /// <returns>Id приёмника который ожидает ответ и данные пакета.</returns>
    public Task<(Guid Id, byte[] Data, DateTime SendedTime, DateTime AcceptedTime)> AcceptNextAsync(CancellationToken token = default);

    public (Guid Id, byte[] Data, DateTime SendedTime, DateTime AcceptedTime) AcceptNext();
}
