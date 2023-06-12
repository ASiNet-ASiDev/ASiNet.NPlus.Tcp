using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASiNet.NPlus.Tcp;
public interface INPlusClient : IDisposable, INPlusClientProperties
{
    /// <summary>
    /// Отправить пакет и получить ответ.
    /// </summary>
    /// <param name="package">Данные.</param>
    /// <param name="token">Токен отмены операции.</param>
    /// <returns>Полученые данные.</returns>
    public Task<ResponsePackage> SendAndWaitResponseAsync(byte[] package, CancellationToken token = default);

    /// <summary>
    /// Отправить пакет и получить ответ.
    /// </summary>
    /// <param name="package">Данные.</param>
    /// <param name="token">Токен отмены операции.</param>
    /// <returns>Полученые данные.</returns>
    public ResponsePackage SendAndWaitResponse(Span<byte> package, CancellationToken token = default);
    /// <summary>
    /// Отправить данные.
    /// </summary>
    /// <param name="id">id приёмника на который требуется отправить данные.</param>
    /// <param name="package">Данные.</param>
    public NPlusStatus SendResponse(Guid id, byte[] package, NPlusStatus status = NPlusStatus.Done);

    public NPlusStatus SendResponse(Guid id, Span<byte> package, NPlusStatus status = NPlusStatus.Done);
    /// <summary>
    /// Принять следующий пакет данных.
    /// </summary>
    /// <param name="token">Токен отмены.</param>
    /// <returns>Id приёмника который ожидает ответ и данные пакета.</returns>
    public Task<RequestPackage> AcceptNextAsync(CancellationToken token = default);

    public RequestPackage AcceptNext();
}
